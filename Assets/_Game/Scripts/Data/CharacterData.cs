using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterId;
    public string characterName;
    public Sprite avatar;
    public string description;

    public int level;
    public long levelUpCost;
    public float revenueMultiplier;

    public List<CharacterPhotoEntry> photos = new List<CharacterPhotoEntry>(4);

    [Header("Level Icons (index 0 = LV1)")]
    public List<Sprite> levelIcons = new List<Sprite>(12);

    public Sprite GetLevelIcon(int lv)
    {
        if (levelIcons == null || levelIcons.Count == 0) return null;

        int idx = Mathf.Clamp(lv, 1, levelIcons.Count) - 1;
        return levelIcons[idx];
    }
}
