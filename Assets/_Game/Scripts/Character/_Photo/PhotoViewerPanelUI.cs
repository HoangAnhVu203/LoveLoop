using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhotoViewerPanelUI : UICanvas
{
    [Header("Main")]
    [SerializeField] Image mainImage;
    [SerializeField] TMP_Text commentText;

    [Header("Thumbs")]
    [SerializeField] Transform content;
    [SerializeField] PhotoThumbItemUI thumbPrefab;

    [Header("Like")]
    [SerializeField] Button likeBTN;
    [SerializeField] Image likeIcon;
    private Color likedColor = Color.red;
    private Color unlikedColor = Color.white;

    [Header("Particle")]
    [SerializeField] RectTransform particleRoot;
    [SerializeField] UIHeartParticle particlePrefab;

    List<PhotoThumbItemUI> thumbs = new();

    CharacterData current;
    string currentCharacterId;
    int currentIndex;

    // ===================== PUBLIC =====================

    public void Show(CharacterData data)
    {
        if (data == null) return;

        current = data;
        currentCharacterId = data.characterId;

        if (likeBTN != null)
        {
            likeBTN.onClick.RemoveAllListeners();
            likeBTN.onClick.AddListener(OnLikeClick);
        }

        BuildThumbs();
        Select(0);

        gameObject.SetActive(true);
    }

    public void Close()
    {
        //ClearParticles();
        gameObject.SetActive(false);
    }

    public void OnDimClick()
    {
        Close();
    }

    //void ClearParticles()
    //{
    //    if(particleRoot == null) return;

    //    for(int i = particleRoot.childCount -1; i >= 0; i --)
    //    {
    //        Destroy(particleRoot.GetChild(i).gameObject);
    //    }
    //}

    // ===================== THUMBS =====================

    void BuildThumbs()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        thumbs.Clear();

        int defaultLv = current.level <= 0 ? 1 : current.level;
        int lv = CharacterProgressStore.GetLevel(currentCharacterId, defaultLv);

        for (int i = 0; i < current.photos.Count; i++)
        {
            var entry = current.photos[i];
            if (entry == null) continue;

            bool unlocked = lv >= entry.requiredLevel;

            Sprite spriteToShow = unlocked ? entry.photo : entry.lockedPhoto;

            if (spriteToShow == null)
                continue; 

            var t = Instantiate(thumbPrefab, content, false);

            int idx = i;
            t.Bind(spriteToShow, idx, unlocked, Select);  
            thumbs.Add(t);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);
    }


    void Select(int index)
    {
        if (index < 0 || index >= current.photos.Count) return;

        int defaultLv = current.level <= 0 ? 1 : current.level;
        int lv = CharacterProgressStore.GetLevel(currentCharacterId, defaultLv);

        var entry = current.photos[index];
        if (entry == null) return;

        bool unlocked = lv >= entry.requiredLevel;
        if (!unlocked)
        {
            return;
        }

        currentIndex = index;

        mainImage.sprite = entry.photo;
        commentText.text = entry.comment;

        bool liked = PhotoLikeStore.IsLiked(currentCharacterId, entry.photoId);
        UpdateLikeUI(liked);
    }

    // ===================== LIKE =====================

    void OnLikeClick()
    {
        if (current == null) return;
        if (currentIndex < 0 || currentIndex >= current.photos.Count) return;

        var entry = current.photos[currentIndex];
        if (entry == null) return;

        bool liked = PhotoLikeStore.Toggle(currentCharacterId, entry.photoId);

        UpdateLikeUI(liked);

        if (liked)
        {
            PlayLikeEffect();
        }
    }

    void UpdateLikeUI(bool liked)
    {
        if (likeIcon == null) return;
        likeIcon.color = liked ? likedColor : unlikedColor;
    }

    // ===================== PARTICLE =====================

    void PlayLikeEffect()
    {
        if (particlePrefab == null || particleRoot == null) return;

        for (int i = 0; i < 6; i++)
        {
            var p = Instantiate(particlePrefab, particleRoot);
            p.transform.position = likeBTN.transform.position;
            p.Play();
        }
    }
}
