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

    [Header("Items")]
    [SerializeField] private List<RectTransform> itemRects = new();

    [Header("Legacy (for Character list)")]
    [SerializeField] private List<CharacterThumbItemUI> legacyItemUIs = new();

    [Header("Snap Settings")]
    [SerializeField] private float snapDuration = 0.18f;
    [SerializeField] private float snapThreshold = 5f;
    [SerializeField] private bool updateWhileDragging = true;

    [Header("Layout")]
    [SerializeField] private bool forceRebuildLayoutBeforeSnap = true;

    // NEW: event theo index (dùng cho Building list)
    public System.Action<int> OnCenteredIndexChanged;

    // LEGACY: event theo CharacterData (dùng cho FlirtBook cũ)
    public System.Action<CharacterData> OnCenteredChanged;

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
        SnapToClosest();
    }

    // ===================== PUBLIC API =====================

    // Dùng cho Building list (RectTransform)
    public void SetItems(List<RectTransform> rects)
    {
        legacyItemUIs = null;

        itemRects = rects != null ? new List<RectTransform>(rects) : new List<RectTransform>();
        currentIndex = -1;

        RebuildIfNeeded();
        RefreshImmediate();
        SnapToClosest();
    }

    // Dùng cho FlirtBook list (cũ)
    public void SetItems(List<CharacterThumbItemUI> uis)
    {
        legacyItemUIs = uis;

        itemRects = new List<RectTransform>();
        if (uis != null)
        {
            for (int i = 0; i < uis.Count; i++)
                if (uis[i] != null) itemRects.Add((RectTransform)uis[i].transform);
        }

        currentIndex = -1;

        RebuildIfNeeded();
        RefreshImmediate();
        SnapToClosest();
    }

    // Bấm chọn item => auto vào giữa
    public void SnapToIndex(int idx, bool animated = true)
    {
        if (itemRects == null || itemRects.Count == 0) return;

        idx = Mathf.Clamp(idx, 0, itemRects.Count - 1);

        RebuildIfNeeded();

        Vector2 target = GetAnchoredPosToCenterItem(idx);

        StopSnap();

        scrollRect.StopMovement();
        scrollRect.velocity = Vector2.zero;

        if (!animated)
        {
            content.anchoredPosition = target;
            ApplyFocused(idx);
            return;
        }

        snapCR = StartCoroutine(SnapCoroutine(target, idx));
    }

    // ===================== DRAG EVENTS =====================

    public void OnBeginDrag(PointerEventData eventData)
    {
        StopSnap();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (updateWhileDragging)
            RefreshImmediate();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SnapToClosest();
    }

    // ===================== CORE =====================

    private void SnapToClosest()
    {
        if (itemRects == null || itemRects.Count == 0) return;

        RebuildIfNeeded();

        int idx = FindClosestIndexToCenter();
        Vector2 target = GetAnchoredPosToCenterItem(idx);

        if (Vector2.Distance(content.anchoredPosition, target) <= snapThreshold)
        {
            ApplyFocused(idx);
            return;
        }

        StopSnap();

        scrollRect.StopMovement();
        scrollRect.velocity = Vector2.zero;

        snapCR = StartCoroutine(SnapCoroutine(target, idx));
    }

    private IEnumerator SnapCoroutine(Vector2 targetAnchoredPos, int targetIndex)
    {
        scrollRect.StopMovement();
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
        Vector3 viewportCenterWorld = WorldCenter(viewport);

        float best = float.MaxValue;
        int bestIdx = 0;

        for (int i = 0; i < itemRects.Count; i++)
        {
            RectTransform rt = itemRects[i];
            if (rt == null) continue;

            Vector3 itemCenterWorld = WorldCenter(rt);
            float d = (itemCenterWorld - viewportCenterWorld).sqrMagnitude;

            if (d < best)
            {
                best = d;
                bestIdx = i;
            }
        }

        return bestIdx;
    }

    // Quan trọng: tính delta theo local của CONTENT để snap chuẩn
    private Vector2 GetAnchoredPosToCenterItem(int idx)
    {
        Vector3 viewportCenterWorld = WorldCenter(viewport);
        Vector3 itemCenterWorld = WorldCenter(itemRects[idx]);

        Vector3 viewportCenterLocal = content.InverseTransformPoint(viewportCenterWorld);
        Vector3 itemCenterLocal = content.InverseTransformPoint(itemCenterWorld);

        float deltaX = viewportCenterLocal.x - itemCenterLocal.x;
        return content.anchoredPosition + new Vector2(deltaX, 0f);
    }

    private void ApplyFocused(int idx)
    {
        if (idx == currentIndex) return;
        currentIndex = idx;

        // event kiểu index (building list)
        OnCenteredIndexChanged?.Invoke(idx);

        // legacy event kiểu CharacterData (flirt book)
        if (OnCenteredChanged != null && legacyItemUIs != null && idx >= 0 && idx < legacyItemUIs.Count)
            OnCenteredChanged.Invoke(legacyItemUIs[idx].Data);
    }

    // ===================== UTIL =====================

    private void RebuildIfNeeded()
    {
        if (!forceRebuildLayoutBeforeSnap) return;
        if (content == null) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private static Vector3 WorldCenter(RectTransform rt)
    {
        Vector3[] c = new Vector3[4];
        rt.GetWorldCorners(c);
        return (c[0] + c[2]) * 0.5f;
    }
}
