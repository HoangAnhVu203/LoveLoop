using UnityEngine;

public class PanelReview : UICanvas
{
    public void CloseBTN()
    {
        UIManager.Instance.CloseUIDirectly<PanelReview>();
    }
}
