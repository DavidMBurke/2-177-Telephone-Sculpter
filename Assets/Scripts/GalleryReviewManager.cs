using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GalleryReviewManager : MonoBehaviour
{
    [Header("Display")]
    public Transform displayAnchor;
    public TMP_Text promptText;
    public float autoAdvanceSeconds = 3f;

    [Tooltip("If true, item will be parented under displayAnchor while shown.")]
    public bool reparentToDisplay = false;

    [Tooltip("Applied when the item is shown at the displayAnchor.")]
    public Vector3 displayScale = Vector3.one;

    private class Item
    {
        public GameObject go;
        public string prompt;
        public string guess;

        // For optional restoration if you want to put items back later
        public Transform originalParent;
        public Vector3 originalPosition;
        public Quaternion originalRotation;
        public Vector3 originalScale;
    }

    private readonly List<Item> _items = new List<Item>();
    private int _current = -1;
    private Coroutine _cycleCo;

    /// <summary>
    /// BuildZone.SaveAndClear(...) calls this. Signature must match!
    /// </summary>
    public void Register(GameObject sculpture, string prompt, string guess)
    {
        if (sculpture == null) return;

        // Cache original xform
        var it = new Item
        {
            go = sculpture,
            prompt = prompt,
            guess = guess ?? "",
            originalParent = sculpture.transform.parent,
            originalPosition = sculpture.transform.position,
            originalRotation = sculpture.transform.rotation,
            originalScale = sculpture.transform.localScale
        };

        // Ensure off until it's their turn
        sculpture.SetActive(false);

        _items.Add(it);
    }

    public void StartReview()
    {
        if (_items.Count == 0)
        {
            Debug.Log("[GalleryReviewManager] StartReview called with no items.");
            return;
        }

        StopReview(); // ensure no double coroutine
        HideAll();
        _current = -1;

        // First frame show, then auto-advance
        Next();

        if (autoAdvanceSeconds > 0f)
            _cycleCo = StartCoroutine(AutoCycle());
    }

    public void StopReview()
    {
        if (_cycleCo != null)
        {
            StopCoroutine(_cycleCo);
            _cycleCo = null;
        }
        // Keep items hidden when stopping
        HideAll();
        _current = -1;
    }

    public void ClearAll()
    {
        StopReview();
        HideAll();
        _items.Clear();
        _current = -1;
        if (promptText != null) promptText.text = "";
    }

    public void Next()
    {
        if (_items.Count == 0) return;

        // Hide old
        if (_current >= 0 && _current < _items.Count && _items[_current].go != null)
            _items[_current].go.SetActive(false);

        // Advance
        _current = (_current + 1) % _items.Count;

        // Show current
        ShowCurrent();
    }

    private IEnumerator AutoCycle()
    {
        var wait = new WaitForSeconds(autoAdvanceSeconds);
        while (true)
        {
            yield return wait;
            Next();
        }
    }

    private void ShowCurrent()
    {
        if (_current < 0 || _current >= _items.Count) return;

        var it = _items[_current];
        if (it.go == null) return;

        // Text
        if (promptText != null)
        {
            if (!string.IsNullOrEmpty(it.guess))
                promptText.text = $"Prompt: {it.prompt}\nGuess: {it.guess}";
            else
                promptText.text = $"Prompt: {it.prompt}";
        }

        // Positioning
        var tf = it.go.transform;

        if (displayAnchor != null)
        {
            if (reparentToDisplay)
                tf.SetParent(displayAnchor, worldPositionStays: false);

            // Place/rotate to anchor
            tf.position = displayAnchor.position;
            tf.rotation = displayAnchor.rotation;
        }

        // Scale
        if (displayScale != Vector3.zero)
            tf.localScale = displayScale;

        // Optional: center the bounds at the anchor for nicer framing
        if (displayAnchor != null && TryGetHierarchyBounds(it.go, out var b))
        {
            var delta = displayAnchor.position - b.center;
            tf.position += delta;
        }

        it.go.SetActive(true);
    }

    private void HideAll()
    {
        foreach (var it in _items)
        {
            if (it?.go != null)
                it.go.SetActive(false);
        }
    }

    private bool TryGetHierarchyBounds(GameObject go, out Bounds bounds)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = new Bounds(go.transform.position, Vector3.zero);
            return false;
        }
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return true;
    }
}
