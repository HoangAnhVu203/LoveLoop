using System;
using UnityEngine;

[Serializable]
public class CharacterPhotoEntry
{
    public Sprite photo;
    [TextArea(2, 4)] public string comment;
}
