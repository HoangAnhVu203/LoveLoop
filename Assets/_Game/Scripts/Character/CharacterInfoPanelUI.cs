using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoPanelUI : MonoBehaviour
{
    [SerializeField] Text levelText;
    [SerializeField] Text descText;

    [Header("Level Icon UI")]
    [SerializeField] Image levelIconImage;

    CharacterData current;
    int currentLevel;

    public void Show(CharacterData data, int lv)
    {
        current = data;
        currentLevel = lv;

        if (current == null) return;

        if (levelText != null)
            levelText.text = $"LEVEL {currentLevel}";

        if (descText != null)
            descText.text = current.description;

        RefreshLevelIcon();
    }

    public void Refresh(int lv)
    {
        currentLevel = lv;

        if (current == null) return;

        if (levelText != null)
            levelText.text = $"LEVEL {currentLevel}";

        RefreshLevelIcon();
    }

    void RefreshLevelIcon()
    {
        if (levelIconImage == null || current == null) return;

        Sprite icon = current.GetLevelIcon(currentLevel);
        levelIconImage.sprite = icon;
        levelIconImage.enabled = (icon != null);
    }
}
