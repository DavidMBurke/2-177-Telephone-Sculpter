// WaveImeInputField.cs
using UnityEngine;
using TMPro;
using Wave.Essence;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_InputField))]
public class WaveImeInputField : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;

    [Tooltip("e.g., \"\" normal, \"numeric\" number pad")]
    public string locale = "";
    public string title = "Enter text";

    [Tooltip("Show Wave IME mini editor panel (recommended).")]
    public bool useEditorPanel = true;

    private IMEManager ime;
    private IMEManager.IMEParameter param;

    // Queue UI changes for main thread
    private string _pendingText = null;
    private bool _pendingSubmit = false;

    // State flags
    private bool _imeOpen = false;
    private bool _suppressHideOnDeselect = false;
    private bool _closingIme = false;
    private bool _submittedThisCycle = false;

#if UNITY_ANDROID && !UNITY_EDITOR
    private const float BACK_SUPPRESS_SECONDS = 0.35f;
    private float _suppressBackUntil = 0f;
#endif

    // Keep delegates so we can remove reliably
    private UnityEngine.Events.UnityAction<string> _onSelectHandler;
    private UnityEngine.Events.UnityAction<string> _onDeselectHandler;

    void Awake()
    {
        inputField = inputField ? inputField : GetComponent<TMP_InputField>();
        inputField.shouldHideMobileInput = true;
        inputField.richText = false;

        // Prevent Android Back from exiting automatically
        Input.backButtonLeavesApp = false;

        _onSelectHandler = _ => OnFieldSelected();
        _onDeselectHandler = _ => OnFieldDeselected();
        inputField.onSelect.AddListener(_onSelectHandler);
        inputField.onDeselect.AddListener(_onDeselectHandler);
    }

    void OnDestroy()
    {
        if (inputField != null)
        {
            if (_onSelectHandler != null) inputField.onSelect.RemoveListener(_onSelectHandler);
            if (_onDeselectHandler != null) inputField.onDeselect.RemoveListener(_onDeselectHandler);
        }
    }

    void Start()
    {
        ime = IMEManager.instance;
        if (ime == null)
        {
            Debug.LogWarning("[WaveIME] IMEManager.instance is null (Wave XR not active?)");
            return;
        }

        // Some Wave builds require this to initialize JNI side.
        bool ok = ime.isInitialized();
        Debug.Log($"[WaveIME] isInitialized() = {ok}");

        param = new IMEManager.IMEParameter(
            id: 0,
            type: 0x02,               // text
            mode: 2,                  // single-line
            exist: "",
            cursor: 0,
            selectStart: 0, selectEnd: 0,
            pos: new double[] { 0, 0, -1 },
            rot: new double[] { 1, 0, 0, 0 },
            width: 800, height: 800, shadow: 100,
            locale: locale,
            title: title,
            extraInt: 0, extraString: "",
            buttonId: 0
        );
    }

    private void OnFieldSelected()
    {
        if (ime == null) return;

        param.exist = inputField.text ?? "";
        param.cursor = param.exist.Length;
        param.selectStart = param.selectEnd = param.cursor;

        // Opening IME usually steals TMP focus, so TMP.onDeselect will fire.
        _suppressHideOnDeselect = true;
        _imeOpen = true;
        _submittedThisCycle = false;

        try
        {
            ime.showKeyboard(param, useEditorPanel, OnInputDone, OnInputClicked);
            Debug.Log("[WaveIME] showKeyboard() requested.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WaveIME] showKeyboard() threw: {e}");
            _imeOpen = false;
            _suppressHideOnDeselect = false;
        }

        inputField.ActivateInputField();
        inputField.caretPosition = inputField.text.Length;
        inputField.ForceLabelUpdate();
    }

    private void OnFieldDeselected()
    {
        // DO NOT hide IME here; this used to kill callbacks.
        if (_suppressHideOnDeselect)
        {
            Debug.Log("[WaveIME] TMP deselected while IME open; suppressing hide.");
            return;
        }
        // Optional: close IME if user clicked away and it's still open.
        // SafeHideIme();
    }

    // Many Wave builds fire individual keys here (including ENTER)
    private void OnInputClicked(IMEManager.InputResult r)
    {
        if (r == null) return;
        var txt = r.InputContent ?? "";
        var key = r.KeyCode;

        if (key == IMEManager.InputResult.Key.ENTER)
            txt = txt.TrimEnd('\r', '\n');

        Debug.Log($"[WaveIME] Clicked key={key}, text='{txt}'");

        if (!string.IsNullOrEmpty(txt))
            _pendingText = txt;

        // Treat ENTER as submit now for builds that defer completion until panel "Done/?"
        if (key == IMEManager.InputResult.Key.ENTER && !_submittedThisCycle)
        {
            _pendingSubmit = true;
            _submittedThisCycle = true;
            SafeHideIme();
            Debug.Log("[WaveIME] ENTER -> submit & hide IME.");
        }

        if (key == IMEManager.InputResult.Key.CLOSE)
        {
            SafeHideIme();
            Debug.Log("[WaveIME] CLOSE -> hide IME (no submit).");
        }
    }

    // Called when IME commits (panel Done/?). Some builds only send this when editor closes.
    private void OnInputDone(IMEManager.InputResult r)
    {
        var txt = (r?.InputContent ?? "").TrimEnd('\r', '\n');
        Debug.Log($"[WaveIME] Done text: '{txt}' (error={r?.ErrorCode})");

        _pendingText = txt;

        if (!_submittedThisCycle)
        {
            _pendingSubmit = true;
            _submittedThisCycle = true;
        }

        SafeHideIme();
    }

    private void SafeHideIme()
    {
        if (!_imeOpen || _closingIme) return;

        _closingIme = true;
        try
        {
            ime?.hideKeyboard();
        }
        catch (System.Exception e)
        {
            // Catch AndroidJavaException here to avoid hard crash
            Debug.LogWarning($"[WaveIME] hideKeyboard() threw: {e}");
        }
        finally
        {
            _imeOpen = false;
            _suppressHideOnDeselect = false;
#if UNITY_ANDROID && !UNITY_EDITOR
            _suppressBackUntil = Time.unscaledTime + BACK_SUPPRESS_SECONDS;
#endif
            _closingIme = false;
        }
    }

    void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Swallow BACK during the brief window after closing IME.
        if (Time.unscaledTime < _suppressBackUntil)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Note: Unity cannot truly "consume" OS back, but with
                // backButtonLeavesApp=false it won't auto-exit; this guard avoids you calling quit.
                Debug.Log("[WaveIME] Swallowing BACK while IME just closed.");
            }
        }
#endif

        if (_pendingText != null)
        {
            inputField.text = _pendingText;

            int end = inputField.text.Length;
            inputField.caretPosition = end;
            inputField.selectionStringAnchorPosition = end;
            inputField.selectionStringFocusPosition = end;

            inputField.ForceLabelUpdate();
            _pendingText = null;
        }

        if (_pendingSubmit)
        {
            _pendingSubmit = false;
            try
            {
                inputField.onEndEdit?.Invoke(inputField.text);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[WaveIME] onEndEdit listener threw: {e}");
            }

            inputField.DeactivateInputField();
            inputField.ForceLabelUpdate();
            _suppressHideOnDeselect = false;

            Debug.Log("[WaveIME] Submit complete.");
        }
    }
}
