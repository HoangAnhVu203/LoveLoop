using UnityEngine;

public class PanelBuilding : UICanvas
{
    public void ClickDimBTN()
    {
        gameObject.SetActive(false);
    }

    public void UpgradeBuilding(BuildingInstance b)
    {
        if (b == null || b.data == null) return;
        if (!b.CanUpgrade()) return;

        int cost = b.GetUpgradeCost();
        if (RoseWallet.Instance.CurrentRose < cost) return;

        RoseWallet.Instance.SpendRose(cost);
        b.Upgrade();

        GameSaveManager.Instance?.RequestSave();
    }

}
