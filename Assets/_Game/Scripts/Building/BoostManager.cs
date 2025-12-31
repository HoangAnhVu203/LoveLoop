using UnityEngine;

public class BoostManager : MonoBehaviour
{
    public static BoostManager Instance { get; private set; }

    float _boostEndTime;
    bool _applied;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool IsBoosting => Time.time < _boostEndTime;
    public float Remaining => Mathf.Max(0f, _boostEndTime - Time.time);

    public void AddBoostSeconds(float seconds)
    {
        if (seconds <= 0f) return;

        float now = Time.time;
        float end = Mathf.Max(_boostEndTime, now);
        _boostEndTime = end + seconds;

        if (!_applied)
        {
            _applied = true;
            HeartWithEnergy.StartAutoBoost(Remaining);
        }
        else
        {
            HeartWithEnergy.StartAutoBoost(Remaining);
        }
    }

    void Update()
    {

        if (_applied && !IsBoosting)
        {
            _applied = false;
            HeartWithEnergy.StopAutoBoost();
        }
    }
}
