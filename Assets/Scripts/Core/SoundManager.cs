using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Volume Settings (0.0 to 1.0)")]
    [Range(0f, 1f)][SerializeField] private float musicVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;

    [Header("Audio Clips - Music")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameplayMusic;

    [Header("Audio Clips - SFX & Voice")]
    [SerializeField] private AudioClip jellyVfxSound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip lossSound;
    [SerializeField] private AudioClip dropSound;

    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            if (musicSource != null) musicSource.volume = musicVolume;
        }
    }

    public float SFXVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            if (sfxSource != null) sfxSource.volume = sfxVolume;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
    }



    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;

        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlaySFXWithPitch(AudioClip clip, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.pitch = Random.Range(minPitch, maxPitch);
        sfxSource.PlayOneShot(clip, sfxVolume);
        sfxSource.pitch = 1f;
    }


    /// <summary>
    /// Phát nhạc nền Main Menu
    /// </summary>
    public void PlayMainMenuMusic()
    {
        PlayMusic(mainMenuMusic);
    }

    /// <summary>
    /// Phát nhạc nền Gameplay
    /// </summary>
    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic);
    }

    /// <summary>
    /// Phát âm thanh hiệu ứng Jelly (kéo thả / va chạm) kèm biến thiên Pitch cho tự nhiên
    /// </summary>
    public void PlayJellyVFX()
    {
        PlaySFXWithPitch(jellyVfxSound, 0.9f, 1.1f);
    }

    /// <summary>
    /// Phát âm thanh Thắng màn (Win)
    /// </summary>
    public void PlayWinSound()
    {
        PlaySFX(winSound);
    }

    /// <summary>
    /// Phát âm thanh Thua màn (Loss)
    /// </summary>
    public void PlayLossSound()
    {
        PlaySFX(lossSound);
    }
    public void PlayDropSound()
    {
        PlaySFX(dropSound);
    }    
}