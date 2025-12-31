using System.Collections.Generic;
using UnityEngine;

public static class BuildingLevelStore
{
    const string KEY = "BUILDING_LEVELS_V1";

    [System.Serializable]
    class Entry { public string id; public int level; }

    [System.Serializable]
    class Wrap { public List<Entry> list = new(); }

    static Dictionary<string,int> _cache;

    static void EnsureLoaded()
    {
        if (_cache != null) return;
        _cache = new Dictionary<string,int>();

        string json = PlayerPrefs.GetString(KEY, "");
        if (string.IsNullOrEmpty(json)) return;

        var w = JsonUtility.FromJson<Wrap>(json);
        if (w?.list == null) return;

        foreach (var e in w.list)
            if (!string.IsNullOrEmpty(e.id))
                _cache[e.id] = Mathf.Max(1, e.level);
    }

    static void Save()
    {
        var w = new Wrap();
        foreach (var kv in _cache)
            w.list.Add(new Entry { id = kv.Key, level = kv.Value });

        PlayerPrefs.SetString(KEY, JsonUtility.ToJson(w));
        PlayerPrefs.Save();
    }

    public static int GetLevel(string buildingID, int defaultLevel = 1)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(buildingID)) return defaultLevel;
        return _cache.TryGetValue(buildingID, out var lv) ? lv : defaultLevel;
    }

    public static void SetLevel(string buildingID, int level)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(buildingID)) return;
        _cache[buildingID] = Mathf.Max(1, level);
        Save();
    }
}
