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
    }
}
