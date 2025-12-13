using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HeartChainManager : MonoBehaviour
{
    [Header("Heart list")]
    public List<Transform> hearts = new List<Transform>();

    [Header("History theo khoảng cách")]
    public float sampleDistance = 0.05f;
    public float pointsPerHeart = 10f;

    [Header("Độ mượt follower bám theo (bình thường)")]
    public float normalFollowPosLerp = 15f;
    public float normalFollowRotLerp = 15f;

    [Header("Độ mượt follower bám theo (khi BOOST)")]
    public float boostFollowPosLerp = 25f;
    public float boostFollowRotLerp = 25f;

    [Header("Blend giữa normal ↔ boost")]
    public float followLerpBlendSpeed = 10f;

    [Header("Leader Settings")]
    public Transform center; // tâm cho HeartWithEnergy

    struct Pose
    {
        public Vector3 pos;
        public Quaternion rot;
    }

    List<Pose> _history = new List<Pose>();

    Vector3 _lastRecordPos;
    bool _hasLastRecordPos;
    float _currentPosLerp;
    float _currentRotLerp;

    void Start()
    {
        InitHistory();
        _currentPosLerp = normalFollowPosLerp;
        _currentRotLerp = normalFollowRotLerp;

        // đảm bảo leader có energy ngay từ đầu (an toàn)
        EnsureEnergyOnLeaderOnly();
    }

    public void InitHistory()
    {
        _history.Clear();
        _hasLastRecordPos = false;

        if (hearts.Count == 0)
            return;

        Transform leader = hearts[0];

        Pose p;
        p.pos = leader.position;
        p.rot = leader.rotation;

        _history.Add(p);
        _lastRecordPos = leader.position;
        _hasLastRecordPos = true;
    }

    void Update()
    {
        if (hearts.Count == 0)
            return;

        Transform leader = hearts[0];
        if (leader == null) return;

        RecordLeaderHistoryByDistance(leader);

        if (_history.Count < 2)
            return;

        bool isBoosting = HeartWithEnergy.IsBoostingGlobal;

        float targetPosLerp = isBoosting ? boostFollowPosLerp : normalFollowPosLerp;
        float targetRotLerp = isBoosting ? boostFollowRotLerp : normalFollowRotLerp;

        _currentPosLerp = Mathf.Lerp(_currentPosLerp, targetPosLerp, followLerpBlendSpeed * Time.deltaTime);
        _currentRotLerp = Mathf.Lerp(_currentRotLerp, targetRotLerp, followLerpBlendSpeed * Time.deltaTime);

        // follower bám theo history
        for (int i = 1; i < hearts.Count; i++)
        {
            Transform follower = hearts[i];
            if (follower == null) continue;

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

    void RecordLeaderHistoryByDistance(Transform leader)
    {
        if (leader == null) return;

        if (!_hasLastRecordPos)
        {
            _lastRecordPos = leader.position;
            _hasLastRecordPos = true;

            Pose first;
            first.pos = leader.position;
            first.rot = leader.rotation;
            _history.Insert(0, first);
            return;
        }

        Vector3 currentPos = leader.position;
        float sqrDist = (currentPos - _lastRecordPos).sqrMagnitude;
        float minSqr = sampleDistance * sampleDistance;

        if (sqrDist >= minSqr)
        {
            Pose p;
            p.pos = currentPos;
            p.rot = leader.rotation;

            _history.Insert(0, p);
            _lastRecordPos = currentPos;

            int maxPoints = Mathf.CeilToInt(hearts.Count * pointsPerHeart) + 5;
            if (_history.Count > maxPoints)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }
    }

    public void RegisterHeart(Transform newHeart)
    {
        if (newHeart == null) return;

        if (!hearts.Contains(newHeart))
        {
            hearts.Add(newHeart);

            if (_history.Count == 0)
            {
                InitHistory();
                _currentPosLerp = normalFollowPosLerp;
                _currentRotLerp = normalFollowRotLerp;
            }
        }
    }

    // ===== SNAP toàn bộ chain về đúng vị trí theo history =====
    public void SnapAllHeartsToHistory()
    {
        if (hearts.Count == 0 || _history.Count < 2)
            return;

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

            Vector3 targetPos = Vector3.Lerp(p0.pos, p1.pos, t);
            Quaternion targetRot = Quaternion.Slerp(p0.rot, p1.rot, t);

            tf.position = targetPos;
            tf.rotation = targetRot;
        }
    }

    // ========= Leader / Weight =========

    void MoveEnergyToNewLeader(Transform oldLeader, Transform newLeader)
    {
        if (newLeader == null) return;

        // chỉ disable old nếu old != new
        if (oldLeader != null && oldLeader != newLeader)
        {
            var oldEnergy = oldLeader.GetComponent<HeartWithEnergy>();
            if (oldEnergy != null)
                oldEnergy.enabled = false;
        }

        var newEnergy = newLeader.GetComponent<HeartWithEnergy>();
        if (newEnergy == null)
            newEnergy = newLeader.gameObject.AddComponent<HeartWithEnergy>();

        newEnergy.enabled = true;

        if (newEnergy.center == null && center != null)
            newEnergy.center = center;
    }

    /// <summary>
    /// Chắc chắn chỉ leader có HeartWithEnergy bật, follower tắt.
    /// Dùng để tránh case merge xong leader bị thiếu/disable energy.
    /// </summary>
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
                if (e.center == null && center != null) e.center = center;
            }
            else
            {
                if (e != null) e.enabled = false;
            }
        }
    }

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

        // Dù leader không đổi, vẫn phải đảm bảo energy đúng (tránh đứng im)
        if (newLeader == oldLeader)
        {
            MoveEnergyToNewLeader(oldLeader, oldLeader);
            EnsureEnergyOnLeaderOnly();
            return;
        }

        // Xoay list sao cho newLeader về index 0
        List<Transform> newList = new List<Transform>(hearts.Count);
        int n = hearts.Count;
        for (int i = 0; i < n; i++)
        {
            int idx = (bestIndex + i) % n;
            newList.Add(hearts[idx]);
        }
        hearts = newList;

        // Cập nhật Energy cho leader mới
        MoveEnergyToNewLeader(oldLeader, hearts[0]);
        EnsureEnergyOnLeaderOnly();

        // rebuild history + snap để không giật
        RebuildHistoryFromCurrentChain();
        SnapAllHeartsToHistory();

        // cập nhật history[0] theo vị trí leader mới
        if (_history.Count > 0)
        {
            Pose p = _history[0];
            p.pos = hearts[0].position;
            p.rot = hearts[0].rotation;
            _history[0] = p;
            _lastRecordPos = hearts[0].position;
            _hasLastRecordPos = true;
        }

        SnapAllHeartsToHistory();

        Debug.Log($"[Leader] Đổi leader sang: {hearts[0].name} (weight = {bestWeight})");
    }

    public void RebuildHistoryFromCurrentChain()
    {
        _history.Clear();
        _hasLastRecordPos = false;

        if (hearts == null || hearts.Count == 0) return;

        int need = Mathf.CeilToInt(hearts.Count * pointsPerHeart) + 5;

        int h = 0;
        while (_history.Count < need)
        {
            int idx = Mathf.Clamp(h, 0, hearts.Count - 1);
            _history.Add(new Pose { pos = hearts[idx].position, rot = hearts[idx].rotation });
            h++;
        }

        _lastRecordPos = hearts[0].position;
        _hasLastRecordPos = true;
    }

    public Transform GetLeader()
    {
        return hearts.Count > 0 ? hearts[0] : null;
    }

    public Transform GetLastHeart()
    {
        return hearts.Count > 0 ? hearts[hearts.Count - 1] : null;
    }

    public void ForceResetHistory()
    {
        InitHistory();
    }

    public void RebuildHistoryByChainSegments()
    {
        _history.Clear();
        _hasLastRecordPos = false;

        if (hearts == null || hearts.Count == 0) return;

        int need = Mathf.CeilToInt(hearts.Count * pointsPerHeart) + 5;

        Pose p0 = new Pose { pos = hearts[0].position, rot = hearts[0].rotation };
        _history.Add(p0);

        for (int i = 1; i < hearts.Count; i++)
        {
            Transform a = hearts[i - 1];
            Transform b = hearts[i];
            if (a == null || b == null) continue;

            int kCount = Mathf.Max(1, Mathf.RoundToInt(pointsPerHeart));
            for (int k = 1; k <= kCount; k++)
            {
                float t = k / (float)kCount;
                var pp = new Pose
                {
                    pos = Vector3.Lerp(a.position, b.position, t),
                    rot = Quaternion.Slerp(a.rotation, b.rotation, t)
                };
                _history.Add(pp);

                if (_history.Count >= need) break;
            }

            if (_history.Count >= need) break;
        }

        Pose last = _history[_history.Count - 1];
        while (_history.Count < need) _history.Add(last);

        _lastRecordPos = hearts[0].position;
        _hasLastRecordPos = true;
    }

}
