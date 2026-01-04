using UnityEngine;
using UnityEngine.UI;

public class BuildingThumbItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Text nameText;

    [Header("Focus")]
    [SerializeField] private float focusedScale = 1.12f;
    [SerializeField] private float normalScale = 1f;

    public BuildingOnRoad Building { get; private set; }
    public BuildingData Data => (Building != null) ? Building.data : null;

    public void Bind(BuildingOnRoad b)
    {
        Building = b;

        var d = Data;
        if (icon != null) icon.sprite = d != null ? d.buildingIMG : null;
        if (nameText != null) nameText.text = d != null ? d.buildingName : "";

        SetFocused(false);
    }

    public void SetFocused(bool focused)
    {
        transform.localScale = Vector3.one * (focused ? focusedScale : normalScale);
    }

    public bool TryUpgradeRuntime()
    {
        if (Building == null) return false;
        if (Building.data == null) return false;

        return Building.TryUpgrade();
    }

    public int GetCurrentLevel()
    {
        if (Building == null) return 1;
        return Mathf.Max(1, Building.level);
    }
}
