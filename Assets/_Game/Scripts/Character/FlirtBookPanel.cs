using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlirtBookPanel : UICanvas
{
    [SerializeField] RectTransform content;
    [SerializeField] CharacterThumbItemUI thumbPrefab;
    [SerializeField] List<CharacterData> characters;
    [SerializeField] ScrollSnapToCenter centerSelector;

    //[Header("Buttons")]
    //[SerializeField] Button levelUpBtn;

    [Header("UI Detail")]
    [SerializeField] CharacterInfoPanelUI infoPanel;

    [SerializeField] Button cameraBtn;

    CharacterData currentCharacter;

    void Start()
    {
        Build();

        //if (levelUpBtn != null)
        //{
        //    levelUpBtn.onClick.RemoveAllListeners();
        //    levelUpBtn.onClick.AddListener(OnLevelUpClicked);
        //}

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

        RefreshLevelUpButtonState();
    }

    void RefreshLevelUpButtonState()
    {
        //if (levelUpBtn == null || currentCharacter == null) return;

        bool canUp = CharacterProgressStore.CanLevelUp(
            currentCharacter.characterId,
            currentCharacter.level <= 0 ? 1 : currentCharacter.level
        );

        //levelUpBtn.interactable = canUp;
    }

    public void OnLevelUpClicked()
    {
        if (currentCharacter == null) return;

        int defaultLv = currentCharacter.level <= 0 ? 1 : currentCharacter.level;
        int currentLv = CharacterProgressStore.GetLevel(currentCharacter.characterId, defaultLv);

        if (currentLv >= CharacterProgressStore.MAX_LEVEL)
        {
            RefreshLevelUpButtonState();
            return;
        }

        // TODO: CHECK COST + TRỪ TIỀN 
        // long cost = currentCharacter.levelUpCost;  cost theo LV
        // if (PlayerMoney.Instance.money < cost) return;
        // PlayerMoney.Instance.Spend(cost);

        int newLv = CharacterProgressStore.LevelUp(currentCharacter.characterId, defaultLv);

        if (infoPanel != null) infoPanel.Refresh();
        RefreshLevelUpButtonState();

        Debug.Log($"[LevelUp] {currentCharacter.characterId} -> LEVEL {newLv}");
    }

    public void CloseBTN()
    {
        UIManager.Instance.CloseUIDirectly<FlirtBookPanel>();
    }

    public void OpenPhotoBookBTN()
    {
        if (currentCharacter == null)
        {
            Debug.LogError("currentCharacter == null");
            return;
        }

        var panel = UIManager.Instance.OpenUI<PhotoViewerPanelUI>();
        panel.Show(currentCharacter);
    }

    public void OnDimClick()
    {
        UIManager.Instance.CloseUIDirectly<FlirtBookPanel>();
    }
}
