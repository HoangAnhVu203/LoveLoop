using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollSnapToCenter : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [Header("Refs")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;

    [Header("Items (Thumbs)")]
    [SerializeField] private List<RectTransform> itemRects = new();

    [Header("Optional: Update info when centered")]
    [SerializeField] private CharacterInfoPanelUI infoPanel;
    [SerializeField] private List<CharacterThumbItemUI> itemUIs = new(); 

    [Header("Snap Settings")]
    [SerializeField] private float snapDuration = 0.18f;     
    [SerializeField] private float snapThreshold = 5f;       
    [SerializeField] private bool updateWhileDragging = true;
    [SerializeField] private bool highlightFocused = true;

    public System.Action<CharacterData> OnCenteredChanged;


    private bool isDragging;
    private Coroutine snapCR;
    private int currentIndex = -1;

    void Reset()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            viewport = scrollRect.viewport;
            content = scrollRect.content;
        }
    }

    void Awake()
    {
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        if (viewport == null && scrollRect != null) viewport = scrollRect.viewport;
        if (content == null && scrollRect != null) content = scrollRect.content;
    }

    void Start()
    {
        RefreshImmediate();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        StopSnap();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (updateWhileDragging)
            RefreshImmediate();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        SnapToClosest();
    }
    public void SetItems(List<CharacterThumbItemUI> uis)
    {
        itemUIs = uis;
        itemRects = new List<RectTransform>(uis.Count);
        for (int i = 0; i < uis.Count; i++)
            itemRects.Add((RectTransform)uis[i].transform);

        currentIndex = -1;
        RefreshImmediate();
        SnapToClosest();
    }

    private void SnapToClosest()
    {
        if (itemRects == null || itemRects.Count == 0) return;

        int idx = FindClosestIndexToCenter();
        Vector2 target = GetAnchoredPosToCenterItem(idx);

        if (Vector2.Distance(content.anchoredPosition, target) <= snapThreshold)
        {
            ApplyFocused(idx);
            return;
        }

        StopSnap();
        snapCR = StartCoroutine(SnapCoroutine(target, idx));
    }

    private IEnumerator SnapCoroutine(Vector2 targetAnchoredPos, int targetIndex)
    {
        scrollRect.velocity = Vector2.zero;

        Vector2 start = content.anchoredPosition;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.001f, snapDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            content.anchoredPosition = Vector2.LerpUnclamped(start, targetAnchoredPos, eased);
            yield return null;
        }

        content.anchoredPosition = targetAnchoredPos;
        ApplyFocused(targetIndex);
        snapCR = null;
    }

    private void StopSnap()
    {
        if (snapCR != null)
        {
            StopCoroutine(snapCR);
            snapCR = null;
        }
    }

    private void RefreshImmediate()
    {
        if (itemRects == null || itemRects.Count == 0) return;
        int idx = FindClosestIndexToCenter();
        ApplyFocused(idx);
    }

    private int FindClosestIndexToCenter()
    {
        Vector3 viewportCenterWorld = viewport.TransformPoint(viewport.rect.center);

        float best = float.MaxValue;
        int bestIdx = 0;

        for (int i = 0; i < itemRects.Count; i++)
        {
            RectTransform rt = itemRects[i];
            Vector3 itemCenterWorld = rt.TransformPoint(rt.rect.center);

            float d = (itemCenterWorld - viewportCenterWorld).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestIdx = i;
            }
        }

        return bestIdx;
    }

    private Vector2 GetAnchoredPosToCenterItem(int idx)
    {
        Vector3 viewportCenterWorld = viewport.TransformPoint(viewport.rect.center);
        Vector3 itemCenterWorld = itemRects[idx].TransformPoint(itemRects[idx].rect.center);

        Vector3 deltaWorld = viewportCenterWorld - itemCenterWorld;
        Vector3 deltaLocal = content.InverseTransformVector(deltaWorld);

        return content.anchoredPosition + new Vector2(deltaLocal.x, 0f);
    }

    private void ApplyFocused(int idx)
{
    if (idx == currentIndex) return;
    currentIndex = idx;

    if (itemUIs != null && idx >= 0 && idx < itemUIs.Count)
    {
        OnCenteredChanged?.Invoke(itemUIs[idx].Data);
    }

    if (highlightFocused && itemUIs != null)
    {
        for (int i = 0; i < itemUIs.Count; i++)
            itemUIs[i].SetFocused(i == idx);
    }
}

}
