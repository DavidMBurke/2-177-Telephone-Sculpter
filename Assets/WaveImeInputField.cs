// WaveImeInputField.cs
using UnityEngine;
using TMPro;
using Wave.Essence;   // Wave XR Plugin – Essence

[DisallowMultipleComponent]
public class WaveImeInputField : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;

    // Options
    [Tooltip("e.g., \"\" normal, \"numeric\" number pad")]
    public string locale = "";
    public string title  = "Enter text";

    private IMEManager ime;
    private IMEManager.IMEParameter param;

    // Defer UI updates (safer on device)
    private string _pendingText = null;
    private bool _pendingSubmit = false;

    void Awake()
    {
        if (!inputField) inputField = GetComponent<TMP_InputField>();
        inputField.onSelect.AddListener(_ => OnFieldSelected());
        inputField.onDeselect.AddListener(_ => OnFieldDeselected());
    }

    void Start()
    {
        ime = IMEManager.instance;
        if (ime == null)
        {
            Debug.LogWarning("Wave IMEManager is null (Wave XR not active?). Keyboard won't show.");
            return;
        }

        // Minimal sane params (device can ignore size/pos)
        param = new IMEManager.IMEParameter(
            id: 0,
            type: 0x02,
            mode: 2,
            exist: "",
            cursor: 0,
            selectStart: 0, selectEnd: 0,
            pos: new double[] { 0, 0, -1 },
            rot: new double[] { 1, 0, 0, 0 },
            width: 800, height: 800, shadow: 100,
            locale: locale,
            title: title,
            extraInt: 0, extraString: "",
            buttonId: 0   // <— just use 0
        );
    }

    private void OnFieldSelected()
    {
        if (ime == null) return;

        // Seed IME with current text + caret at end
        param.exist = inputField.text ?? "";
        param.cursor = param.exist.Length;
        param.selectStart = param.selectEnd = param.cursor;

        // Show keyboard with editor panel; hook callbacks
        ime.showKeyboard(param, true, OnInputDone, OnInputClicked);

        inputField.ActivateInputField();
        inputField.caretPosition = inputField.text.Length;
    }

    private void OnFieldDeselected()
    {
        ime?.hideKeyboard();
    }

    // Live per-key updates (optional)
    private void OnInputClicked(IMEManager.InputResult r)
    {
        _pendingText = r.InputContent;   // or r.result / r.content depending on your SDK
    }

    private void OnInputDone(IMEManager.InputResult r)
    {
        _pendingText = r.InputContent;
        _pendingSubmit = true;
        ime.hideKeyboard();
    }

    void Update()
    {
        if (_pendingText != null)
        {
            inputField.text = _pendingText;
            inputField.caretPosition = inputField.text.Length;
            _pendingText = null;
        }
        if (_pendingSubmit)
        {
            _pendingSubmit = false;
            inputField.onEndEdit?.Invoke(inputField.text);
            inputField.DeactivateInputField();
        }
    }
}
