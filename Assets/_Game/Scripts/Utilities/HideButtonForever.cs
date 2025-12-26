using UnityEngine;
using UnityEngine.UI;

public class HideButtonForever : MonoBehaviour
{
    const string KEY = "HIDE_MY_BUTTON";

    [SerializeField] GameObject banner;

    void Start()
    {
        if (PlayerPrefs.GetInt(KEY, 0) == 1)
        {
            gameObject.SetActive(false);
            banner.SetActive(false);
        }
    }

    public void OnButtonClick()
    {
        PlayerPrefs.SetInt(KEY, 1);
        PlayerPrefs.Save();
        banner.SetActive(false);
        gameObject.SetActive(false);
    }
}
