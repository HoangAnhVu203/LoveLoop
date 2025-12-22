using UnityEngine;

public static class PhotoLikeStore
{
    // key = "LIKE_<characterId>_<photoId>"
    static string MakeKey(string characterId, string photoId)
        => $"LIKE_{characterId}_{photoId}";

    public static bool IsLiked(string characterId, string photoId)
    {
        return PlayerPrefs.GetInt(MakeKey(characterId, photoId), 0) == 1;
    }

    public static void SetLiked(string characterId, string photoId, bool liked)
    {
        PlayerPrefs.SetInt(MakeKey(characterId, photoId), liked ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool Toggle(string characterId, string photoId)
    {
        bool newValue = !IsLiked(characterId, photoId);
        SetLiked(characterId, photoId, newValue);
        return newValue;
    }
}
