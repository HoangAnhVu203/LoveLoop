using UnityEngine;

public enum BuildingType { Florist, Bank, Factory, Hotel }
public enum RewardType { Flower, Money, Boost5s, Heart }

[CreateAssetMenu(fileName = "BuildingData", menuName = "Game/BuildingData")]
public class BuildingData : ScriptableObject
{
    public string buildingID;
    public string buildingName;
    public Sprite buildingIMG;
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
    public int baseUpgradeCost = 10;
    public float costMultiplierPerLevel = 1.35f;
}
