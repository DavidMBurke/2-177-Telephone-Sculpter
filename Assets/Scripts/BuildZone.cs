using System.Collections.Generic;
using UnityEngine;

public class BuildZone : MonoBehaviour
{
    [Header("Where finished sculptures go")]
    public Transform galleryParent;
    public Transform galleryAnchor;
    public Vector3 fallbackOffset = new Vector3(0, 0, 2f);

    [Header("Filtering")]
    public LayerMask acceptsLayers = ~0;
    public string acceptsTag = "";

    [Header("Review Hook")]
    public GalleryReviewManager reviewManager;

    void Awake()
    {
        if (reviewManager == null)
            reviewManager = FindFirstObjectByType<GalleryReviewManager>(FindObjectsInactive.Include);
    }

    public GameObject SaveAndClear(string prompt, string guess)
    {
        if (transform.childCount == 0) return null;

        var sculpture = new GameObject($"Sculpture_{prompt}");
        if (galleryParent != null)
            sculpture.transform.SetParent(galleryParent, true);

        var toMove = new List<Transform>(transform.childCount);
        for (int i = 0; i < transform.childCount; i++)
            toMove.Add(transform.GetChild(i));

        foreach (var child in toMove)
            child.SetParent(sculpture.transform, true);

        if (galleryAnchor != null)
            sculpture.transform.SetPositionAndRotation(galleryAnchor.position, galleryAnchor.rotation);
        else
            sculpture.transform.position = (galleryParent ? galleryParent.position : transform.position)
                                         + (galleryParent ? galleryParent.TransformVector(fallbackOffset)
                                                          : transform.TransformVector(fallbackOffset));

        foreach (var rb in sculpture.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;
        foreach (var col in sculpture.GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (reviewManager == null)
            reviewManager = FindObjectOfType<GalleryReviewManager>();
        if (reviewManager != null)
            reviewManager.Register(sculpture, prompt, guess);
        else
            Debug.LogWarning("[BuildZone] No GalleryReviewManager found; sculptures won’t cycle.");

        sculpture.SetActive(false);
        return sculpture;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(acceptsTag) && !other.CompareTag(acceptsTag)) return;
        if ((acceptsLayers.value & (1 << other.gameObject.layer)) == 0) return;

        other.transform.SetParent(transform, true);
    }
}
