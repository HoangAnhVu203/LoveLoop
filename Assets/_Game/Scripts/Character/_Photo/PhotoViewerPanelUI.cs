using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PhotoViewerPanelUI : UICanvas
{
    [Header("Main")]
    [SerializeField] Image mainImage;               
    [SerializeField] SkeletonGraphic mainSpine;    
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
        StopMainSpine();
        gameObject.SetActive(false);
    }

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

            // THUMB vẫn là sprite như bạn muốn
            Sprite thumbSprite = unlocked ? entry.photo : entry.lockedPhoto;
            if (thumbSprite == null) continue;

            var t = Instantiate(thumbPrefab, content, false);
            int idx = i;
            t.Bind(thumbSprite, idx, unlocked, Select);
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
        if (!unlocked) return;

        currentIndex = index;

        commentText.text = entry.comment;

        // MAIN: ưu tiên spine, nếu spineAsset null thì fallback sprite
        ShowMain(entry);

        bool liked = PhotoLikeStore.IsLiked(currentCharacterId, entry.photoId);
        UpdateLikeUI(liked);
    }

    // ===================== MAIN SHOW =====================

    void ShowMain(CharacterPhotoEntry entry)
    {
        if (entry.spineAsset != null && mainSpine != null)
        {
            // bật spine, tắt image
            if (mainImage != null) mainImage.gameObject.SetActive(false);
            mainSpine.gameObject.SetActive(true);

            // reset state trước khi đổi
            mainSpine.AnimationState?.ClearTracks();
            mainSpine.skeletonDataAsset = entry.spineAsset;
            mainSpine.Initialize(true);

            string anim = string.IsNullOrEmpty(entry.loopAnimation) ? "animation" : entry.loopAnimation;
            bool loop = entry.loop;

            mainSpine.AnimationState.SetAnimation(0, anim, loop);
        }
        else
        {
            // fallback sprite
            StopMainSpine();

            if (mainImage != null)
            {
                mainImage.gameObject.SetActive(true);
                mainImage.sprite = entry.photo; 
            }
        }
    }

    void StopMainSpine()
    {
        if (mainSpine == null) return;

        // tắt spine và clear để dừng CPU
        mainSpine.AnimationState?.ClearTracks();
        mainSpine.gameObject.SetActive(false);
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

    public void OnDimClick()
    {
        gameObject.SetActive(false);
    }

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
