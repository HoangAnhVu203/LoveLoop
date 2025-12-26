using UnityEngine;

public static class ActionCostStore
{
    const string KEY_ADD_COST   = "COST_ADD";
    const string KEY_MERGE_COST = "COST_MERGE";

    // Base
    const long ADD_START_COST = 10;
    const long MERGE_START_COST = 50;

    // Tăng % mỗi lần
    const float ADD_MIN_GROW = 0.02f; 
    const float ADD_MAX_GROW = 0.03f;  
    const float MERGE_MIN_GROW = 0.04f; 
    const float MERGE_MAX_GROW = 0.05f; 

    public static long GetAddCost()   => ReadLong(KEY_ADD_COST, ADD_START_COST);
    public static long GetMergeCost() => ReadLong(KEY_MERGE_COST, MERGE_START_COST);

    public static void IncreaseAddCost()
    {
        long current = GetAddCost();
        long next = Grow(current, ADD_MIN_GROW, ADD_MAX_GROW);
        SaveLong(KEY_ADD_COST, next);
    }

    public static void IncreaseMergeCost()
    {
        long current = GetMergeCost();
        long next = Grow(current, MERGE_MIN_GROW, MERGE_MAX_GROW);
        SaveLong(KEY_MERGE_COST, next);
    }

    static long Grow(long current, float minPct, float maxPct)
    {
        float pct = Random.Range(minPct, maxPct);
        double next = current * (1.0 + pct);

        long rounded = (long)System.Math.Round(next);
        if (rounded <= current) rounded = current + 1;

        return rounded;
    }

    static void SaveLong(string key, long value)
    {
        if (value > int.MaxValue)
        {
            PlayerPrefs.SetString(key, value.ToString());
        }
        else
        {
            PlayerPrefs.SetInt(key, (int)value);
        }
        PlayerPrefs.Save();
    }

    static long ReadLong(string key, long defaultValue)
    {
        if (PlayerPrefs.HasKey(key))
        {
            string s = PlayerPrefs.GetString(key, "");
            if (!string.IsNullOrEmpty(s) && long.TryParse(s, out long v)) return v;
            return PlayerPrefs.GetInt(key, (int)defaultValue);
        }
        return defaultValue;
    }

    public static void SetAddCost(long value)   => SaveLong(KEY_ADD_COST, (long)Mathf.Max(0, value));
    public static void SetMergeCost(long value) => SaveLong(KEY_MERGE_COST, (long)Mathf.Max(0, value));

}
