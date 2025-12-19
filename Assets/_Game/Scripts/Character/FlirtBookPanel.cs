using System.Collections.Generic;
using UnityEngine;

public class FlirtBookPanel : UICanvas
{
    [SerializeField] RectTransform content;
    [SerializeField] CharacterThumbItemUI thumbPrefab;
    [SerializeField] List<CharacterData> characters;

    [SerializeField] ScrollSnapToCenter centerSelector;

    void Start()
    {
        Build();
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
    }

    public void CloseBTN()
    {
        UIManager.Instance.CloseUIDirectly<FlirtBookPanel>();
    }
}
