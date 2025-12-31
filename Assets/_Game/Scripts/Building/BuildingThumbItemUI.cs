using UnityEngine;
using UnityEngine.UI;

public class BuildingThumbItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Text nameText;

    [Header("Focus")]
    [SerializeField] private float focusedScale = 1.12f;
    [SerializeField] private float normalScale = 1f;

    public BuildingData Data { get; private set; }
    public int RuntimeLevel { get; private set; }

    public void Bind(BuildingData d)
    {
        Data = d;

        if (icon != null) icon.sprite = d != null ? d.buildingIMG : null;
        if (nameText != null) nameText.text = d != null ? d.buildingName : "";

        RuntimeLevel = Mathf.Max(1, BuildingLevelStore.GetLevel(d.buildingID, 1));

        SetFocused(false);
    }



    public void SetFocused(bool focused)
    {
        transform.localScale = Vector3.one * (focused ? focusedScale : normalScale);
    }

    public bool TryUpgradeRuntime()
    {
        if (Data == null) return false;

        RuntimeLevel = Mathf.Max(1, RuntimeLevel);

        if (RuntimeLevel >= Data.maxLevel) return false;

        long cost = Data.CalcUpgradeCost(RuntimeLevel);
        if (cost > 0)
        {
            if (RoseWallet.Instance == null) return false;
            if (RoseWallet.Instance.CurrentRose < cost) return false;
            if (!RoseWallet.Instance.SpendRose(cost)) return false;
        }

        RuntimeLevel++;

        BuildingLevelStore.SetLevel(Data.buildingID, RuntimeLevel);

        GameSaveManager.Instance?.RequestSave();
        return true;
    }

}
