using System;
using UnityEngine;

public class BuildingOnRoad : MonoBehaviour
{
    public BuildingData data;

    [Header("Runtime")]
    public int level = 1;
    public long lastClaimUnix = 0;

    public bool IsActive { get; private set; }

    public void SetActiveOnRoad(bool active)
    {
        SetActiveOnRoad(active, false);
    }

    public void SetActiveOnRoad(bool active, bool resetTimerWhenActivate)
    {
        IsActive = active;

        if (active)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (resetTimerWhenActivate)
                lastClaimUnix = now;

            if (lastClaimUnix <= 0)
                lastClaimUnix = now;
        }

        if (active && data != null)
        level = BuildingLevelStore.GetLevel(data.buildingID, 1);

    }

    public bool IsMaxLevel => (data != null && level >= data.maxLevel);

    public int GetRewardPerCycle()
    {
        if (data == null) return 0;
        return data.CalcRewardAmount(level);
    }

    public long GetUpgradeCost()
    {
        if (data == null) return 0;
        return data.CalcUpgradeCost(level);
    }

    public bool TryUpgrade()
    {
        if (data == null) return false;
        if (level >= data.maxLevel) return false;

        long cost = GetUpgradeCost();
        if (cost > 0)
        {
            if (RoseWallet.Instance == null) return false;
            if (RoseWallet.Instance.CurrentRose < cost) return false;
            if (!RoseWallet.Instance.SpendRose(cost)) return false;
        }

        level++;
        GameSaveManager.Instance?.RequestSave();
        return true;
    }
}
