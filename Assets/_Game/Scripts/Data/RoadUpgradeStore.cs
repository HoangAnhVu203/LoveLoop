using UnityEngine;

public static class RoadUpgradeStore
{
    const string KEY_UNLOCKED_ROADS = "UNLOCKED_ROADS";      
    const string KEY_UPGRADE_COUNT = "ROAD_UPGRADE_COUNT";  

    public static int GetUnlockedRoadCount()
    {
        return Mathf.Max(1, PlayerPrefs.GetInt(KEY_UNLOCKED_ROADS, 1));
    }

    public static int GetUpgradeCount()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(KEY_UPGRADE_COUNT, 0));
    }

    // Lần 1: 30, lần 2: 60, lần 3: 90...
    public static long GetNextUpgradeCost()
    {
        int next = GetUpgradeCount() + 1;
        return next * 30L;
    }

    public static void MarkUpgraded()
    {
        int upgrades = GetUpgradeCount() + 1;
        PlayerPrefs.SetInt(KEY_UPGRADE_COUNT, upgrades);

        int unlocked = GetUnlockedRoadCount() + 1;
        PlayerPrefs.SetInt(KEY_UNLOCKED_ROADS, unlocked);

        PlayerPrefs.Save();
    }
}
