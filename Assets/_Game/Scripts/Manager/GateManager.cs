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

        // BỎ marker
        public int girlIndex = -1;
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

    [Header("Character Mapping")]
    public GirlAvatarOrder girlOrder;   // vẫn giữ vì dùng characterIds/defaultLevels
    public bool loopGirls = false;

    [Header("3D Model Per Girl (optional)")]
    public List<GameObject> girlModelPrefabs = new();
    public GameObject defaultModelPrefab;

    int nextGirlIndex = 0;

    readonly List<GateEntry> _gates = new();
    SplinePath _currentSpline;

    void Awake()
    {
        if (chain == null) chain = FindObjectOfType<HeartChainManager>();
        if (gateRoot == null) gateRoot = transform;
    }

    // ================= ROAD CHANGED =================
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

    // ================= MODEL 3D =================
    GameObject GetModelPrefabForGate(GateEntry entry)
    {
        if (entry == null) return defaultModelPrefab;

        int idx = entry.girlIndex;
        if (idx >= 0 && idx < girlModelPrefabs.Count && girlModelPrefabs[idx] != null)
            return girlModelPrefabs[idx];

        return defaultModelPrefab;
    }

    void AttachGateModel(GateEntry entry)
    {
        if (entry == null || entry.go == null) return;

        var binder = entry.go.GetComponent<GateVisualBinder>();
        if (binder == null) return;

        var prefab = GetModelPrefabForGate(entry);
        binder.AttachModel(prefab);
    }

    // ================= SPAWN =================
    public bool SpawnGate()
    {
        if (RoadManager.Instance != null && !RoadManager.Instance.CanAddGateOnCurrentRoad())
        {
            Debug.Log("[GateManager] This road already has max gates. Upgrade road to add more.");
            return false;
        }

        if (RoseWallet.Instance == null)
        {
            Debug.LogWarning("[GateManager] RoseWallet is missing.");
            return false;
        }

        long cost = GateCostStore.GetNextGateCost();
        if (RoseWallet.Instance.CurrentRose < cost)
        {
            Debug.Log($"[GateManager] Not enough Rose. Need {cost}, have {RoseWallet.Instance.CurrentRose}");
            return false;
        }

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

        if (!RoseWallet.Instance.SpendRose(cost))
            return false;

        var go = Instantiate(gatePrefab, gateRoot);
        go.name = $"{gatePrefab.name}_Gate_{_gates.Count + 1}";

        var entry = new GateEntry { go = go, ratio = ratio };
        _gates.Add(entry);

        ApplyGateByRatio(entry);

        AssignGirlIndex(entry);

        // GẮN MODEL 3D (thay cho marker ảnh)
        AttachGateModel(entry);

        // bind characterId thật cho Gate (để reward đúng)
        BindGateCharacterFromGirlIndex(entry);

        RoadManager.Instance?.NotifyGateSpawnedOnCurrentRoad();

        GateCostStore.MarkGatePurchased();

        GameManager.Instance?.RefreshLapPreview();
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

    // ================= INTERNAL =================
    void ResolveSpline()
    {
        if (_currentSpline != null && _currentSpline.TotalLength > 0f) return;
        if (chain != null) _currentSpline = chain.splinePath;
    }

    void AssignGirlIndex(GateEntry entry)
    {
        if (entry == null) return;

        int total = GetGirlCount();
        if (total <= 0)
        {
            entry.girlIndex = -1;
            return;
        }

        int idx = nextGirlIndex;

        if (idx >= total)
        {
            if (!loopGirls) idx = total - 1;
            else idx = idx % total;
        }

        entry.girlIndex = idx;

        FlirtBookUnlockStore.TryUnlock(idx);

        nextGirlIndex++;
    }

    int GetGirlCount()
    {
        if (girlOrder == null) return 0;

        if (girlOrder.characterIds != null && girlOrder.characterIds.Count > 0)
            return girlOrder.characterIds.Count;

        // fallback
        if (girlOrder.avatars != null && girlOrder.avatars.Count > 0)
            return girlOrder.avatars.Count;

        return 0;
    }

    void BindGateCharacterFromGirlIndex(GateEntry entry)
    {
        if (entry == null || entry.go == null) return;

        var gate = entry.go.GetComponent<Gate>();
        if (gate == null) return;

        GetCharacterInfo(entry.girlIndex, out string charId, out int defaultLv);
        gate.SetCharacter(charId, defaultLv);
    }

    void GetCharacterInfo(int girlIndex, out string characterId, out int defaultLv)
    {
        characterId = "";
        defaultLv = 1;

        if (girlOrder == null) return;

        if (girlOrder.characterIds != null &&
            girlIndex >= 0 && girlIndex < girlOrder.characterIds.Count)
        {
            characterId = girlOrder.characterIds[girlIndex];
        }

        if (girlOrder.defaultLevels != null &&
            girlIndex >= 0 && girlIndex < girlOrder.defaultLevels.Count)
        {
            defaultLv = Mathf.Max(1, girlOrder.defaultLevels[girlIndex]);
        }

        if (string.IsNullOrEmpty(characterId))
            characterId = $"girl_{girlIndex}";
    }

    // ================= PICK RATIO =================
    bool TryPickValidRatio(out float ratio)
    {
        ratio = 0f;

        float rMin = Mathf.Clamp01(minSpawnRatio);
        float rMax = Mathf.Clamp01(maxSpawnRatio);
        if (rMax < rMin) (rMin, rMax) = (rMax, rMin);

        float leaderRatio = GetLeaderRatio();
        float avoidLeader = Mathf.Clamp01(avoidLeaderWindowRatio);
        float minSpacing = Mathf.Clamp01(minGateSpacingRatio);

        for (int k = 0; k < Mathf.Max(1, maxRandomTries); k++)
        {
            float r = UnityEngine.Random.Range(rMin, rMax);
            if (!IsValidRatio(r, leaderRatio, avoidLeader, minSpacing)) continue;

            ratio = r;
            return true;
        }

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

    // ================= APPLY =================
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

    // ================= PUBLIC API =================
    public int GatesCount => _gates.Count;

    public List<GateSave> ExportAllGates()
    {
        var result = new List<GateSave>();
        for (int i = 0; i < _gates.Count; i++)
        {
            var e = _gates[i];
            if (e == null || e.go == null) continue;

            result.Add(new GateSave
            {
                roadIndex = 0,
                ratio = e.ratio,
                girlIndex = e.girlIndex
            });
        }
        return result;
    }

    public void LoadGatesFromSave(List<GateSave> saves)
    {
        ClearAllGates();

        if (saves == null || saves.Count == 0) return;

        ResolveSpline();
        if (_currentSpline == null || _currentSpline.TotalLength <= 0f) return;

        int maxNext = 0;

        for (int i = 0; i < saves.Count; i++)
        {
            var s = saves[i];
            if (gatePrefab == null) break;

            var go = Instantiate(gatePrefab, gateRoot);
            go.name = $"{gatePrefab.name}_Gate_{i + 1}";

            var entry = new GateEntry
            {
                go = go,
                ratio = Mathf.Clamp01(s.ratio),
                girlIndex = s.girlIndex
            };

            _gates.Add(entry);
            ApplyGateByRatio(entry);

            FlirtBookUnlockStore.TryUnlock(entry.girlIndex);

            // GẮN MODEL 3D khi load
            AttachGateModel(entry);

            BindGateCharacterFromGirlIndex(entry);

            maxNext = Mathf.Max(maxNext, entry.girlIndex + 1);
        }

        nextGirlIndex = Mathf.Max(nextGirlIndex, maxNext);

        GameManager.Instance?.RefreshLapPreview();
    }
}
