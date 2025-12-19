using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollCenterSelector : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] RectTransform viewport;
    [SerializeField] RectTransform content;
    [SerializeField] CharacterInfoPanelUI infoPanel;

    [Header("Items")]
    [SerializeField] List<CharacterThumbItemUI> items = new();

    [Header("Tuning")]
    [Tooltip("Chỉ update khi thay đổi index để tránh spam UI")]
    [SerializeField] bool onlyUpdateOnChange = true;

    int currentIndex = -1;

    void Reset()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            viewport = scrollRect.viewport;
            content = scrollRect.content;
        }
    }

    void Update()
    {
        if (items == null || items.Count == 0) return;

        int idx = FindClosestToCenter();
        if (onlyUpdateOnChange && idx == currentIndex) return;

        SetCurrentIndex(idx);
    }

    int FindClosestToCenter()
    {
        // Tâm viewport trong world space
        Vector3 viewportCenterWorld = viewport.TransformPoint(viewport.rect.center);

        float best = float.MaxValue;
        int bestIndex = 0;

        for (int i = 0; i < items.Count; i++)
        {
            var rt = (RectTransform)items[i].transform;
            Vector3 itemCenterWorld = rt.TransformPoint(rt.rect.center);

            float d = Vector3.SqrMagnitude(itemCenterWorld - viewportCenterWorld);
            if (d < best)
            {
                best = d;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    void SetCurrentIndex(int idx)
    {
        currentIndex = idx;

        // update info panel
        infoPanel.Show(items[idx].Data);

        // highlight
        for (int i = 0; i < items.Count; i++)
            items[i].SetFocused(i == currentIndex);
    }

    public void SetItems(List<CharacterThumbItemUI> newItems)
    {
        items = newItems;
        currentIndex = -1;
        if (items != null && items.Count > 0)
            SetCurrentIndex(FindClosestToCenter());
    }
}
