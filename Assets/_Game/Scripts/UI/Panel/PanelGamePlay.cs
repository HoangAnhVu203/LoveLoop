using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelGamePlay : UICanvas
{
    [Header("UI")]
    [SerializeField] GameObject mergeButtonGO;
    [SerializeField] Text moneyText;

    [Header("Cost UI")]
    [SerializeField] TMP_Text addCostText;
    [SerializeField] TMP_Text mergeCostText;
    [SerializeField] string costPrefix = "$ ";

    [Header("Heart Cap UI")]
    [SerializeField] TMP_Text heartCapText;
    private string capFormat = "{0}/{1}";
    private float checkInterval = 0.15f;

    [Header("Lap Money UI")]
    [SerializeField] Text lapMoneyText;

    HeartChainManager _chain;
    float _nextCheckTime;
    bool _lastCanMerge;

    [Header("Boost")]
    public Button boostBtn;
    public Text labelText;
    private string normalText = "Boost Update";
    private string maxText = "Boost Max LV";

    [Header("Add Heart Icon")]
    public Image iconImage;

    [Header("Visual when disabled")]
    private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Merge Preview UI")]
    [SerializeField] Image mergeFromIcon;
    [SerializeField] Image mergeToIcon;

    [SerializeField] TMP_Text roseText;


    Image _btnImage;

    void Awake()
    {
        _chain = FindObjectOfType<HeartChainManager>();
        if (boostBtn != null) _btnImage = boostBtn.GetComponent<Image>();
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHeartCapChanged += RefreshHeartCapUI;
            GameManager.Instance.OnLapPreviewChanged += UpdateLapMoneyUI;
            GameManager.Instance.OnLapCompleted += ShowLapCompletedUI;

            UpdateLapMoneyUI(GameManager.Instance.GetLapPreviewMoney());
        }
        if (RoseWallet.Instance != null && roseText != null)
            RoseWallet.Instance.BindRoseText(roseText);


        if (PlayerMoney.Instance != null && moneyText != null)
            PlayerMoney.Instance.BindMoneyText(moneyText);

        Refresh();
        ForceRefreshMergeButton();
        RefreshState();
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHeartCapChanged -= RefreshHeartCapUI;
            GameManager.Instance.OnLapPreviewChanged -= UpdateLapMoneyUI;
            GameManager.Instance.OnLapCompleted -= ShowLapCompletedUI;
        }
    }

    void Update()
    {
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + checkInterval;

        if (_chain == null) _chain = FindObjectOfType<HeartChainManager>();
        RefreshMergeButtonIfNeeded();
    }

    // ================= MERGE BUTTON =================

    void ForceRefreshMergeButton()
    {
        RefreshMergePreviewUI();
        _lastCanMerge = CanMergeTriple();
    }

    void RefreshMergeButtonIfNeeded()
    {
        bool canMerge = CanMergeTriple();
        if (canMerge == _lastCanMerge) return;

        _lastCanMerge = canMerge;
        RefreshMergePreviewUI();
    }

    bool CanMergeTriple()
    {
        if (_chain == null || _chain.hearts == null) return false;

        var list = _chain.hearts;
        int n = list.Count;
        if (n < 3) return false;

        for (int i = 0; i <= n - 3; i++)
        {
            var a = list[i] ? list[i].GetComponent<HeartStats>() : null;
            var b = list[i + 1] ? list[i + 1].GetComponent<HeartStats>() : null;
            var c = list[i + 2] ? list[i + 2].GetComponent<HeartStats>() : null;

            if (a == null || b == null || c == null) continue;
            if (a.type == b.type && b.type == c.type) return true;
        }
        return false;
    }

    // ================= BOOST =================

    public void OnClickUpgradeDrain()
    {
        if (_chain == null) _chain = FindObjectOfType<HeartChainManager>();
        if (_chain == null || _chain.GetLeader() == null) return;

        var energy = _chain.GetLeader().GetComponent<HeartWithEnergy>();
        if (energy == null) return;

        bool success = energy.TryUpgradeDrain();
        if (!success) { SetMaxState(); return; }

        RefreshState();
    }

    void RefreshState()
    {
        if (_chain == null) _chain = FindObjectOfType<HeartChainManager>();
        if (_chain == null || _chain.GetLeader() == null) return;

        var energy = _chain.GetLeader().GetComponent<HeartWithEnergy>();
        if (energy == null) return;

        if (energy.IsDrainUpgradeMaxed()) SetMaxState();
        else SetNormalState();
    }

    void SetNormalState()
    {
        if (labelText != null) labelText.text = normalText;

        if (boostBtn != null)
        {
            boostBtn.interactable = true;
            if (_btnImage != null) _btnImage.color = Color.white;
        }
    }

    void SetMaxState()
    {
        if (labelText != null) labelText.text = maxText;

        if (boostBtn != null) boostBtn.interactable = false;
        if (_btnImage != null) _btnImage.color = disabledColor;
    }

    // ================= LAP UI =================

    void UpdateLapMoneyUI(long value)
    {
        if (lapMoneyText == null) return;
        lapMoneyText.text = "+" + MoneyFormatter.Format(value) + "/LAP";
    }

    void ShowLapCompletedUI(long total)
    {
        Debug.Log($"[LAP] Completed. Earned = {total}");
    }

    // ================= MAIN REFRESH =================

    public void Refresh()
    {
        RefreshAddHeartIcon();
        RefreshHeartCapUI();
        RefreshCostUI();

        GameManager.Instance?.RefreshLapPreview();
    }

    void RefreshCostUI()
    {
        if (addCostText != null)
        {
            long addCost = ActionCostStore.GetAddCost();
            addCostText.text = costPrefix + MoneyFormatter.Format(addCost);
        }

        if (mergeCostText != null)
        {
            long mergeCost = ActionCostStore.GetMergeCost();
            mergeCostText.text = costPrefix + MoneyFormatter.Format(mergeCost);
        }
    }

    void RefreshAddHeartIcon()
    {
        if (HeartManager.Instance == null || iconImage == null) return;

        int level = HeartManager.Instance.GetAddableHeartLevel();
        int index = level - 1;

        if (index < 0 || index >= HeartManager.Instance.heartPrefabsByLevel.Count)
        {
            iconImage.enabled = false;
            return;
        }

        GameObject prefab = HeartManager.Instance.heartPrefabsByLevel[index];
        if (prefab == null) { iconImage.enabled = false; return; }

        HeartStats stats = prefab.GetComponent<HeartStats>();
        if (stats == null || stats.icon == null) { iconImage.enabled = false; return; }

        iconImage.sprite = stats.icon;
        iconImage.enabled = true;
    }

    void RefreshHeartCapUI()
    {
        if (heartCapText == null) return;

        if (_chain == null) _chain = FindObjectOfType<HeartChainManager>();

        int current = (_chain != null && _chain.hearts != null) ? _chain.hearts.Count : 0;
        int max = (GameManager.Instance != null) ? GameManager.Instance.MaxHearts : current;

        heartCapText.text = string.Format(capFormat, current, max);
    }

    void RefreshMergePreviewUI()
    {
        if (HeartManager.Instance == null) return;

        HeartManager.MergePreview p;
        bool can = HeartManager.Instance.TryGetMergePreview(out p);

        if (mergeButtonGO != null) mergeButtonGO.SetActive(can);

        if (mergeCostText != null)
            mergeCostText.gameObject.SetActive(can);

        if (!can)
        {
            if (mergeFromIcon != null) mergeFromIcon.enabled = false;
            if (mergeToIcon != null) mergeToIcon.enabled = false;
            return;
        }

        if (mergeFromIcon != null)
        {
            mergeFromIcon.sprite = p.tripleIcon;
            mergeFromIcon.enabled = (p.tripleIcon != null);
        }

        if (mergeToIcon != null)
        {
            mergeToIcon.sprite = p.resultIcon;
            mergeToIcon.enabled = (p.resultIcon != null);
        }
    }

    // ================= BUTTON EVENTS =================

    public void AddHeartBTN()
    {
        GameManager.Instance?.RefreshLapPreview();

        HeartManager.Instance.AddHeart();
        Refresh();
        ForceRefreshMergeButton();
    }

    public void MergeHeartBTN()
    {
        GameManager.Instance?.RefreshLapPreview();

        HeartManager.Instance.MergeAnyTriple();
        Refresh();
        ForceRefreshMergeButton();
    }

    public void AddGateBTN()
    {
        GameManager.Instance?.RefreshLapPreview();

        GateManager.Instance?.SpawnGate();

        GameManager.Instance?.RefreshLapPreview();
    }

    public void NextRoadBTN()
    {
        RoadManager.Instance?.NextRoad();
    }

    public void OpenFlirtBookBTN()
    {
        UIManager.Instance.OpenUI<FlirtBookPanel>();
    }
}
