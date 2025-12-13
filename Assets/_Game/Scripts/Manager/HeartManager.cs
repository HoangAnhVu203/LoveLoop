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

    [Tooltip("Tên Layer dùng cho HeartPink (để nhận diện)")]
    public string pinkLayerName = "HeartPink";

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
    }

    [System.Obsolete]
    public void AddHeart()
    {
        if (HeartChainManagerInstance == null) return;

        var manager = HeartChainManagerInstance;

        if (manager.hearts.Count == 0)
        {
            Debug.LogWarning("[HeartManager] ChainManager chưa có leader.");
            return;
        }

        Transform last = manager.hearts[manager.hearts.Count - 1];
        if (last == null) return;

        // Spawn vào ROOT
        GameObject newHeart = Instantiate(
            heartPrefab,
            last.position,
            last.rotation,
            spawnParent
        );

        // COPY SCALE CHUẨN TỪ HEART CŨ
        newHeart.transform.localScale = last.localScale;

        // follower không cần Energy
        var energy = newHeart.GetComponent<HeartWithEnergy>();
        if (energy != null)
            energy.enabled = false;

        manager.RegisterHeart(newHeart.transform);

        // recalc leader và đảm bảo energy đúng
        manager.RecalculateLeaderByWeight();
        manager.EnsureEnergyOnLeaderOnly();
    }

    [System.Obsolete]
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

    [System.Obsolete]
    HeartChainManager HeartChainManagerInstance
    {
        get { return FindObjectOfType<HeartChainManager>(); }
    }

    // ======== MERGE ANY TRIPLE ========

    [System.Obsolete]
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

        // 1. Tìm cụm 3 liên tiếp cùng loại
        HeartType tripleType;
        int startIndex = FindFirstMergeTripleIndex(out tripleType);
        if (startIndex < 0)
        {
            Debug.Log("[Merge] Không có cụm 3 heart liên tiếp cùng loại.");
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

        // 2. Vị trí spawn = trung bình 3 tim
        Vector3 spawnPos = (h0.position + h1.position + h2.position) / 3f;
        Quaternion spawnRot = h1.rotation;

        // 3. Xoá 3 tim khỏi list & scene (từ index lớn về nhỏ)
        // NOTE: remove khỏi list trước để tránh logic khác đọc nhầm
        for (int i = startIndex + 2; i >= startIndex; i--)
        {
            Transform h = list[i];
            list.RemoveAt(i);
            if (h != null)
                Destroy(h.gameObject);
        }

        // 4. Tạo tim mới
        GameObject newHeart = Instantiate(
            stats.mergeResultPrefab,
            spawnPos,
            spawnRot,
            spawnParent
        );

        newHeart.transform.localScale = h1.localScale;

        // 5. KHÔNG tự quyết Energy ở đây (để chain quyết sau khi recalc)
        var energy = newHeart.GetComponent<HeartWithEnergy>();
        if (energy != null) energy.enabled = false;

        // 6. Thêm tim mới vào chuỗi tại vị trí startIndex
        list.Insert(startIndex, newHeart.transform);

        Debug.Log($"[Merge] Merge 3 {tripleType} tại index {startIndex} → {stats.mergeResultPrefab.name}");

        // 7. Recalc leader + đảm bảo energy đúng
        chain.RecalculateLeaderByWeight();
        chain.EnsureEnergyOnLeaderOnly();

        // 8. Reset history + snap để node nối ngay, không bị đứng/khựng
        chain.RebuildHistoryByChainSegments();
        chain.SnapAllHeartsToHistory();
    }

    // ======== Helper lấy leader / last từ ChainManager ========

    [System.Obsolete]
    public Transform GetLastHeart()
    {
        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts.Count == 0)
            return null;

        return chain.hearts[chain.hearts.Count - 1];
    }

    [System.Obsolete]
    public Transform GetLeader()
    {
        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts.Count == 0)
            return null;

        return chain.hearts[0];
    }
}
