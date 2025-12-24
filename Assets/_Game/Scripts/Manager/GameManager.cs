using System;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    const string KEY_MAX_HEARTS = "MAX_HEARTS";

    [Header("Heart Cap Rule")]
    [SerializeField] int startMaxHearts = 3;
    [SerializeField] int addCapPerNewHeartUnlock = 3;

    public int MaxHearts { get; private set; }
    public event Action OnHeartCapChanged;

    public event Action<long> OnLapPreviewChanged;

    public event Action<long> OnLapCompleted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        Load();
    }

    void Start()
    {
        UIManager.Instance.OpenUI<PanelGamePlay>();
        OnHeartCapChanged?.Invoke();
        RefreshLapPreview();
    }

    void Load()
    {
        MaxHearts = PlayerPrefs.GetInt(KEY_MAX_HEARTS, startMaxHearts);
    }

    void Save()
    {
        PlayerPrefs.SetInt(KEY_MAX_HEARTS, MaxHearts);
        PlayerPrefs.Save();
    }

    public void OnUnlockedNewHeartType()
    {
        MaxHearts += addCapPerNewHeartUnlock;
        Save();
        OnHeartCapChanged?.Invoke();

        RefreshLapPreview();
    }

    // ================== LAP PREVIEW ==================

    public long GetLapPreviewMoney()
    {
        return CalculateLapPreview();
    }

    public void RefreshLapPreview()
    {
        OnLapPreviewChanged?.Invoke(CalculateLapPreview());
    }

    long CalculateLapPreview()
    {
        var chain = FindObjectOfType<HeartChainManager>();
        if (chain == null || chain.hearts == null) return 0;

        long sumHearts = 0;
        for (int i = 0; i < chain.hearts.Count; i++)
        {
            var t = chain.hearts[i];
            if (t == null) continue;

            var stats = t.GetComponent<HeartStats>();
            if (stats == null) continue;

            sumHearts += stats.moneyValue;
        }

        int gateCount = (GateManager.Instance != null) ? GateManager.Instance.GatesCount : 0;
        return sumHearts * gateCount;
    }

    // ================== LAP COMPLETE ==================

    public void CompleteLap()
    {
        long totalPerLap = CalculateLapPreview();
        OnLapCompleted?.Invoke(totalPerLap);

        RefreshLapPreview();
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }
}
