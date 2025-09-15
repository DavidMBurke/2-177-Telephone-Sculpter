using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class CloneOnGrab : MonoBehaviour
{
    [Header("Clone Source")]
    [Tooltip("Use a clean prefab WITHOUT CloneOnGrab if available. If null, this object will be duplicated safely.")]
    public GameObject clonePrefab;

    [Header("Behavior")]
    public bool keepTemplate = true;
    public float debounceSeconds = 0.1f;
    public float cooldownSeconds = 1.0f;   // per-interactor cooldown

    private XRGrabInteractable templateGrab;
    private XRInteractionManager manager;

    // per-interactor cooldown
    private readonly Dictionary<IXRSelectInteractor, float> _lastSpawnTime = new();

    void Awake()
    {
        templateGrab = GetComponent<XRGrabInteractable>();
        manager = FindObjectOfType<XRInteractionManager>();

        templateGrab.selectEntered.AddListener(OnSelectEntered);

        // Make template stationary
        var rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
    }

    void OnDestroy()
    {
        templateGrab.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject is not IXRSelectInteractor sel) return;

        // Per-interactor cooldown (safety net)
        float now = Time.time;
        if (_lastSpawnTime.TryGetValue(sel, out float last) && now - last < cooldownSeconds)
            return;
        _lastSpawnTime[sel] = now;

        // Only spawn if the interactor isn't already holding another thing
        // (hasSelection true means it’s currently selecting the template; allow if count == 1)
        if (sel.hasSelection && sel.interactablesSelected.Count > 1)
            return;

        // --- CREATE CLONE INACTIVE ---
        GameObject srcGO = clonePrefab != null ? clonePrefab : gameObject;
        GameObject cloneGO = Instantiate(srcGO);
        cloneGO.SetActive(false); // critical: prevent Awake/handlers from running

        // Strip all CloneOnGrab from clone hierarchy BEFORE activation
        // (they never get a chance to subscribe to events)
        foreach (var cloner in cloneGO.GetComponentsInChildren<CloneOnGrab>(true))
            Destroy(cloner);

        // Ensure grab/physics are set up on the clone
        if (!cloneGO.TryGetComponent<XRGrabInteractable>(out var cloneGrab))
            cloneGrab = cloneGO.AddComponent<XRGrabInteractable>();

        if (!cloneGO.TryGetComponent<Rigidbody>(out var cloneRB))
            cloneRB = cloneGO.AddComponent<Rigidbody>();

        cloneRB.isKinematic = false;
        cloneRB.useGravity = true;

        // Match pose of template
        cloneGO.transform.SetPositionAndRotation(
            templateGrab.transform.position,
            templateGrab.transform.rotation
        );

        // Now safe to activate; no CloneOnGrab remains to loop
        cloneGO.SetActive(true);

        // Hand off selection to the clone
        manager.SelectExit(sel, templateGrab);
        manager.SelectEnter(sel, cloneGrab);

        // Debounce the template so the ray doesn't immediately reselect it
        StartCoroutine(DebounceTemplate());

        if (!keepTemplate)
        {
            templateGrab.enabled = false;
            foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
        }
    }

    private IEnumerator DebounceTemplate()
    {
        templateGrab.enabled = false;
        var cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;

        yield return new WaitForSeconds(debounceSeconds);

        foreach (var c in cols) c.enabled = true;
        templateGrab.enabled = true;
    }
}
