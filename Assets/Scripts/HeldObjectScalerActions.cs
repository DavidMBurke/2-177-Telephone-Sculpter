using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using CommonUsages = UnityEngine.XR.CommonUsages;

public class HeldObjectScalerActions : MonoBehaviour
{
    [Header("Actions (Right Controller)")]
    [Tooltip("Bind to the right controller A button (primaryButton).")]
    public InputActionProperty scaleUpAction;     // A
    [Tooltip("Bind to the right controller B button (secondaryButton).")]
    public InputActionProperty scaleDownAction;   // B

    [Header("Scaling")]
    [Tooltip("Percent per second (0.5 = 50%/sec).")]
    public float scaleRate = 0.5f;
    public float minScale = 0.1f;
    public float maxScale = 3.0f;

    // Will auto-locate any interactor on this object (Ray or Direct).
    private IXRSelectInteractor rightInteractor;

    // Fallback device query if actions are not set
    private UnityEngine.XR.InputDevice rightHandDevice;

    void Awake()
    {
        // Find any select-capable interactor on this object or its children
        rightInteractor = GetComponentInChildren<IXRSelectInteractor>(true);

        // Enable actions if assigned
        if (scaleUpAction.action != null) scaleUpAction.action.Enable();
        if (scaleDownAction.action != null) scaleDownAction.action.Enable();
    }

    void OnEnable()
    {
        if (scaleUpAction.action != null) scaleUpAction.action.Enable();
        if (scaleDownAction.action != null) scaleDownAction.action.Enable();
    }

    void OnDisable()
    {
        if (scaleUpAction.action != null) scaleUpAction.action.Disable();
        if (scaleDownAction.action != null) scaleDownAction.action.Disable();
    }

    void Update()
    {
        // Reacquire interactor if needed (e.g., component enabled later)
        if (rightInteractor == null)
            rightInteractor = GetComponentInChildren<IXRSelectInteractor>(true);

        if (rightInteractor == null)
            return;

        // Are we holding something?
        if (!rightInteractor.hasSelection)
            return;

        var held = rightInteractor.interactablesSelected.Count > 0
            ? rightInteractor.interactablesSelected[0] as Object
            : null;

        Transform target = null;
        if (held is IXRSelectInteractable si && si is Component c)
            target = c.transform;

        if (target == null)
            return;

        // Read inputs (prefer actions; fall back to XR device if none)
        bool upPressed = false, downPressed = false;

        if (scaleUpAction.action != null)
            upPressed = scaleUpAction.action.IsPressed();
        if (scaleDownAction.action != null)
            downPressed = scaleDownAction.action.IsPressed();

        if ((scaleUpAction.action == null) || (scaleDownAction.action == null))
        {
            if (!rightHandDevice.isValid)
                rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            if (rightHandDevice.isValid)
            {
                if (scaleUpAction.action == null &&
                    rightHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool a))
                    upPressed |= a;

                if (scaleDownAction.action == null &&
                    rightHandDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool b))
                    downPressed |= b;
            }
        }

        float factor = 1f;
        if (upPressed) factor *= 1f + (scaleRate * Time.deltaTime);
        if (downPressed) factor *= 1f - (scaleRate * Time.deltaTime);

        if (!Mathf.Approximately(factor, 1f))
        {
            Vector3 newScale = target.localScale * factor;
            float clamped = Mathf.Clamp(newScale.x, minScale, maxScale);
            // Keep uniform
            target.localScale = new Vector3(clamped, clamped, clamped);
        }
    }
}
