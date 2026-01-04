using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelBuilding : UICanvas
{
    [Header("Scroll")]
    [SerializeField] private ScrollSnapToCenter snap;
    [SerializeField] private RectTransform content;
    [SerializeField] private BuildingThumbItemUI itemPrefab;

    [Header("Info Panel (OUTSIDE items)")]
    [SerializeField] private Text levelText;
    [SerializeField] private Text desText;

    [Header("Upgrade UI")]
    [SerializeField] private Button upgradeBtn;
    [SerializeField] private Text productionText;

    private List<BuildingOnRoad> _roadBuildings = new();
    private readonly List<BuildingThumbItemUI> _items = new();

    private Coroutine _initCR;
    private int _focusedIndex;

    void OnEnable()
    {
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

        if (_initCR != null) StopCoroutine(_initCR);
        _initCR = StartCoroutine(InitCR());
    }

    void OnDisable()
    {
        if (_initCR != null)
        {
            StopCoroutine(_initCR);
            _initCR = null;
        }

        if (snap != null)
            snap.OnCenteredIndexChanged -= OnCenteredIndexChanged;
    }

    public void DimClick()
    {
        gameObject.SetActive(false);
    }

    IEnumerator InitCR()
    {
        BuildList();

        yield return null;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        var sr = snap != null ? snap.GetComponent<ScrollRect>() : null;
        if (sr != null)
        {
            sr.StopMovement();
            sr.velocity = Vector2.zero;
        }

        _focusedIndex = (_items.Count > 0) ? 0 : -1;

        if (_focusedIndex >= 0)
        {
            snap?.SnapToIndex(_focusedIndex, false);
            OnCenteredIndexChanged(_focusedIndex);
        }
        else
        {
            if (desText != null) desText.text = "";
            if (levelText != null) levelText.text = "";
            if (productionText != null) productionText.text = "";
        }

        _initCR = null;
    }

    void BuildList()
    {
        if (content == null || itemPrefab == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        _items.Clear();

        _roadBuildings = RoadManager.Instance != null
            ? RoadManager.Instance.GetBuildingsOnCurrentRoad(true)
            : new List<BuildingOnRoad>();

        for (int i = 0; i < _roadBuildings.Count; i++)
        {
            var it = Instantiate(itemPrefab, content);
            it.Bind(_roadBuildings[i]);    
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

    void OnCenteredIndexChanged(int idx)
    {
        _focusedIndex = idx;

        if (idx < 0 || idx >= _items.Count) return;

        var b = _items[idx].Building;
        if (b == null || b.data == null) return;

        if (desText != null) desText.text = b.data.buildingDes;
        if (levelText != null) levelText.text = $"Level {b.level}";

        if (productionText != null)
        {
            int amount = b.data.CalcRewardAmount(b.level);

            switch (b.data.rewardType)
            {
                case RewardType.Flower: productionText.text = $"+{amount} Rose "; break;
                case RewardType.Money: productionText.text = $"+ $ {amount} Money "; break;
                case RewardType.Heart: productionText.text = $"+{amount} Heart "; break;
                case RewardType.Boost5s:
                    productionText.text = $"+{amount * 5}s Boost ";
                    break;
            }
        }
    }

    void UpgradeFocusedBuilding()
    {
        if (_focusedIndex < 0 || _focusedIndex >= _items.Count) return;

        var item = _items[_focusedIndex];
        if (item == null) return;

        if (item.TryUpgradeRuntime())
            OnCenteredIndexChanged(_focusedIndex);
    }
}
