using System;
using Spine.Unity;
using UnityEngine;

[Serializable]
public class CharacterPhotoEntry
{
    public string photoId;
    public Sprite photo;
    public SkeletonDataAsset spineAsset;     
    public string loopAnimation = "animation";    
    public bool loop = true;
    public Sprite lockedPhoto;
    [TextArea(2, 4)] public string comment;
    public int requiredLevel;
}
