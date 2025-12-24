using System;
using System.Collections.Generic;
using UnityEngine;

public class GateManager : Singleton<GateManager>
{
    [Serializable]
    class GateEntry
    {
        public GameObject go;
        [Range(0f, 1f)] public float ratio;

        public GateAvatarMarker marker;
        public int girlIndex = -1;

        public int roadIndex;
    }

    [Header("Prefab & Root")]
    [SerializeField] GameObject gatePrefab;
    [SerializeField] Transform gateRoot;

    [Header("Refs")]
    [SerializeField] HeartChainManager chain;

    [Header("Spawn (pick ratio)")]
    [Range(0f, 1f)] public float minSpawnRatio = 0.05f;
    [Range(0f, 1f)] public float maxSpawnRatio = 0.95f;
    [Range(0f, 1f)] public float avoidLeaderWindowRatio = 0.05f;

    [Header("Anti-overlap")]
    [Range(0f, 1f)] public float minGateSpacingRatio = 0.08f;
    public int maxRandomTries = 40;
    [Range(0.001f, 0.1f)] public float fallbackScanStepRatio = 0.01f;

    [Header("Transform")]
    public Vector3 gateWorldOffset = new Vector3(0f, 0.02f, 0f);
    public bool alignToSplineForward = true;
    public Vector3 gateEulerOffset = Vector3.zero;

    [Header("Optional limits")]
    public int maxGates = 0;

    [Header("Avatar (optional)")]
    public GateAvatarMarker avatarPrefab;     
    public GirlAvatarOrder girlOrder;      
    public Vector3 avatarLocalOffset = new Vector3(0f, 1.2f, 0f);
    public bool loopGirls = false;           

    int nextGirlIndex = 0;

    readonly List<GateEntry> _gates = new();
    SplinePath _currentSpline;

    void Awake()
    {
        if (chain == null) chain = FindObjectOfType<HeartChainManager>();
        if (gateRoot == null) gateRoot = transform;
    }


    public void OnRoadChanged(SplinePath newSpline)
    {
        _currentSpline = newSpline;
        if (_currentSpline == null || _currentSpline.TotalLength <= 0f) return;

        for (int i = 0; i < _gates.Count; i++)
        {
            var e = _gates[i];
            if (e == null || e.go == null) continue;
            ApplyGateByRatio(e);
        }
    }

    public bool SpawnGate()
    {

        if (RoadManager.Instance != null && !RoadManager.Instance.CanAddGateOnCurrentRoad())
        {
            Debug.Log("[GateManager] This road already has max 3 gates. Upgrade road to add more.");
            return false; 
        }

        // 0) check wallet
        if (RoseWallet.Instance == null)
        {
            Debug.LogWarning("[GateManager] RoseWallet is missing.");
            return false;
        }

        // 1) tính cost và check rose
        long cost = GateCostStore.GetNextGateCost();
        if (RoseWallet.Instance.CurrentRose < cost)
        {
            Debug.Log($"[GateManager] Not enough Rose. Need {cost}, have {RoseWallet.Instance.CurrentRose}");
            return false;
        }

        // 2) validate điều kiện spawn TRƯỚC (để tránh trừ rose rồi fail)
        if (gatePrefab == null)
        {
            Debug.LogError("[GateManager] gatePrefab is null");
            return false;
        }

        ResolveSpline();
        if (_currentSpline == null || _currentSpline.TotalLength <= 0f)
        {
            Debug.LogError("[GateManager] No active spline to spawn.");
            return false;
        }

        if (maxGates > 0 && _gates.Count >= maxGates)
        {
            Debug.LogWarning("[GateManager] Reached maxGates, not spawning.");
            return false;
        }

        if (!TryPickValidRatio(out float ratio))
        {
            Debug.LogWarning("[GateManager] Cannot find valid spawn ratio (too crowded).");
            return false;
        }

        // 3) tới đây chắc chắn spawn được -> trừ rose
        if (!RoseWallet.Instance.SpendRose(cost))
            return false;

        // 4) spawn gate
        var go = Instantiate(gatePrefab, gateRoot);
        go.name = $"{gatePrefab.name}_Gate_{_gates.Count}";

        var entry = new GateEntry { go = go, ratio = ratio };
        _gates.Add(entry);
        ApplyGateByRatio(entry);
        AttachAvatarMarker(entry);

        RoadManager.Instance?.NotifyGateSpawnedOnCurrentRoad();

        // 5) đánh dấu đã mua gate -> cost tăng ngay
        GateCostStore.MarkGatePurchased();

        // 6) refresh các hệ thống phụ thuộc gate count (nếu có)
        GameManager.Instance?.RefreshLapPreview();

        entry.roadIndex = RoadManager.Instance != null ? RoadManager.Instance.CurrentRoadIndex : 0;
        GameSaveManager.Instance?.RequestSave();

        return true;
    }



    public void ClearAllGates()
    {
        for (int i = 0; i < _gates.Count; i++)
        {
            if (_gates[i]?.go != null) Destroy(_gates[i].go);
        }
        _gates.Clear();
    }

    // ================== INTERNAL ==================

    void ResolveSpline()
    {
        if (_currentSpline != null && _currentSpline.TotalLength > 0f) return;

        if (chain != null) _currentSpline = chain.splinePath;
    }

    void AttachAvatarMarker(GateEntry entry)
    {
        if (entry == null || entry.go == null) return;
        if (avatarPrefab == null) return;
        if (girlOrder == null || girlOrder.avatars == null || girlOrder.avatars.Count == 0) return;

        int count = girlOrder.avatars.Count;

        // chọn index theo thứ tự
        int idx = nextGirlIndex;

        if (idx >= count)
        {
            if (!loopGirls)
            {
                // hết nhân vật -> không spawn avatar nữa
                return;
            }
            idx = idx % count;
        }

        // spawn marker làm con của gate
        var marker = Instantiate(avatarPrefab, entry.go.transform);
        marker.transform.localPosition = avatarLocalOffset;
        marker.transform.localRotation = Quaternion.identity;
        marker.transform.localScale = Vector3.one;

        marker.SetAvatar(girlOrder.avatars[idx]);

        entry.marker = marker;
        entry.girlIndex = idx;

        nextGirlIndex++;
    }

    // ================== PICK RATIO ==================

    bool TryPickValidRatio(out float ratio)
    {
        ratio = 0f;

        float rMin = Mathf.Clamp01(minSpawnRatio);
        float rMax = Mathf.Clamp01(maxSpawnRatio);
        if (rMax < rMin) (rMin, rMax) = (rMax, rMin);

        float leaderRatio = GetLeaderRatio();
        float avoidLeader = Mathf.Clamp01(avoidLeaderWindowRatio);
        float minSpacing = Mathf.Clamp01(minGateSpacingRatio);

        // random tries
        for (int k = 0; k < Mathf.Max(1, maxRandomTries); k++)
        {
            float r = UnityEngine.Random.Range(rMin, rMax);
            if (!IsValidRatio(r, leaderRatio, avoidLeader, minSpacing)) continue;

            ratio = r;
            return true;
        }

        // fallback scan
        float step = Mathf.Clamp(fallbackScanStepRatio, 0.001f, 0.1f);
        float start = UnityEngine.Random.Range(rMin, rMax);

        for (int pass = 0; pass < 2; pass++)
        {
            float dir = (pass == 0) ? 1f : -1f;
            float t = start;

            int maxSteps = Mathf.CeilToInt((rMax - rMin) / step) + 2;

            for (int i = 0; i < maxSteps; i++)
            {
                if (t < rMin) t = rMax - (rMin - t);
                if (t > rMax) t = rMin + (t - rMax);

                if (IsValidRatio(t, leaderRatio, avoidLeader, minSpacing))
                {
                    ratio = t;
                    return true;
                }

                t += dir * step;
            }
        }

        return false;
    }

    bool IsValidRatio(float r, float leaderRatio, float avoidLeader, float minSpacing)
    {
        bool loop = (_currentSpline != null && _currentSpline.loop);

        if (avoidLeader > 0f)
        {
            float dLeader = RatioDistance01(r, leaderRatio, loop);
            if (dLeader < avoidLeader) return false;
        }

        if (minSpacing > 0f)
        {
            for (int i = 0; i < _gates.Count; i++)
            {
                var e = _gates[i];
                if (e == null || e.go == null) continue;

                float dGate = RatioDistance01(r, e.ratio, loop);
                if (dGate < minSpacing) return false;
            }
        }

        return true;
    }

    float GetLeaderRatio()
    {
        if (chain == null || _currentSpline == null || _currentSpline.TotalLength <= 0f)
            return 0f;

        float d = chain.leaderDistance;
        float total = _currentSpline.TotalLength;

        if (_currentSpline.loop) d = Mathf.Repeat(d, total);
        else d = Mathf.Clamp(d, 0f, total);

        return total > 1e-6f ? Mathf.Clamp01(d / total) : 0f;
    }

    static float RatioDistance01(float a, float b, bool loop)
    {
        float diff = Mathf.Abs(a - b);
        if (!loop) return diff;
        return Mathf.Min(diff, 1f - diff);
    }

    // ================== APPLY ==================

    void ApplyGateByRatio(GateEntry e)
    {
        if (e == null || e.go == null || _currentSpline == null) return;

        float total = _currentSpline.TotalLength;
        if (total <= 0f) return;

        float d = Mathf.Clamp01(e.ratio) * total;

        if (_currentSpline.loop) d = Mathf.Repeat(d, total);
        else d = Mathf.Clamp(d, 0f, total);

        _currentSpline.SampleAtDistance(d, out var pos, out var fwd);

        e.go.transform.position = pos + gateWorldOffset;

        if (alignToSplineForward)
        {
            Vector3 flat = new Vector3(fwd.x, 0f, fwd.z);
            if (flat.sqrMagnitude < 1e-6f) flat = Vector3.forward;

            Quaternion rot = Quaternion.LookRotation(flat.normalized, Vector3.up);
            e.go.transform.rotation = rot * Quaternion.Euler(gateEulerOffset);
        }
        else
        {
            e.go.transform.rotation = Quaternion.Euler(gateEulerOffset);
        }
    }

    // ================== OPTIONAL API ==================

    public int GatesCount => _gates.Count;

    public void ResetGirlOrder(int startIndex = 0)
    {
        nextGirlIndex = Mathf.Max(0, startIndex);
    }

    public List<GateSave> ExportAllGates()
    {
        var result = new List<GateSave>();
        // cần biết gate thuộc road nào -> bạn đang dùng 1 list _gates cho road hiện tại.
        // Cách đúng: lưu roadIndex ngay trong GateEntry.
        // Tạm thời: mình yêu cầu bạn thêm roadIndex vào GateEntry.

        for (int i = 0; i < _gates.Count; i++)
        {
            var e = _gates[i];
            if (e == null || e.go == null) continue;

            result.Add(new GateSave
            {
                roadIndex = e.roadIndex,
                ratio = e.ratio,
                girlIndex = e.girlIndex
            });
        }
        return result;
    }

    public void LoadGatesFromSave(System.Collections.Generic.List<GateSave> saves)
    {
        ClearAllGates();

        if (saves == null || saves.Count == 0) return;

        // Đảm bảo spline current
        ResolveSpline();
        if (_currentSpline == null && chain != null) _currentSpline = chain.splinePath;
        if (_currentSpline == null || _currentSpline.TotalLength <= 0f) return;

        for (int i = 0; i < saves.Count; i++)
        {
            var s = saves[i];
            if (gatePrefab == null) break;

            var go = Instantiate(gatePrefab, gateRoot);
            go.name = $"{gatePrefab.name}_Gate_Load_{i}";

            var entry = new GateEntry
            {
                go = go,
                ratio = Mathf.Clamp01(s.ratio),
                girlIndex = s.girlIndex
            };

            _gates.Add(entry);
            ApplyGateByRatio(entry);

            // nếu bạn muốn restore avatar đúng girlIndex
            RestoreAvatarMarker(entry, s.girlIndex);
        }
    }

    void RestoreAvatarMarker(GateEntry entry, int girlIndex)
    {
        if (entry == null || entry.go == null) return;
        if (avatarPrefab == null) return;
        if (girlOrder == null || girlOrder.avatars == null || girlOrder.avatars.Count == 0) return;
        if (girlIndex < 0 || girlIndex >= girlOrder.avatars.Count) return;

        var marker = Instantiate(avatarPrefab, entry.go.transform);
        marker.transform.localPosition = avatarLocalOffset;
        marker.transform.localRotation = Quaternion.identity;
        marker.transform.localScale = Vector3.one;

        marker.SetAvatar(girlOrder.avatars[girlIndex]);

        entry.marker = marker;
        entry.girlIndex = girlIndex;
    }


    // Bạn cần thêm trường roadIndex trong GateEntry:
    // public int roadIndex;

    void SpawnGateAtRatio_NoCost(float ratio, int girlIndex, int roadIndex)
    {
        if (gatePrefab == null) return;

        var go = Instantiate(gatePrefab, gateRoot);
        go.name = $"{gatePrefab.name}_Gate_Loaded_{_gates.Count}";

        var entry = new GateEntry { go = go, ratio = Mathf.Clamp01(ratio) };
        entry.roadIndex = roadIndex;
        _gates.Add(entry);

        ApplyGateByRatio(entry);

        // restore avatar theo girlIndex
        if (girlIndex >= 0)
        {
            ForceAttachAvatarByIndex(entry, girlIndex);
        }

        // cập nhật gate count road
        RoadManager.Instance?.NotifyGateSpawnedOnCurrentRoad();
    }

    void ForceAttachAvatarByIndex(GateEntry entry, int girlIndex)
    {
        if (entry == null || entry.go == null) return;
        if (avatarPrefab == null) return;
        if (girlOrder == null || girlOrder.avatars == null) return;
        if (girlIndex < 0 || girlIndex >= girlOrder.avatars.Count) return;

        var marker = Instantiate(avatarPrefab, entry.go.transform);
        marker.transform.localPosition = avatarLocalOffset;
        marker.transform.localRotation = UnityEngine.Quaternion.identity;
        marker.transform.localScale = UnityEngine.Vector3.one;

        marker.SetAvatar(girlOrder.avatars[girlIndex]);

        entry.marker = marker;
        entry.girlIndex = girlIndex;
    }

        public System.Collections.Generic.List<GateSave> ExportAllGatesWithRoadIndex(int currentRoadIndex)
    {
        var list = new System.Collections.Generic.List<GateSave>();
        for (int i = 0; i < _gates.Count; i++)
        {
            var e = _gates[i];
            if (e == null || e.go == null) continue;

            list.Add(new GateSave
            {
                roadIndex = currentRoadIndex, // gate hiện đang “thuộc” road nào theo hệ thống của bạn
                ratio = Mathf.Clamp01(e.ratio),
                girlIndex = e.girlIndex
            });
        }
        return list;
    }

}
