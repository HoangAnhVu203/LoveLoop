using UnityEngine;

public class RiderAnimator : MonoBehaviour
{
    [SerializeField] Animator animator;
    static readonly int GateHit = Animator.StringToHash("GateHit");

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
    }

    public void PlayGateHit()
    {
        Debug.Log("[Rider] Hit gate");
        if (animator == null) return;

        animator.ResetTrigger(GateHit);
        animator.SetTrigger(GateHit);
    }
}
