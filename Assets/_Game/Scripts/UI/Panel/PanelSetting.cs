using UnityEngine;
using UnityEngine.UI;

public class PanelSetting : UICanvas
{
    [Header("Toggles")]
    [SerializeField] Toggle soundToggle;
    [SerializeField] Toggle musicToggle;
    [SerializeField] Toggle vibrationToggle;

    bool _isInit;

    void OnEnable()
    {
        Init();
    }

    void Init()
    {
        if (SoundManager.Instance == null) return;

        _isInit = true;

        soundToggle.isOn     = SoundManager.Instance.SoundOn;
        musicToggle.isOn     = SoundManager.Instance.MusicOn;
        vibrationToggle.isOn = SoundManager.Instance.VibrationOn;

        soundToggle.onValueChanged.AddListener(OnSoundToggle);
        musicToggle.onValueChanged.AddListener(OnMusicToggle);
        vibrationToggle.onValueChanged.AddListener(OnVibrationToggle);

        _isInit = false;
    }

    void OnDisable()
    {
        soundToggle.onValueChanged.RemoveListener(OnSoundToggle);
        musicToggle.onValueChanged.RemoveListener(OnMusicToggle);
        vibrationToggle.onValueChanged.RemoveListener(OnVibrationToggle);
    }

    // ================= TOGGLE CALLBACKS =================

    void OnSoundToggle(bool on)
    {
        if (_isInit) return;
        SoundManager.Instance.SetSound(on);
    }

    void OnMusicToggle(bool on)
    {
        if (_isInit) return;
        SoundManager.Instance.SetMusic(on);
    }

    void OnVibrationToggle(bool on)
    {
        if (_isInit) return;
        SoundManager.Instance.SetVibration(on);
    }

    // ================= BUTTON =================

    public void CloseBTN()
    {
        UIManager.Instance.CloseUIDirectly<PanelSetting>();
    }

    public void RestorePurchaseBTN()
    {
        //TODO: RESTORE DATA
    }
}
