using UnityEngine;

public enum BuildingType { Florist, Bank, Factory, Hotel }
public enum RewardType { Flower, Money, Boost5s, Heart }

[CreateAssetMenu(fileName = "BuildingData", menuName = "Game/BuildingData")]
public class BuildingData : ScriptableObject
{
    public string buildingID;
    public string buildingName;
    public Sprite buildingIMG;
    public int buildingLevel;
    [TextArea] public string buildingDes;

    public BuildingType type;

    [Header("Production")]
    public float intervalSeconds = 180f; 
    public RewardType rewardType;

    [Header("Base Reward (level 1)")]
    public int baseAmount = 1;

    [Header("Growth per level")]
    public float amountMultiplierPerLevel = 2f;
    public int maxLevel = 3;

    [Header("Upgrade Cost")]
    public int baseUpgradeCost = 0;
    public float costMultiplierPerLevel = 0;

    // ===== Helpers =====
    public int CalcRewardAmount(int level)
    {
        level = Mathf.Clamp(level, 1, maxLevel);
        int mul = 1 << (level - 1);
        return Mathf.Max(1, baseAmount * mul);
    }

    public long CalcUpgradeCost(int currentLevel)
    {
        int next = Mathf.Clamp(currentLevel + 1, 1, maxLevel);
        if (next <= 1) return baseUpgradeCost;

        float mul = (costMultiplierPerLevel <= 0f) ? 1f : Mathf.Pow(costMultiplierPerLevel, next - 2);
        return Mathf.Max(0, Mathf.RoundToInt(baseUpgradeCost * mul));
    }
     public string GetRewardText(int level)
    {
        int amount = CalcRewardAmount(level);

        switch (rewardType)
        {
            case RewardType.Flower:  return $"+ {amount} rose";
            case RewardType.Money:   return $"+ {amount} coin";
            case RewardType.Heart:   return $"+ {amount} heart";
            case RewardType.Boost5s: return $"+ {amount * 5}s boost";
            default:                 return $"+ {amount}";
        }
    }
}
