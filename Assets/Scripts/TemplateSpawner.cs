using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRSimpleInteractable))]
public class TemplateSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [Tooltip("Clean prefab with XRGrabInteractable (NO TemplateSpawner on it).")]
    public GameObject spawnPrefab;

    [Tooltip("Where to spawn. If null, uses template's transform.")]
    public Transform spawnOrigin;

    [Header("Safety")]
    [Tooltip("Extra delay after hover leaves before re-enabling interactions.")]
    public float cooldownAfterHoverExit = 1.0f;

    private XRSimpleInteractable _simple;
    private XRInteractionManager _manager;
    private readonly HashSet<IXRHoverInteractor> _hovering = new();
    private bool _locked;

    void Awake()
    {
        _simple = GetComponent<XRSimpleInteractable>();
        _manager = FindObjectOfType<XRInteractionManager>();

        _simple.hoverEntered.AddListener(OnHoverEntered);
        _simple.hoverExited.AddListener(OnHoverExited);
        _simple.selectEntered.AddListener(OnSelectEntered);

        // Make the template stationary & non-physical
        var rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
    }

    void OnDestroy()
    {
        _simple.hoverEntered.RemoveListener(OnHoverEntered);
        _simple.hoverExited.RemoveListener(OnHoverExited);
        _simple.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        _hovering.Add(args.interactorObject);
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        _hovering.Remove(args.interactorObject);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (_locked) return;
        if (spawnPrefab == null || _manager == null) return;

        // Only proceed if the selecting interactor is a grab-capable interactor
        if (args.interactorObject is not IXRSelectInteractor sel) return;

        // If they’re already holding something else (besides this template “button”), bail.
        // (XRSimpleInteractable itself isn’t "held", but guard anyway for custom interactors.)
        if (sel.hasSelection && sel.interactablesSelected.Count > 0)
        {
            // Allow selection handoff only when the template is the first/only thing they hit.
            // If your setup marks "select" during simple interactions, uncomment next line to be strict:
            // if (sel.interactablesSelected.Count > 1) return;
        }

        StartCoroutine(SpawnAndHandOff(sel));
    }

    private IEnumerator SpawnAndHandOff(IXRSelectInteractor sel)
    {
        // LOCK template immediately to prevent re-entry while the ray stays on it
        LockTemplate(true);

        // Instantiate inactive so nothing on the clone runs yet
        GameObject go = Instantiate(spawnPrefab);
        go.SetActive(false);

        // Ensure no spawner scripts exist on the clone
        foreach (var sp in go.GetComponentsInChildren<TemplateSpawner>(true))
            Destroy(sp);

        // Ensure XRGrabInteractable exists
        if (!go.TryGetComponent<XRGrabInteractable>(out var cloneGrab))
            cloneGrab = go.AddComponent<XRGrabInteractable>();

        // If your prefab already has a Rigidbody, leave its settings alone.
        // Only add one if it’s missing.
        if (!go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb = go.AddComponent<Rigidbody>();
            // optional: set defaults only if you *need* to
            rb.isKinematic = false;
            rb.useGravity = true;
        }


        // Spawn pose: use interactor attach if available, else origin/template
        Vector3 pos; Quaternion rot;
        if (sel.transform is Transform interactorTf &&
            (sel as MonoBehaviour) != null &&
            (sel as XRBaseInteractor) != null &&
            (sel as XRBaseInteractor).attachTransform != null)
        {
            var attach = (sel as XRBaseInteractor).attachTransform;
            pos = attach.position; rot = attach.rotation;
        }
        else
        {
            var origin = spawnOrigin != null ? spawnOrigin : transform;
            pos = origin.position; rot = origin.rotation;
        }
        go.transform.SetPositionAndRotation(pos, rot);

        // Now safe to activate clone
        go.SetActive(true);

        // Hand off selection to the clone
        _manager.SelectEnter(sel, cloneGrab);

        // Wait until the interactor is no longer hovering the template
        // (prevents instant retrigger while ray/hand remains on it)
        yield return new WaitUntil(() => _hovering.Count == 0);

        // Extra cooldown as you requested
        yield return new WaitForSeconds(cooldownAfterHoverExit);

        LockTemplate(false);
    }

    private void LockTemplate(bool locked)
    {
        _locked = locked;

        // Disable interactable & its colliders while locked to avoid any events
        _simple.enabled = !locked;
        var cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.enabled = !locked;
    }
}
