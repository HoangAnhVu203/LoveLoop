using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HeartChainManager : MonoBehaviour
{
    [Header("Danh sách Heart (0 = leader)")]
    public List<Transform> hearts = new List<Transform>();

    [Header("Khoảng cách history giữa các heart")]
    [Tooltip("Càng lớn thì tim cách nhau càng xa (đơn vị: số mẫu history)")]
    public float pointsPerHeart = 18f;   

    [Header("Ghi history leader")]
    [Tooltip("Khoảng thời gian giữa 2 mẫu history, 0.01–0.02 là mượt")]
    public float recordInterval = 0.01f;

    [Header("Độ mượt follower bám theo (bình thường)")]
    public float normalFollowPosLerp = 15f;
    public float normalFollowRotLerp = 15f;

    [Header("Độ mượt follower bám theo (khi BOOST)")]
    public float boostFollowPosLerp = 40f;
    public float boostFollowRotLerp = 40f;

    struct Pose
    {
        public Vector3 pos;
        public Quaternion rot;
    }

    List<Pose> _history = new List<Pose>();
    float _recordTimer;

    void Start()
    {
        InitHistory();
    }

    void InitHistory()
    {
        _history.Clear();

        if (hearts.Count == 0)
            return;

        Pose p;
        p.pos = hearts[0].position;
        p.rot = hearts[0].rotation;
        _history.Add(p);
    }

    void Update()
    {
        if (hearts.Count == 0)
            return;

        Transform leader = hearts[0];

        // 1) Ghi history của leader
        _recordTimer += Time.deltaTime;
        if (_recordTimer >= recordInterval)
        {
            _recordTimer = 0f;

            Pose p;
            p.pos = leader.position;
            p.rot = leader.rotation;

            _history.Insert(0, p); // phần tử 0 là frame mới nhất

            // giữ history vừa đủ dài
            int maxPoints = Mathf.CeilToInt(hearts.Count * pointsPerHeart) + 2;
            if (_history.Count > maxPoints)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }

        if (_history.Count < 2)
            return;

        // 2) Chọn độ Lerp theo trạng thái BOOST
        bool isBoosting = HeartWithEnergy.IsBoostingGlobal;
        float posLerp = isBoosting ? boostFollowPosLerp : normalFollowPosLerp;
        float rotLerp = isBoosting ? boostFollowRotLerp : normalFollowRotLerp;

        // 3) Follower bám theo history
        for (int i = 1; i < hearts.Count; i++)
        {
            Transform follower = hearts[i];

            // index history mà tim thứ i nên theo (spacing CỐ ĐỊNH)
            float fIndex = i * pointsPerHeart;

            if (fIndex >= _history.Count - 1)
                fIndex = _history.Count - 1.001f; // tránh out of range

            int idx0 = Mathf.FloorToInt(fIndex);
            int idx1 = Mathf.Clamp(idx0 + 1, 0, _history.Count - 1);
            float t = fIndex - idx0;

            Pose p0 = _history[idx0];
            Pose p1 = _history[idx1];

            // nội suy giữa 2 frame history → target mềm
            Vector3 targetPos = Vector3.Lerp(p0.pos, p1.pos, t);
            Quaternion targetRot = Quaternion.Slerp(p0.rot, p1.rot, t);

            // follower trôi dần tới target → chuyển động mượt, không giật
            follower.position = Vector3.Lerp(
                follower.position,
                targetPos,
                posLerp * Time.deltaTime
            );

            follower.rotation = Quaternion.Slerp(
                follower.rotation,
                targetRot,
                rotLerp * Time.deltaTime
            );
        }
    }

    // gọi khi spawn thêm heart
    public void RegisterHeart(Transform newHeart)
    {
        if (!hearts.Contains(newHeart))
        {
            hearts.Add(newHeart);

            // nếu chưa có history thì init
            if (_history.Count == 0)
            {
                InitHistory();
            }
        }
    }

    public Transform GetLeader()
    {
        return hearts.Count > 0 ? hearts[0] : null;
    }

    public Transform GetLastHeart()
    {
        return hearts.Count > 0 ? hearts[hearts.Count - 1] : null;
    }
}
