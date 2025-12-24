using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] Vector3 offset = new Vector3(0, 5f, -10f);
    [SerializeField] float moveSpeed = 5f;

    [Header("Target (tự động = Heart leader)")]
    [SerializeField] Transform target;
    [SerializeField] HeartChainManager chainManager;

    void Awake()
    {
        if (chainManager == null)
        {
            chainManager = FindObjectOfType<HeartChainManager>();
        }
    }

    void Start()
    {
        if (target == null)
        {
            UpdateTargetToLeader();
        }
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }

    void LateUpdate()
    {
        AutoUpdateTargetFromLeader();

        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            moveSpeed * Time.deltaTime
        );
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }

    public void UpdateTargetToLeader()
    {
        if (chainManager == null) return;

        Transform leader = chainManager.GetLeader();
        if (leader != null)
        {
            target = leader;
        }
    }

    void AutoUpdateTargetFromLeader()
    {
        if (chainManager == null)
        {
            chainManager = FindObjectOfType<HeartChainManager>();
            if (chainManager == null) return;
        }

        Transform leader = chainManager.GetLeader();
        if (leader == null) return;
        if (target != leader)
        {
            target = leader;

            offset = transform.position - target.position;
        }
    }

    public void RebindToLeaderSnap()
    {
        if (chainManager == null) chainManager = FindObjectOfType<HeartChainManager>();
        if (chainManager == null) return;

        var leader = chainManager.GetLeader();
        if (leader == null) return;

        target = leader;
        // Snap ngay để “focus”
        transform.position = target.position + offset;
        transform.LookAt(target.position);
    }
}
