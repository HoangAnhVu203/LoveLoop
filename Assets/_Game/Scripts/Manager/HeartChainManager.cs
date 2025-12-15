using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HeartChainManager : MonoBehaviour
{
    [Header("Heart list (0 = leader)")]
    public List<Transform> hearts = new List<Transform>();

    [Header("History sampling")]
    [Tooltip("Leader đi được >= sampleDistance thì record 1 điểm history")]
    public float sampleDistance = 0.05f;

    [Tooltip("Khoảng cách (theo số điểm history) giữa các heart")]
    public float pointsPerHeart = 10f;

    [Header("Follow Lerp (Normal)")]
    public float normalFollowPosLerp = 15f;
    public float normalFollowRotLerp = 15f;

    [Header("Follow Lerp (Boost)")]
    public float boostFollowPosLerp = 25f;
    public float boostFollowRotLerp = 25f;

    [Header("Blend normal <-> boost")]
    public float followLerpBlendSpeed = 10f;

    [Header("Leader / Energy")]
    public Transform center; // tâm cho HeartWithEnergy

    // ================= INTERNAL =================

    struct Pose
    {
        public Vector3 pos;
        public Quaternion rot;
    }

    readonly List<Pose> _history = new List<Pose>();

    Vector3 _lastRecordPos;
    bool _hasLastRecordPos;

    float _currentPosLerp;
    float _currentRotLerp;

    // ================= UNITY =================

    void Start()
    {
        InitHistory();

        _currentPosLerp = normalFollowPosLerp;
        _currentRotLerp = normalFollowRotLerp;

        EnsureEnergyOnLeaderOnly();
    }

    void Update()
    {
        if (hearts == null || hearts.Count == 0) return;

        Transform leader = hearts[0];
        if (leader == null) return;

        RecordLeaderHistoryByDistance(leader);
        if (_history.Count < 2) return;

        bool isBoosting = HeartWithEnergy.IsBoostingGlobal;

        float posTarget = isBoosting ? boostFollowPosLerp : normalFollowPosLerp;
        float rotTarget = isBoosting ? boostFollowRotLerp : normalFollowRotLerp;

        _currentPosLerp = Mathf.Lerp(_currentPosLerp, posTarget, followLerpBlendSpeed * Time.deltaTime);
        _currentRotLerp = Mathf.Lerp(_currentRotLerp, rotTarget, followLerpBlendSpeed * Time.deltaTime);

        // FOLLOWER bám theo history (logic cũ)
        for (int i = 1; i < hearts.Count; i++)
        {
            Transform follower = hearts[i];
            if (follower == null) continue;

            // QUAN TRỌNG: spacing KHÔNG đổi theo boost -> không dồn, không giật
            float fIndex = i * pointsPerHeart;

            if (fIndex >= _history.Count - 1)
                fIndex = _history.Count - 1.001f;

            int idx0 = Mathf.FloorToInt(fIndex);
            int idx1 = Mathf.Clamp(idx0 + 1, 0, _history.Count - 1);
            float t = fIndex - idx0;

            Pose p0 = _history[idx0];
            Pose p1 = _history[idx1];

            Vector3 targetPos = Vector3.Lerp(p0.pos, p1.pos, t);
            Quaternion targetRot = Quaternion.Slerp(p0.rot, p1.rot, t);

            follower.position = Vector3.Lerp(
                follower.position,
                targetPos,
                _currentPosLerp * Time.deltaTime
            );

            follower.rotation = Quaternion.Slerp(
                follower.rotation,
                targetRot,
                _currentRotLerp * Time.deltaTime
            );
        }
    }

    // ================= HISTORY (BẢN CŨ – GIỮ NGUYÊN) =================

    void RecordLeaderHistoryByDistance(Transform leader)
    {
        if (leader == null) return;

        if (!_hasLastRecordPos)
        {
            _lastRecordPos = leader.position;
            _hasLastRecordPos = true;

            _history.Insert(0, new Pose { pos = leader.position, rot = leader.rotation });
            return;
        }

        Vector3 currentPos = leader.position;
        float sqrDist = (currentPos - _lastRecordPos).sqrMagnitude;
        float minSqr = sampleDistance * sampleDistance;

        if (sqrDist >= minSqr)
        {
            _history.Insert(0, new Pose { pos = currentPos, rot = leader.rotation });
            _lastRecordPos = currentPos;

            int maxPoints = Mathf.CeilToInt(hearts.Count * pointsPerHeart) + 20;
            if (_history.Count > maxPoints)
            {
                _history.RemoveRange(maxPoints, _history.Count - maxPoints);
            }
        }
    }

    public void InitHistory()
    {
        _history.Clear();
        _hasLastRecordPos = false;

        if (hearts == null || hearts.Count == 0) return;

        Transform leader = hearts[0];
        if (leader == null) return;

        _history.Add(new Pose { pos = leader.position, rot = leader.rotation });
        _lastRecordPos = leader.position;
        _hasLastRecordPos = true;
    }

    // ================= PUBLIC API (CHO SCRIPT KHÁC DÙNG) =================

    public Transform GetLeader()
    {
        return (hearts != null && hearts.Count > 0) ? hearts[0] : null;
    }

    public Transform GetLastHeart()
    {
        return (hearts != null && hearts.Count > 0) ? hearts[hearts.Count - 1] : null;
    }

    // Alias để tương thích nếu script cũ gọi GetLeader()
    public Transform GetLeaderTransform() => GetLeader();

    public void RegisterHeart(Transform newHeart)
    {
        if (newHeart == null) return;
        if (hearts == null) hearts = new List<Transform>();

        if (!hearts.Contains(newHeart))
        {
            hearts.Add(newHeart);

            // nếu trước đó history trống thì init lại
            if (_history.Count == 0)
            {
                InitHistory();
                _currentPosLerp = normalFollowPosLerp;
                _currentRotLerp = normalFollowRotLerp;
            }
        }
    }

    // ================= SNAP / REBUILD (DÙNG KHI MERGE / ĐỔI LEADER) =================

    public void SnapAllHeartsToHistory()
    {
        if (hearts == null || hearts.Count == 0) return;
        if (_history.Count < 2) return;

        for (int i = 0; i < hearts.Count; i++)
        {
            Transform tf = hearts[i];
            if (tf == null) continue;

            float fIndex = i * pointsPerHeart;
            if (fIndex >= _history.Count - 1)
                fIndex = _history.Count - 1.001f;

            int idx0 = Mathf.FloorToInt(fIndex);
            int idx1 = Mathf.Clamp(idx0 + 1, 0, _history.Count - 1);
            float t = fIndex - idx0;

            Pose p0 = _history[idx0];
            Pose p1 = _history[idx1];

            tf.position = Vector3.Lerp(p0.pos, p1.pos, t);
            tf.rotation = Quaternion.Slerp(p0.rot, p1.rot, t);
        }
    }

    public void RebuildHistoryFromCurrentChain()
    {
        _history.Clear();
        _hasLastRecordPos = false;

        if (hearts == null || hearts.Count == 0) return;
        if (hearts[0] == null) return;

        int need = Mathf.CeilToInt(hearts.Count * pointsPerHeart) + 20;

        // Fill history dựa trên chain hiện tại (đủ dài để follower không bị "hụt")
        for (int i = 0; i < hearts.Count && _history.Count < need; i++)
        {
            if (hearts[i] == null) continue;

            _history.Add(new Pose
            {
                pos = hearts[i].position,
                rot = hearts[i].rotation
            });
        }

        while (_history.Count < need)
        {
            Pose last = _history[_history.Count - 1];
            _history.Add(last);
        }

        _lastRecordPos = hearts[0].position;
        _hasLastRecordPos = true;
    }

    // Hàm này để FIX error bạn báo: HeartManager gọi RebuildHistoryByChainSegments
    public void RebuildHistoryByChainSegments()
    {
        _history.Clear();
        _hasLastRecordPos = false;

        if (hearts == null || hearts.Count == 0) return;

        int need = Mathf.CeilToInt(hearts.Count * pointsPerHeart) + 20;

        // bắt đầu từ leader
        _history.Add(new Pose { pos = hearts[0].position, rot = hearts[0].rotation });

        for (int i = 1; i < hearts.Count && _history.Count < need; i++)
        {
            Transform a = hearts[i - 1];
            Transform b = hearts[i];
            if (a == null || b == null) continue;

            int kCount = Mathf.Max(1, Mathf.RoundToInt(pointsPerHeart));
            for (int k = 1; k <= kCount && _history.Count < need; k++)
            {
                float t = k / (float)kCount;
                _history.Add(new Pose
                {
                    pos = Vector3.Lerp(a.position, b.position, t),
                    rot = Quaternion.Slerp(a.rotation, b.rotation, t)
                });
            }
        }

        // pad đủ length
        Pose pad = _history[_history.Count - 1];
        while (_history.Count < need) _history.Add(pad);

        _lastRecordPos = hearts[0].position;
        _hasLastRecordPos = true;
    }

    // ================= ENERGY / LEADER =================

    void MoveEnergyToNewLeader(Transform oldLeader, Transform newLeader)
    {
        if (newLeader == null) return;

        if (oldLeader != null && oldLeader != newLeader)
        {
            var oldE = oldLeader.GetComponent<HeartWithEnergy>();
            if (oldE != null) oldE.enabled = false;
        }

        var newE = newLeader.GetComponent<HeartWithEnergy>();
        if (newE == null) newE = newLeader.gameObject.AddComponent<HeartWithEnergy>();

        newE.enabled = true;
        if (newE.center == null) newE.center = center;
    }

    public void EnsureEnergyOnLeaderOnly()
    {
        if (hearts == null || hearts.Count == 0) return;

        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] == null) continue;

            var e = hearts[i].GetComponent<HeartWithEnergy>();

            if (i == 0)
            {
                if (e == null) e = hearts[i].gameObject.AddComponent<HeartWithEnergy>();
                e.enabled = true;
                if (e.center == null) e.center = center;
            }
            else
            {
                if (e != null) e.enabled = false;
            }
        }
    }

    // ================= LEADER BY WEIGHT =================

    public void RecalculateLeaderByWeight()
    {
        if (hearts == null || hearts.Count == 0) return;

        int bestIndex = 0;
        int bestWeight = int.MinValue;

        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] == null) continue;

            var stats = hearts[i].GetComponent<HeartStats>();
            int w = (stats != null) ? stats.weight : 0;

            if (w > bestWeight)
            {
                bestWeight = w;
                bestIndex = i;
            }
        }

        Transform oldLeader = hearts[0];
        Transform newLeader = hearts[bestIndex];

        if (oldLeader == newLeader)
        {
            EnsureEnergyOnLeaderOnly();
            return;
        }

        // rotate list để newLeader lên index 0
        List<Transform> newList = new List<Transform>(hearts.Count);
        for (int i = 0; i < hearts.Count; i++)
        {
            int idx = (bestIndex + i) % hearts.Count;
            newList.Add(hearts[idx]);
        }
        hearts = newList;

        MoveEnergyToNewLeader(oldLeader, hearts[0]);
        EnsureEnergyOnLeaderOnly();

        // rebuild + snap để không giật sau đổi leader
        RebuildHistoryByChainSegments();
        SnapAllHeartsToHistory();

        Debug.Log($"[Leader] Đổi leader sang: {hearts[0].name} (weight = {bestWeight})");
    }
}
