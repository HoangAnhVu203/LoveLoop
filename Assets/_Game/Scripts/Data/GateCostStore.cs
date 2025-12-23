using UnityEngine;

public static class GateCostStore
{
    const string KEY_GATE_COUNT = "GATE_PURCHASED_COUNT";
    const string KEY_LAST_COST = "GATE_LAST_COST";

    // fixed costs for gate #1..#4
    static readonly long[] fixedCosts = { 1, 5, 15, 20 };

    public static int GetPurchasedGateCount()
        => PlayerPrefs.GetInt(KEY_GATE_COUNT, 0);

    public static long GetNextGateCost()
    {
        int count = GetPurchasedGateCount();   
        int nextIndex = count + 1;             

        if (nextIndex <= 4)
            return fixedCosts[nextIndex - 1];

        // gate #5+ : lastCost * 1.2
        long lastCost = PlayerPrefs.GetInt(KEY_LAST_COST, (int)fixedCosts[3]); 
        return CeilToLong(lastCost * 1.2f);
    }

    public static void MarkGatePurchased()
    {
        int count = GetPurchasedGateCount();
        long costJustPaid = GetNextGateCost();

        count++;
        PlayerPrefs.SetInt(KEY_GATE_COUNT, count);

        PlayerPrefs.SetInt(KEY_LAST_COST, (int)Mathf.Clamp(costJustPaid, 0, int.MaxValue));

        PlayerPrefs.Save();
    }

    public static void ResetForTest()
    {
        PlayerPrefs.DeleteKey(KEY_GATE_COUNT);
        PlayerPrefs.DeleteKey(KEY_LAST_COST);
        PlayerPrefs.Save();
    }

    static long CeilToLong(float v)
    {
        if (v <= 0f) return 0;
        return (long)Mathf.CeilToInt(v);
    }
}
