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

        int savedLevel = CharacterProgressStore.GetLevel(current.characterId, current.level <= 0 ? 1 : current.level);
        levelText.text = $"LEVEL {savedLevel}";
        descText.text = current.description;
    }

    public void Refresh()
    {
        if (current == null) return;
        int savedLevel = CharacterProgressStore.GetLevel(current.characterId, current.level <= 0 ? 1 : current.level);
        levelText.text = $"LEVEL {savedLevel}";
    }
}
