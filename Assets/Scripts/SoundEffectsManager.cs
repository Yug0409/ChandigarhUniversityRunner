using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoundEffectsManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource effectsSource;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private bool playMusicOnAwake = true;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip gameStartSound;
    [SerializeField] private AudioClip laneChangeSound;
    [SerializeField] private AudioClip coinPickupSound;
    [SerializeField] private AudioClip obstacleHitSound;
    [SerializeField] private AudioClip finishSound;
    [SerializeField] private AudioClip retrySound;
    [SerializeField, Range(0f, 1f)] private float effectsVolume = 1f;

    [Header("Volume Toggle")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Button volumeButton;
    [SerializeField] private TMP_Text volumeButtonText;
    [SerializeField] private Image volumeIcon;
    [SerializeField] private Sprite volumeOnSprite;
    [SerializeField] private Sprite volumeOffSprite;
    [SerializeField] private bool startMuted;

    private Text fallbackVolumeButtonText;
    private bool muted;

    public bool IsMuted => muted;

    private void Awake()
    {
        ConfigureAudioSources();
        CreateVolumeButtonIfNeeded();

        muted = startMuted;
        ApplyMuteState();

        if (playMusicOnAwake)
        {
            PlayBackgroundMusic();
        }
    }

    public void ToggleMute()
    {
        muted = !muted;
        ApplyMuteState();
    }

    public void SetMuted(bool value)
    {
        muted = value;
        ApplyMuteState();
    }

    public void PlayButtonClick() => PlayEffect(buttonClickSound);

    public void PlayGameStart() => PlayEffect(gameStartSound);

    public void PlayLaneChange() => PlayEffect(laneChangeSound);

    public void PlayCoinPickup() => PlayEffect(coinPickupSound);

    public void PlayObstacleHit() => PlayEffect(obstacleHitSound);

    public void PlayFinish() => PlayEffect(finishSound);

    public void PlayRetry() => PlayEffect(retrySound);

    private void ConfigureAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (effectsSource == null)
        {
            effectsSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;
        musicSource.clip = backgroundMusic;

        effectsSource.playOnAwake = false;
        effectsSource.loop = false;
        effectsSource.spatialBlend = 0f;
        effectsSource.volume = effectsVolume;
    }

    private void PlayBackgroundMusic()
    {
        if (backgroundMusic == null || musicSource == null)
            return;

        if (musicSource.clip != backgroundMusic)
        {
            musicSource.clip = backgroundMusic;
        }

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    private void PlayEffect(AudioClip clip)
    {
        if (clip != null && effectsSource != null)
        {
            effectsSource.PlayOneShot(clip);
        }
    }

    private void ApplyMuteState()
    {
        if (musicSource != null)
        {
            musicSource.mute = muted;
        }

        if (effectsSource != null)
        {
            effectsSource.mute = muted;
        }

        if (volumeIcon != null)
        {
            volumeIcon.sprite = muted ? volumeOffSprite : volumeOnSprite;
            volumeIcon.enabled = volumeIcon.sprite != null;
        }

        if (volumeButtonText != null)
        {
            volumeButtonText.text = muted ? "SOUND OFF" : "SOUND ON";
        }

        if (fallbackVolumeButtonText != null)
        {
            fallbackVolumeButtonText.text = muted ? "SOUND OFF" : "SOUND ON";
        }
    }

    private void CreateVolumeButtonIfNeeded()
    {
        if (volumeButton == null && uiCanvas != null)
        {
            GameObject buttonObject = new GameObject(
                "VolumeButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );

            RectTransform buttonRect =
                buttonObject.GetComponent<RectTransform>();

            buttonRect.SetParent(uiCanvas.transform, false);
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-24f, -24f);
            buttonRect.sizeDelta = new Vector2(170f, 58f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.06f, 0.1f, 0.09f, 0.88f);

            volumeButton = buttonObject.GetComponent<Button>();
            volumeButton.targetGraphic = buttonImage;

            GameObject labelObject = new GameObject(
                "VolumeLabel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );

            RectTransform labelRect =
                labelObject.GetComponent<RectTransform>();

            labelRect.SetParent(buttonRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            fallbackVolumeButtonText = labelObject.GetComponent<Text>();
            fallbackVolumeButtonText.font =
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fallbackVolumeButtonText.fontSize = 22;
            fallbackVolumeButtonText.fontStyle = FontStyle.Bold;
            fallbackVolumeButtonText.alignment = TextAnchor.MiddleCenter;
            fallbackVolumeButtonText.color = Color.white;
            fallbackVolumeButtonText.raycastTarget = false;
        }

        if (volumeButton != null)
        {
            volumeButton.onClick.RemoveListener(ToggleMute);
            volumeButton.onClick.AddListener(ToggleMute);

            if (volumeButtonText == null)
            {
                volumeButtonText =
                    volumeButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (fallbackVolumeButtonText == null && volumeButtonText == null)
            {
                fallbackVolumeButtonText =
                    volumeButton.GetComponentInChildren<Text>(true);
            }
        }

        ApplyMuteState();
    }
}
