using UnityEngine;

public class BoostManager : MonoBehaviour
{
    public static BoostManager Instance { get; private set; }

    float _boostEndTime;

    void Awake() => Instance = this;

    public bool IsBoosting => Time.time < _boostEndTime;
    public float Remaining => Mathf.Max(0f, _boostEndTime - Time.time);

    public void AddBoostSeconds(float seconds)
    {
        if (seconds <= 0f) return;

        float now = Time.time;
        float end = Mathf.Max(_boostEndTime, now);
        _boostEndTime = end + seconds;

        HeartWithEnergy.StartAutoBoost(Remaining);
    }

    void Update()
    {
        if (IsBoosting)
        {
            HeartWithEnergy.StartAutoBoost(Remaining);
        }
    }
}
