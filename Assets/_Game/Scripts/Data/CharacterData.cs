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
}
