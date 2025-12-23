using UnityEngine;
using UnityEngine.UI;

public class PhotoThumbItemUI : MonoBehaviour
{
    [SerializeField] Image thumb;

    int index;
    System.Action<int> onClick;
    bool interactable = true;

    public void Bind(Sprite sprite, int idx, bool canClick, System.Action<int> cb)
    {
        thumb.sprite = sprite;
        index = idx;
        onClick = cb;
        interactable = canClick;
        
        //TODO: Làm mờ ảnh
        // thumb.color = canClick ? Color.white : new Color(1f,1f,1f,0.6f);
    }

    public void OnClick()
    {
        if (!interactable) return;
        onClick?.Invoke(index);
    }
}
