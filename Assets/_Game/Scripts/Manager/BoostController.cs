using System;
using System.Collections;
using UnityEngine;

public class BoostController : Singleton<BoostController>
{
    [Header("Refs")]
    [SerializeField] HeartChainManager chain;

    [Header("Config")]
    [SerializeField] float defaultDuration = 60f;

    public bool IsBoosting { get; private set; }
    public float Remaining { get; private set; }

    public event Action<bool> OnBoostStateChanged;   // true = start, false = end
    public event Action<float> OnRemainingChanged;   // optional UI

    Coroutine _cr;

    void Awake()
    {
        if (chain == null) chain = FindObjectOfType<HeartChainManager>();
    }

    public bool TryStartBoost(float durationSeconds = -1f)
    {
        if (IsBoosting) return false;

        if (durationSeconds <= 0f) durationSeconds = defaultDuration;

        if (_cr != null) StopCoroutine(_cr);
        _cr = StartCoroutine(BoostRoutine(durationSeconds));
        return true;
    }

    IEnumerator BoostRoutine(float duration)
    {
        IsBoosting = true;
        Remaining = duration;

        ApplyBoost(true);
        OnBoostStateChanged?.Invoke(true);
        OnRemainingChanged?.Invoke(Remaining);

        while (Remaining > 0f)
        {
            Remaining -= Time.deltaTime;
            if (Remaining < 0f) Remaining = 0f;

            OnRemainingChanged?.Invoke(Remaining);
            yield return null;
        }

        ApplyBoost(false);
        IsBoosting = false;

        OnBoostStateChanged?.Invoke(false);
        _cr = null;
    }

    void ApplyBoost(bool on)
    {
        if (chain == null) return;

        // ====== CHỈNH 1 DÒNG NÀY CHO KHỚP PROJECT CỦA BẠN ======
        // Ví dụ nếu HeartChainManager có API:
        // chain.SetBoost(on);

        // Hoặc nếu bạn có flag:
        // chain.isBoosting = on;

        // Hoặc nếu bạn dùng global:
        // HeartWithEnergy.SetBoostGlobal(on);
    }
}
