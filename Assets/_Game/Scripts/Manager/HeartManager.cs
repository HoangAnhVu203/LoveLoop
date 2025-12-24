using System.Collections.Generic;
using UnityEngine;

public class HeartManager : MonoBehaviour
{
    public static HeartManager Instance;

    [Header("Prefab mặc định khi Add")]
    public GameObject heartPrefab;
    public Transform spawnParent;
    public Transform center;
    public float followSmooth = 8f;

    [Header("Merge Settings")]
    public GameObject heartPinkPrefab;
    public GameObject heartLightBluePrefab;
    public int needCountToMerge = 3;

    [Header("Heart Prefabs By Level")]
    public List<GameObject> heartPrefabsByLevel;

    [Tooltip("Tên Layer dùng cho HeartPink ")]
    public string pinkLayerName = "HeartPink";

    public struct MergePreview
    {
        public bool canMerge;
        public HeartType tripleType;
        public GameObject resultPrefab;

        public Sprite tripleIcon;
        public Sprite resultIcon;
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (HeartUnlocks.Instance != null)
        {
            var chain = FindObjectOfType<HeartChainManager>();
            if (chain != null && chain.hearts != null)
            {
                foreach (var t in chain.hearts)
                {
                    if (t == null) continue;
                    var s = t.GetComponent<HeartStats>();
                    if (s != null) HeartUnlocks.Instance.MarkUnlocked(s.type);
                }
            }
        }
    }

    // ===================== ADD HEART =====================

    public void AddHeart()
    {
        if (!CanAddHeart())
        {
            Debug.Log("[AddHeart] Reached heart cap. Cannot add more.");
            return;
        }

        long cost = ActionCostStore.GetAddCost();
        if (PlayerMoney.Instance == null || !PlayerMoney.Instance.TrySpend(cost))
        {
            Debug.Log($"[AddHeart] Not enough money. Need ${cost}");
            return;
        }

        var manager = HeartChainManagerInstance;
        if (manager == null || manager.hearts.Count == 0)
            return;

        Transform last = manager.hearts[manager.hearts.Count - 1];
        if (last == null) return;

        int spawnLevel = GetAddableHeartLevel();

        int index = spawnLevel - 1;
        if (index < 0 || index >= heartPrefabsByLevel.Count)
        {
            Debug.LogError($"[AddHeart] Không có prefab cho level {spawnLevel}");
            return;
        }

        GameObject prefab = heartPrefabsByLevel[index];

        GameObject newHeart = Instantiate(prefab, last.position, last.rotation, spawnParent);
        newHeart.transform.localScale = last.localScale;

        var energy = newHeart.GetComponent<HeartWithEnergy>();
        if (energy != null) energy.enabled = false;

        manager.RegisterHeart(newHeart.transform);
        manager.RecalculateLeaderByWeight();
        manager.EnsureEnergyOnLeaderOnly();

        Debug.Log($"[AddHeart] Spawn heart level {spawnLevel} (cost={cost})");

        ActionCostStore.IncreaseAddCost();

        var panel = FindObjectOfType<PanelGamePlay>(true);
        if (panel != null) panel.Refresh();

        RoseWallet.Instance?.AddRose(1);

        GameSaveManager.Instance?.RequestSave();

    }

    // ===================== FIND MERGE TRIPLE =====================

    int FindFirstMergeTripleIndex(out HeartType foundType)
    {
        foundType = default;
        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts == null) return -1;

        var list = chain.hearts;
        int n = list.Count;
        if (n < 3) return -1;

        for (int i = 0; i <= n - 3; i++)
        {
            var s0 = list[i].GetComponent<HeartStats>();
            var s1 = list[i + 1].GetComponent<HeartStats>();
            var s2 = list[i + 2].GetComponent<HeartStats>();

            if (s0 == null || s1 == null || s2 == null) continue;

            if (s0.type == s1.type && s1.type == s2.type)
            {
                foundType = s0.type;
                return i;
            }
        }

        return -1;
    }

    HeartChainManager HeartChainManagerInstance => FindObjectOfType<HeartChainManager>();

    // ===================== MERGE =====================

    public void MergeAnyTriple()
    {
        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts == null)
            return;

        List<Transform> list = chain.hearts;
        int count = list.Count;

        if (count < 3)
        {
            Debug.Log("[Merge] Chưa đủ 3 heart.");
            return;
        }

        // 1) Tìm cụm 3 liên tiếp
        HeartType tripleType;
        int startIndex = FindFirstMergeTripleIndex(out tripleType);
        if (startIndex < 0)
        {
            Debug.Log("[Merge] Không có cụm 3 heart liên tiếp cùng loại.");
            return;
        }

        long cost = ActionCostStore.GetMergeCost();
        if (PlayerMoney.Instance == null || !PlayerMoney.Instance.TrySpend(cost))
        {
            Debug.Log($"[Merge] Not enough money. Need ${cost}");
            return;
        }

        Transform h0 = list[startIndex];
        Transform h1 = list[startIndex + 1];
        Transform h2 = list[startIndex + 2];

        if (h0 == null || h1 == null || h2 == null) return;

        HeartStats stats = h0.GetComponent<HeartStats>();
        if (stats == null)
        {
            Debug.LogWarning("[Merge] Thiếu HeartStats trên heart.");
            return;
        }

        if (stats.mergeResultPrefab == null)
        {
            Debug.LogWarning("[Merge] mergeResultPrefab chưa được gán cho loại " + stats.type);
            return;
        }

        long oldMoney = stats.moneyValue;

        // 2) spawnPos
        Vector3 spawnPos = (h0.position + h1.position + h2.position) / 3f;
        Quaternion spawnRot = h1.rotation;

        // 3) remove 3 tim
        for (int i = startIndex + 2; i >= startIndex; i--)
        {
            Transform h = list[i];
            list.RemoveAt(i);
            if (h != null) Destroy(h.gameObject);
        }

        // 4) tạo tim mới
        GameObject newHeart = Instantiate(stats.mergeResultPrefab, spawnPos, spawnRot, spawnParent);
        newHeart.transform.localScale = h1.localScale;

        var newStats = newHeart.GetComponent<HeartStats>();

        if (HeartUnlocks.Instance != null && newStats != null)
            HeartUnlocks.Instance.TryUpdateMaxLevel(newStats.level);

        var panel = FindObjectOfType<PanelGamePlay>(true);
        if (panel != null) panel.Refresh();

        var energy = newHeart.GetComponent<HeartWithEnergy>();
        if (energy != null) energy.enabled = false;

        // 6) insert vào chain
        list.Insert(startIndex, newHeart.transform);
        RoseWallet.Instance?.AddRose(1);


        Debug.Log($"[Merge] Merge 3 {tripleType} -> {stats.mergeResultPrefab.name} (cost={cost})");

        // 7) recalc
        chain.RecalculateLeaderByWeight();
        chain.EnsureEnergyOnLeaderOnly();
        chain.SnapChainImmediate();

        ActionCostStore.IncreaseMergeCost();

        if (newStats != null && HeartUnlocks.Instance != null)
        {
            if (!HeartUnlocks.Instance.IsUnlocked(newStats.type))
            {
                HeartUnlocks.Instance.MarkUnlocked(newStats.type);

                if (GameManager.Instance != null)
                    GameManager.Instance.OnUnlockedNewHeartType();

                var panels = UIManager.Instance.OpenUI<PanelNewHeart>();
                Sprite icon = newStats.icon;
                panels.Show(icon, newStats.level, oldMoney, newStats.moneyValue);
            }
        }

        GameSaveManager.Instance?.RequestSave();
    }

    // ===================== HELPERS =====================

    public Transform GetLastHeart()
    {
        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts.Count == 0) return null;
        return chain.hearts[chain.hearts.Count - 1];
    }

    public Transform GetLeader()
    {
        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts.Count == 0) return null;
        return chain.hearts[0];
    }

    public int GetAddableHeartLevel()
    {
        if (HeartUnlocks.Instance == null)
            return 1;

        int maxUnlocked = HeartUnlocks.Instance.GetMaxUnlockedLevel();
        int maxAddable = maxUnlocked - 3;

        if (maxAddable < 1)
            maxAddable = 1;

        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts == null || chain.hearts.Count == 0)
            return maxAddable;

        int lowestLevelInChain = int.MaxValue;

        foreach (var t in chain.hearts)
        {
            if (t == null) continue;

            var stats = t.GetComponent<HeartStats>();
            if (stats == null) continue;

            if (stats.level < lowestLevelInChain)
                lowestLevelInChain = stats.level;
        }

        if (lowestLevelInChain < maxAddable)
            return lowestLevelInChain;

        return maxAddable;
    }

    int GetCurrentHeartCount()
    {
        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts == null) return 0;
        return chain.hearts.Count;
    }

    bool CanAddHeart()
    {
        if (GameManager.Instance == null) return true;
        return GetCurrentHeartCount() < GameManager.Instance.MaxHearts;
    }

    // ===================== MERGE PREVIEW =====================

    public bool TryGetMergePreview(out MergePreview preview)
    {
        preview = default;

        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts == null) return false;

        var list = chain.hearts;
        int n = list.Count;
        if (n < 3) return false;

        HeartType tripleType;
        int startIndex = FindFirstMergeTripleIndex(out tripleType);
        if (startIndex < 0) return false;

        var h0 = list[startIndex];
        if (h0 == null) return false;

        var s0 = h0.GetComponent<HeartStats>();
        if (s0 == null) return false;

        GameObject resultPrefab = s0.mergeResultPrefab;
        if (resultPrefab == null) return false;

        Sprite tripleIcon = s0.icon;
        if (tripleIcon == null) tripleIcon = GetIconFromPrefabFallback(s0);

        Sprite resultIcon = null;
        var resultStats = resultPrefab.GetComponent<HeartStats>();
        if (resultStats != null) resultIcon = resultStats.icon;
        if (resultIcon == null && resultStats != null) resultIcon = GetIconFromPrefabFallback(resultStats);

        preview = new MergePreview
        {
            canMerge = true,
            tripleType = tripleType,
            resultPrefab = resultPrefab,
            tripleIcon = tripleIcon,
            resultIcon = resultIcon
        };

        return true;
    }

    Sprite GetIconFromPrefabFallback(HeartStats stats)
    {
        if (stats == null) return null;

        int level = stats.level;
        int index = level - 1;

        if (heartPrefabsByLevel == null) return null;
        if (index < 0 || index >= heartPrefabsByLevel.Count) return null;

        var prefab = heartPrefabsByLevel[index];
        if (prefab == null) return null;

        var prefabStats = prefab.GetComponent<HeartStats>();
        if (prefabStats == null) return null;

        return prefabStats.icon;
    }

    public void RebuildChainFromLevels(System.Collections.Generic.List<int> levels)
    {
        var chain = FindObjectOfType<HeartChainManager>();
        if (chain == null) return;

        // destroy old hearts in chain (scene)
        if (chain.hearts != null)
        {
            for (int i = chain.hearts.Count - 1; i >= 0; i--)
            {
                var t = chain.hearts[i];
                if (t != null) Destroy(t.gameObject);
            }
            chain.hearts.Clear();
        }
        else chain.hearts = new System.Collections.Generic.List<Transform>();

        if (levels == null || levels.Count == 0) return;

        // spawn sequentially using heartPrefabsByLevel (level-1)
        for (int i = 0; i < levels.Count; i++)
        {
            int lv = Mathf.Max(1, levels[i]);
            int idx = lv - 1;

            if (heartPrefabsByLevel == null || idx < 0 || idx >= heartPrefabsByLevel.Count) idx = 0;
            var prefab = heartPrefabsByLevel[idx];
            if (prefab == null) continue;

            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;

            // spawn near previous (tạm), sau đó chain.SnapChainImmediate sẽ đặt đúng
            if (i > 0 && chain.hearts[i - 1] != null)
            {
                pos = chain.hearts[i - 1].position;
                rot = chain.hearts[i - 1].rotation;
            }
            else if (chain.GetLeader() != null)
            {
                pos = chain.GetLeader().position;
                rot = chain.GetLeader().rotation;
            }

            var go = Instantiate(prefab, pos, rot, spawnParent);
            chain.hearts.Add(go.transform);

            var energy = go.GetComponent<HeartWithEnergy>();
            if (energy != null) energy.enabled = false;
        }

        chain.RecalculateLeaderByWeight();
        chain.EnsureEnergyOnLeaderOnly();
        chain.SnapChainImmediate();
    }

}
