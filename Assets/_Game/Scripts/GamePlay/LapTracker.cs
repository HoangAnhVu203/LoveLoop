using UnityEngine;

public class LapTracker : MonoBehaviour
{
    [SerializeField] HeartChainManager chain;
    float lastDistance = 0f;

    void Awake()
    {
        if (chain == null) chain = FindObjectOfType<HeartChainManager>();
    }

    void Update()
    {
        if (chain == null || chain.splinePath == null) return;
        var sp = chain.splinePath;
        if (!sp.loop) return;

        float total = sp.TotalLength;
        if (total <= 0f) return;

        float d = chain.leaderDistance;
        if (lastDistance > total * 0.8f && d < total * 0.2f)
        {
            GameManager.Instance?.CompleteLap();
        }

        lastDistance = d;
    }
}
