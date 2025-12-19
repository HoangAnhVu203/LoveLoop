using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterThumbItemUI : MonoBehaviour
{
    [SerializeField] Image avatar;
    [SerializeField] TMP_Text nameText;

    public CharacterData Data { get; private set; }

    public void Bind(CharacterData d)
    {
        Data = d;
        avatar.sprite = d.avatar;
        nameText.text = d.characterName;
    }

    public void SetFocused(bool focused)
    {
        transform.localScale = focused ? Vector3.one * 1.05f : Vector3.one;
    }
}
