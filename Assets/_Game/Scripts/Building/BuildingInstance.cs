using UnityEngine;

public class BuildingInstance : MonoBehaviour
{
    public BuildingData data { get; private set; }
    public int Level { get; private set; } = 1;
    public long LastClaimUnix { get; private set; }

    public void Init(BuildingData d, int level, long lastClaimUnix)
    {
        data = d;
        Level = Mathf.Max(1, level);
        LastClaimUnix = lastClaimUnix;
    }

    public void SetLastClaim(long unix) => LastClaimUnix = unix;

    public int GetRewardAmount()
    {
        float mul = Mathf.Pow(data.amountMultiplierPerLevel, Level - 1);
        return Mathf.Max(1, Mathf.RoundToInt(data.baseAmount * mul));
    }

    public int GetUpgradeCost()
    {
        float mul = Mathf.Pow(data.costMultiplierPerLevel, Level - 1);
        return Mathf.Max(1, Mathf.RoundToInt(data.baseUpgradeCost * mul));
    }

    public bool CanUpgrade() => data != null && Level < data.maxLevel;
    public void Upgrade() => Level = Mathf.Min(data.maxLevel, Level + 1);
}
