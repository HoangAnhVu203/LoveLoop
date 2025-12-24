using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadManager : Singleton<RoadManager>
{
    [Header("Road prefabs")]
    public List<GameObject> roadPrefabs = new();
    public Transform roadRoot;

    [Header("Refs")]
    public HeartChainManager chain;

    [Header("Rule")]
    public int maxGatesPerRoad = 3;

    int _index = 0;
    readonly List<GameObject> _instances = new();

    readonly Dictionary<int, int> _gateCountByRoad = new();

    void Awake()
    {
        if (chain == null) chain = FindObjectOfType<HeartChainManager>();
        if (roadRoot == null) roadRoot = transform;
    }

    void Start()
    {
        for (int i = 0; i < roadPrefabs.Count; i++)
        {
            var go = Instantiate(roadPrefabs[i], roadRoot);
            go.name = roadPrefabs[i].name + "_Instance";
            go.SetActive(false);
            _instances.Add(go);
        }

        StartCoroutine(SwitchRoadCR(0));
        IsReady = true;

    }

    public int CurrentRoadIndex => _index;

    public int GetGateCountOnCurrentRoad()
    {
        _gateCountByRoad.TryGetValue(_index, out int c);
        return c;
    }

    public bool CanAddGateOnCurrentRoad()
    {
        return GetGateCountOnCurrentRoad() < maxGatesPerRoad;
    }

    public void NotifyGateSpawnedOnCurrentRoad()
    {
        int c = GetGateCountOnCurrentRoad();
        _gateCountByRoad[_index] = c + 1;
    }

    public void NextRoad()
    {
        if (_instances.Count == 0) return;

        int unlocked = Mathf.Min(RoadUpgradeStore.GetUnlockedRoadCount(), _instances.Count);
        if (unlocked <= 1)
        {
            return;
        }

        _index = (_index + 1) % unlocked;
        StartCoroutine(SwitchRoadCR(_index));
    }

    public bool TryUpgradeRoad()
    {
        if (RoseWallet.Instance == null) return false;

        int unlocked = RoadUpgradeStore.GetUnlockedRoadCount();
        if (unlocked >= _instances.Count)
        {
            Debug.Log("[RoadManager] All roads already unlocked.");
            return false;
        }

        long cost = RoadUpgradeStore.GetNextUpgradeCost();
        if (RoseWallet.Instance.CurrentRose < cost)
        {
            Debug.Log($"[RoadManager] Not enough Rose to upgrade road. Need {cost}");
            return false;
        }

        if (!RoseWallet.Instance.SpendRose(cost))
            return false;

        RoadUpgradeStore.MarkUpgraded();

        int newIndex = Mathf.Min(unlocked, _instances.Count - 1);
        _index = newIndex;
        StartCoroutine(SwitchRoadCR(_index));

        Debug.Log($"[RoadManager] Upgraded road. Cost={cost}, now unlocked={RoadUpgradeStore.GetUnlockedRoadCount()}");
        GameSaveManager.Instance?.RequestSave();
        return true;
    }

    IEnumerator SwitchRoadCR(int idx)
    {
        for (int i = 0; i < _instances.Count; i++)
            _instances[i].SetActive(i == idx);

        yield return null;

        var spline = _instances[idx].GetComponentInChildren<SplinePath>(true);
        if (spline == null)
        {
            Debug.LogError("[RoadManager] Road thiếu SplinePath");
            yield break;
        }

        spline.Rebuild();

        if (chain != null)
            chain.SetSplinePathKeepState(spline);

        if (GateManager.Instance != null)
            GateManager.Instance.OnRoadChanged(spline);

        Debug.Log($"[RoadManager] Switched to road {idx}: {_instances[idx].name}");
        GameSaveManager.Instance?.RequestSave();
    }

    public int GetTotalGateCountAllRoads()
    {
        int sum = 0;
        foreach (var kv in _gateCountByRoad)
            sum += kv.Value;
        return sum;
    }

    public int GetTotalGateCap()
    {
        int unlocked = RoadUpgradeStore.GetUnlockedRoadCount(); 
        return unlocked * maxGatesPerRoad; 
    }

    public bool IsReady { get; private set; }

    public Dictionary<int, int> ExportRoadGateCounts()
    {
        return new Dictionary<int, int>(_gateCountByRoad);
    }

    public void ImportRoadGateCounts(System.Collections.Generic.List<RoadGateCountSave> list)
    {
        _gateCountByRoad.Clear();
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
            _gateCountByRoad[list[i].roadIndex] = Mathf.Max(0, list[i].count);
    }

    public void SwitchToRoadImmediate(int idx)
    {
        if (_instances.Count == 0) return;

        int unlocked = Mathf.Min(RoadUpgradeStore.GetUnlockedRoadCount(), _instances.Count);
        idx = Mathf.Clamp(idx, 0, Mathf.Max(0, unlocked - 1));

        _index = idx;
        StopAllCoroutines();
        StartCoroutine(SwitchRoadCR(_index));
    }


    

}
