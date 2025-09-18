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
    [Tooltip("Reparent to the anchor so local transform resets (usually not required).")]
    public bool reparentToDisplay = false;
    public Vector3 displayScale = Vector3.one;

    private class Item
    {
        public GameObject go;
        public string prompt;
        public string guess;
        public Item(GameObject go, string prompt, string guess)
        {
            this.go = go;
            this.prompt = prompt;
            this.guess = guess;
        }
    }

    private readonly List<Item> _items = new List<Item>();
    private int _current = -1;
    private Coroutine _cycleCo;
    private bool showPrompt = true;

    public void Register(GameObject sculpture, string prompt, string guess)
    {
        if (sculpture == null) return;
        sculpture.SetActive(false);
        _items.Add(new Item(sculpture, prompt, guess));
    }

    public void StartReview()
    {
        if (_items.Count == 0)
        {
            Debug.LogWarning("[GalleryReview] No items to review.");
            return;
        }
        HideAll();
        _current = -1;
        Next();

        if (autoAdvanceSeconds > 0f)
        {
            if (_cycleCo != null) StopCoroutine(_cycleCo);
            _cycleCo = StartCoroutine(CycleRoutine());
        }
    }

    public void StopReview()
    {
        if (_cycleCo != null) StopCoroutine(_cycleCo);
        _cycleCo = null;
        HideAll();
        _current = -1;
    }

    public void Next()
    {
        if (_items.Count == 0) return;
        if (_current >= 0 && _current < _items.Count && _items[_current].go != null)
            _items[_current].go.SetActive(false);

        _current = (_current + 1) % _items.Count;
        ShowCurrent();
    }

    public void Prev()
    {
        if (_items.Count == 0) return;
        if (_current >= 0 && _current < _items.Count && _items[_current].go != null)
            _items[_current].go.SetActive(false);

        _current = (_current - 1 + _items.Count) % _items.Count;
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        var item = _items[_current];
        if (item == null || item.go == null) return;

        if (displayAnchor != null)
            PlaceAtAnchorByBoundsCenter(item.go, displayAnchor);

        item.go.SetActive(true);
        if (promptText != null)
            promptText.text = showPrompt ? item.prompt ?? "" : item.guess ?? "";
    }

    private void HideAll()
    {
        foreach (var it in _items)
            if (it != null && it.go != null)
                it.go.SetActive(false);
    }

    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoAdvanceSeconds);
            showPrompt = !showPrompt;
            if (!showPrompt)
                Next();
            else
                ShowCurrent();
        }
    }

    private void PlaceAtAnchorByBoundsCenter(GameObject go, Transform anchor)
    {
        if (reparentToDisplay)
            go.transform.SetParent(anchor, true);

        Bounds b;
        if (TryGetHierarchyBounds(go, out b))
        {
            Vector3 offset = b.center - go.transform.position;
            go.transform.SetPositionAndRotation(anchor.position - offset, anchor.rotation);
        }
        else
        {
            go.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        }

        go.transform.localScale = displayScale;
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

    public void ClearAll()
    {
        StopReview();
        HideAll();
        _items.Clear();
        _current = -1;
        if (promptText != null) promptText.text = "";
    }
}
