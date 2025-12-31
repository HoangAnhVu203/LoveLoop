using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelBuilding : UICanvas
{
    [Header("Scroll")]
    [SerializeField] private ScrollSnapToCenter snap;
    [SerializeField] private RectTransform content;
    [SerializeField] private BuildingThumbItemUI itemPrefab;

    [Header("Data")]
    [SerializeField] private List<BuildingData> buildings = new();

    [Header("Info Panel (OUTSIDE items)")]
    [SerializeField] private Text levelText;
    [SerializeField] private Text desText;

    [Header("Upgrade UI")]
    [SerializeField] private Button upgradeBtn;
    [SerializeField] private Text productionText;

    Coroutine _initCR;
    int _focusedIndex;

    readonly List<BuildingThumbItemUI> _items = new();

    void OnEnable()
    {
        if (_initCR != null) StopCoroutine(_initCR);
            _initCR = StartCoroutine(InitCR());
                BuildList();

        if (snap != null)
        {
            snap.OnCenteredIndexChanged -= OnCenteredIndexChanged;
            snap.OnCenteredIndexChanged += OnCenteredIndexChanged;
        }
        if (upgradeBtn != null)
        {
            upgradeBtn.onClick.RemoveAllListeners();
            upgradeBtn.onClick.AddListener(UpgradeFocusedBuilding);
        }

        _focusedIndex = 0;

        snap?.SnapToIndex(0, false);
        OnCenteredIndexChanged(0);
    }

    void OnDisable()
    {
        if (snap != null)
            snap.OnCenteredIndexChanged -= OnCenteredIndexChanged;

        if (_initCR != null) StopCoroutine(_initCR);
            _initCR = null;

        if (snap != null)
            snap.OnCenteredIndexChanged -= OnCenteredIndexChanged;
    }

    void BuildList()
    {
        if (content == null || itemPrefab == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        _items.Clear();

        for (int i = 0; i < buildings.Count; i++)
        {
            var it = Instantiate(itemPrefab, content);
            it.Bind(buildings[i]);
            _items.Add(it);
        }

        if (snap != null)
        {
            var rects = new List<RectTransform>(_items.Count);
            for (int i = 0; i < _items.Count; i++)
                rects.Add((RectTransform)_items[i].transform);

            snap.SetItems(rects);
        }
    }
    
    public void DimClick()
    {
        gameObject.SetActive(false);
    }

    void OnCenteredIndexChanged(int idx)
    {
        _focusedIndex = idx;

        if (idx < 0 || idx >= _items.Count) return;
        var item = _items[idx];
        if (item == null || item.Data == null) return;

        var data = item.Data;

        if (desText != null) desText.text = data.buildingDes;

        int lv = item.RuntimeLevel;
        if (levelText != null) levelText.text = $"Level {lv}";

        if (productionText != null)
            productionText.text = data.GetRewardText(lv);
    }

    void UpgradeFocusedBuilding()
    {
        if (_focusedIndex < 0 || _focusedIndex >= _items.Count) return;

        var item = _items[_focusedIndex];
        if (item == null) return;

        if (item.TryUpgradeRuntime())
        {
            OnCenteredIndexChanged(_focusedIndex);
        }
    }

    IEnumerator InitCR()
    {
        BuildList();

        if (snap != null)
        {
            snap.OnCenteredIndexChanged -= OnCenteredIndexChanged;
            snap.OnCenteredIndexChanged += OnCenteredIndexChanged;
        }

        if (upgradeBtn != null)
        {
            upgradeBtn.onClick.RemoveAllListeners();
            upgradeBtn.onClick.AddListener(UpgradeFocusedBuilding);
        }

        var sr = snap != null ? snap.GetComponent<ScrollRect>() : null;
        if (sr != null)
        {
            sr.StopMovement();
            sr.velocity = Vector2.zero;
            sr.normalizedPosition = new Vector2(0f, 0f); 
        }

        yield return null;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        _focusedIndex = 0;
        snap?.SnapToIndex(0, false);
        OnCenteredIndexChanged(0);

        _initCR = null;
    }

    
}
