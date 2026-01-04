using System;
using UnityEngine;

public class BuildingOnRoad : MonoBehaviour
{
    public BuildingData data;
    public int roadIndex; 

    [Header("Runtime")]
    public int level = 1;
    public long lastClaimUnix = 0;

    public bool IsActive { get; private set; }

    void Awake()
    {
        if (data == null) return;
        level = BuildingLevelStore.GetLevel(GetSaveKey(), 1);
    }

    string GetSaveKey()
    {
        return $"{roadIndex}_{data.buildingID}";
    }

    public void SetActiveOnRoad(bool active, bool resetTimerWhenActivate = false)
    {
        IsActive = active;

        if (!active) return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (resetTimerWhenActivate || lastClaimUnix <= 0)
            lastClaimUnix = now;
    }

    public bool TryUpgrade()
    {
        if (data == null) return false;
        if (level >= data.maxLevel) return false;

        long cost = data.CalcUpgradeCost(level);
        if (cost > 0)
        {
            if (RoseWallet.Instance == null) return false;
            if (RoseWallet.Instance.CurrentRose < cost) return false;
            if (!RoseWallet.Instance.SpendRose(cost)) return false;
        }

        level++;
        BuildingLevelStore.SetLevel(GetSaveKey(), level);
        GameSaveManager.Instance?.RequestSave();
        return true;
    }
}
