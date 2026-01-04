using UnityEngine;

public static class ActionCostStore
{
    // ===== Keys =====
    const string KEY_ADD_COST = "COST_ADD";
    const string KEY_MERGE_COST = "COST_MERGE";
    const string KEY_ADD_COUNT = "COST_ADD_COUNT";
    const string KEY_MERGE_COUNT = "COST_MERGE_COUNT";

    // ===== Base =====
    const long ADD_START_COST = 10;
    const long MERGE_START_COST = 50;

    const int ADD_TIER1_END = 50;
    const int ADD_TIER2_END = 200;

    const float ADD_T1_MIN = 0.02f;
    const float ADD_T1_MAX = 0.03f;

    const float ADD_T2_MIN = 0.04f;
    const float ADD_T2_MAX = 0.06f;

    const float ADD_T3_MIN = 0.07f;
    const float ADD_T3_MAX = 0.10f;

    const int MERGE_TIER1_END = 30;
    const int MERGE_TIER2_END = 120;

    const float MERGE_T1_MIN = 0.04f;
    const float MERGE_T1_MAX = 0.05f;

    const float MERGE_T2_MIN = 0.06f;
    const float MERGE_T2_MAX = 0.08f;

    const float MERGE_T3_MIN = 0.09f;
    const float MERGE_T3_MAX = 0.12f;

    const double ADD_BASE_EXP = 1.06;
    const double MERGE_BASE_EXP = 1.08;

    // ===== Public API =====

    public static long GetAddCost()
    {
        int n = GetAddCount();
        return CalcAddCost(n);
    }

    public static long GetMergeCost()
    {
        int n = GetMergeCount();
        return CalcMergeCost(n);
    }

    public static void IncreaseAddCost()
    {
        int n = GetAddCount() + 1;
        PlayerPrefs.SetInt(KEY_ADD_COUNT, n);

        long next = CalcAddCost(n);
        SaveLong(KEY_ADD_COST, next);
        PlayerPrefs.Save();
    }

    public static void IncreaseMergeCost()
    {
        int n = GetMergeCount() + 1;
        PlayerPrefs.SetInt(KEY_MERGE_COUNT, n);

        long next = CalcMergeCost(n);
        SaveLong(KEY_MERGE_COST, next);
        PlayerPrefs.Save();
    }

    public static int GetAddCount() => PlayerPrefs.GetInt(KEY_ADD_COUNT, 0);
    public static int GetMergeCount() => PlayerPrefs.GetInt(KEY_MERGE_COUNT, 0);

    public static void SetAddCost(long value)
    {
        SaveLong(KEY_ADD_COST, (long)Mathf.Max(0, value));
        PlayerPrefs.Save();
    }

    public static void SetMergeCost(long value)
    {
        SaveLong(KEY_MERGE_COST, (long)Mathf.Max(0, value));
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(KEY_ADD_COST);
        PlayerPrefs.DeleteKey(KEY_MERGE_COST);
        PlayerPrefs.DeleteKey(KEY_ADD_COUNT);
        PlayerPrefs.DeleteKey(KEY_MERGE_COUNT);
        PlayerPrefs.Save();
    }

    // ===== Core: cost curve =====

    static long CalcAddCost(int count)
    {
        count = Mathf.Max(0, count);

        double baseCost = ADD_START_COST * System.Math.Pow(ADD_BASE_EXP, count);

        float pct = PickTierPct(
            count,
            ADD_TIER1_END, ADD_TIER2_END,
            ADD_T1_MIN, ADD_T1_MAX,
            ADD_T2_MIN, ADD_T2_MAX,
            ADD_T3_MIN, ADD_T3_MAX
        );

        double next = baseCost * (1.0 + pct);

        long rounded = (long)System.Math.Ceiling(next);
        if (rounded <= 0) rounded = 1;
        return rounded;
    }

    static long CalcMergeCost(int count)
    {
        count = Mathf.Max(0, count);

        double baseCost = MERGE_START_COST * System.Math.Pow(MERGE_BASE_EXP, count);

        float pct = PickTierPct(
            count,
            MERGE_TIER1_END, MERGE_TIER2_END,
            MERGE_T1_MIN, MERGE_T1_MAX,
            MERGE_T2_MIN, MERGE_T2_MAX,
            MERGE_T3_MIN, MERGE_T3_MAX
        );

        double next = baseCost * (1.0 + pct);

        long rounded = (long)System.Math.Ceiling(next);
        if (rounded <= 0) rounded = 1;
        return rounded;
    }

    static float PickTierPct(
        int count,
        int tier1End, int tier2End,
        float t1Min, float t1Max,
        float t2Min, float t2Max,
        float t3Min, float t3Max
    )
    {
        if (count < tier1End) return Random.Range(t1Min, t1Max);
        if (count < tier2End) return Random.Range(t2Min, t2Max);
        return Random.Range(t3Min, t3Max);
    }

    // ===== PlayerPrefs helpers =====

    static void SaveLong(string key, long value)
    {
        if (value > int.MaxValue)
            PlayerPrefs.SetString(key, value.ToString());
        else
            PlayerPrefs.SetInt(key, (int)value);
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
}
