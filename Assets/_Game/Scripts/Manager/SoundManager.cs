using UnityEngine;
using CandyCoded.HapticFeedback;
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    const string KEY_SOUND = "SOUND_ON";
    const string KEY_MUSIC = "MUSIC_ON";
    const string KEY_VIBRATION = "VIBRATION_ON";

    [Header("Audio Sources")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;

    public bool SoundOn { get; private set; }
    public bool MusicOn { get; private set; }
    public bool VibrationOn { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplyAll();
    }

    // ================= LOAD / SAVE =================

    void LoadSettings()
    {
        SoundOn     = PlayerPrefs.GetInt(KEY_SOUND, 1) == 1;
        MusicOn     = PlayerPrefs.GetInt(KEY_MUSIC, 1) == 1;
        VibrationOn = PlayerPrefs.GetInt(KEY_VIBRATION, 1) == 1;
    }

    void Save()
    {
        PlayerPrefs.SetInt(KEY_SOUND, SoundOn ? 1 : 0);
        PlayerPrefs.SetInt(KEY_MUSIC, MusicOn ? 1 : 0);
        PlayerPrefs.SetInt(KEY_VIBRATION, VibrationOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ================= APPLY =================

    void ApplyAll()
    {
        ApplyMusic();
        ApplySound();
    }

    void ApplyMusic()
    {
        if (musicSource == null) return;

        musicSource.mute = !MusicOn;

        if (MusicOn && !musicSource.isPlaying)
            musicSource.Play();
        else if (!MusicOn && musicSource.isPlaying)
            musicSource.Pause();
    }

    void ApplySound()
    {
        if (sfxSource == null) return;
        sfxSource.mute = !SoundOn;
    }

    // ================= PUBLIC API =================

    public void SetMusic(bool on)
    {
        MusicOn = on;
        ApplyMusic();
        Save();
    }

    public void SetSound(bool on)
    {
        SoundOn = on;
        ApplySound();
        Save();
    }

    public void SetVibration(bool on)
    {
        VibrationOn = on;
        Save();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (!SoundOn || clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void Vibrate()
    {
        if (!VibrationOn) return;
#if UNITY_ANDROID || UNITY_IOS
        HapticFeedback.LightFeedback();
#endif
    }
}
