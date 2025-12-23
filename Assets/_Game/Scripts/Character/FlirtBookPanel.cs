using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlirtBookPanel : UICanvas
{
    [Header("List")]
    [SerializeField] RectTransform content;
    [SerializeField] CharacterThumbItemUI thumbPrefab;
    [SerializeField] List<CharacterData> characters;
    [SerializeField] ScrollSnapToCenter centerSelector;

    [Header("UI Detail")]
    [SerializeField] CharacterInfoPanelUI infoPanel;

    [Header("Photo Button (Lock at Lv4)")]
    [SerializeField] Button cameraBtn;
    [SerializeField] CanvasGroup cameraBtnGroup;
    [SerializeField, Range(0f, 1f)] float lockedAlpha = 0.4f;
    [SerializeField, Range(0f, 1f)] float unlockedAlpha = 1f;
    [SerializeField] int photoUnlockLevel = 4;

    CharacterData currentCharacter;

    void Start()
    {
        Build();
        RefreshCurrentUI();
    }

    void Build()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        var spawned = new List<CharacterThumbItemUI>();

        foreach (var c in characters)
        {
            var item = Instantiate(thumbPrefab, content);
            item.Bind(c);
            spawned.Add(item);
        }

        centerSelector.SetItems(spawned);

        centerSelector.OnCenteredChanged = (c) =>
        {
            currentCharacter = c;
            RefreshCurrentUI();
        };

        if (characters != null && characters.Count > 0)
            currentCharacter = characters[0];
    }

    void RefreshCurrentUI()
    {
        if (currentCharacter == null) return;

        if (infoPanel != null)
            infoPanel.Show(currentCharacter);

        RefreshPhotoButtonState();
    }

    void RefreshPhotoButtonState()
    {
        if (currentCharacter == null) return;

        int defaultLv = currentCharacter.level <= 0 ? 1 : currentCharacter.level;
        int lv = CharacterProgressStore.GetLevel(currentCharacter.characterId, defaultLv);

        bool unlocked = lv >= photoUnlockLevel;

        if (cameraBtn != null)
            cameraBtn.interactable = unlocked;
        if (cameraBtnGroup != null)
        {
            cameraBtnGroup.alpha = unlocked ? unlockedAlpha : lockedAlpha;
            cameraBtnGroup.interactable = unlocked;
            cameraBtnGroup.blocksRaycasts = unlocked;
        }
    }

    public void OnLevelUpClicked()
    {
        if (currentCharacter == null) return;

        int defaultLv = currentCharacter.level <= 0 ? 1 : currentCharacter.level;
        int currentLv = CharacterProgressStore.GetLevel(currentCharacter.characterId, defaultLv);

        if (currentLv >= CharacterProgressStore.MAX_LEVEL)
            return;

        int newLv = CharacterProgressStore.LevelUp(currentCharacter.characterId, defaultLv);

        if (infoPanel != null) infoPanel.Refresh();

        RefreshPhotoButtonState();

        Debug.Log($"[LevelUp] {currentCharacter.characterId} -> LEVEL {newLv}");
    }

    public void OpenPhotoBookBTN()
    {
        if (currentCharacter == null) return;

        int defaultLv = currentCharacter.level <= 0 ? 1 : currentCharacter.level;
        int lv = CharacterProgressStore.GetLevel(currentCharacter.characterId, defaultLv);

        if (lv < photoUnlockLevel)
        {
            Debug.Log($"Photo locked: Reach level {photoUnlockLevel} to unlock.");
            return;
        }

        var panel = UIManager.Instance.OpenUI<PhotoViewerPanelUI>();
        panel.Show(currentCharacter);
    }

    public void CloseBTN()
    {
        UIManager.Instance.CloseUIDirectly<FlirtBookPanel>();
    }

    public void OnDimClick()
    {
        UIManager.Instance.CloseUIDirectly<FlirtBookPanel>();
    }
}
