using System;
using UnityEngine;

[Serializable]
public class CharacterPhotoEntry
{
    public string photoId;

    public Sprite photo;
    [TextArea(2, 4)] public string comment;

    public int requiredLevel;
}
