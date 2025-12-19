using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoPanelUI : MonoBehaviour
{
    [SerializeField] Text levelText;
    [SerializeField] Text descText;

    CharacterData current;

    public void Show(CharacterData data)
    {
        current = data;
        levelText.text = $"LEVEL {current.level}";
        descText.text = current.description;
    }
}
