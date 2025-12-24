using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoseWallet : MonoBehaviour
{
    public static RoseWallet Instance { get; private set; }

    const string KEY_ROSE = "ROSE_COUNT";

    [Header("UI (optional)")]
    [SerializeField] TMP_Text roseText;

    public long CurrentRose { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
        UpdateUI();
    }

    void Load()
    {
        CurrentRose = PlayerPrefs.GetInt(KEY_ROSE, 0);
    }

    void Save()
    {
        PlayerPrefs.SetInt(KEY_ROSE, (int)Mathf.Clamp(CurrentRose, 0, int.MaxValue));
        PlayerPrefs.Save();
    }

    public void BindRoseText(TMP_Text t)
    {
        roseText = t;
        UpdateUI();
    }

    public void AddRose(long amount)
    {
        if (amount <= 0) return;

        CurrentRose += amount;
        if (CurrentRose < 0) CurrentRose = 0;

        Save();
        UpdateUI();
    }

    public bool SpendRose(long amount)
    {
        if (amount <= 0) return true;
        if (CurrentRose < amount) return false;

        CurrentRose -= amount;
        Save();
        UpdateUI();
        return true;
    }

    void UpdateUI()
    {
        if (roseText != null)
            roseText.text = CurrentRose.ToString("N0");
    }

    public void SetRose(long value)
    {
        CurrentRose = value;
        if (CurrentRose < 0) CurrentRose = 0;

        Save();
        UpdateUI();
    }

}
