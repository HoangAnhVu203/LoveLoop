using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleSwitchUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] RectTransform handle;     
    [SerializeField] Image background;         

    [Header("Positions (anchored X)")]
    [SerializeField] float offX = -20f;        
    [SerializeField] float onX  =  20f;        

    [Header("Colors")]
    [SerializeField] Color offBgColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] Color onBgColor  = new Color(0.20f, 0.80f, 0.35f, 1f); 

    [Header("Animation")]
    [SerializeField] float animDuration = 0.12f;
    [SerializeField] bool instantOnEnable = true;

    Toggle _toggle;
    Coroutine _cr;

    void Awake()
    {
        _toggle = GetComponent<Toggle>();
        if (background == null) background = GetComponentInChildren<Image>();
    }

    void OnEnable()
    {
        _toggle.onValueChanged.AddListener(OnToggleChanged);

        if (instantOnEnable)
            ApplyInstant(_toggle.isOn);
        else
            PlayAnim(_toggle.isOn);
    }

    void OnDisable()
    {
        _toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    void OnToggleChanged(bool isOn)
    {
        PlayAnim(isOn);
    }

    void ApplyInstant(bool isOn)
    {
        if (handle != null)
        {
            var p = handle.anchoredPosition;
            p.x = isOn ? onX : offX;
            handle.anchoredPosition = p;
        }

        if (background != null)
            background.color = isOn ? onBgColor : offBgColor;
    }

    void PlayAnim(bool isOn)
    {
        if (_cr != null) StopCoroutine(_cr);
        _cr = StartCoroutine(AnimCR(isOn));
    }

    IEnumerator AnimCR(bool isOn)
    {
        float targetX = isOn ? onX : offX;
        Color targetC = isOn ? onBgColor : offBgColor;

        Vector2 startPos = handle != null ? handle.anchoredPosition : Vector2.zero;
        float startX = startPos.x;

        Color startC = background != null ? background.color : Color.white;

        float t = 0f;
        float d = Mathf.Max(0.01f, animDuration);

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / d;
            float eased = 1f - Mathf.Pow(1f - t, 3f); 
            if (handle != null)
            {
                var p = handle.anchoredPosition;
                p.x = Mathf.Lerp(startX, targetX, eased);
                handle.anchoredPosition = p;
            }

            if (background != null)
                background.color = Color.Lerp(startC, targetC, eased);

            yield return null;
        }

        ApplyInstant(isOn);
        _cr = null;
    }
}
