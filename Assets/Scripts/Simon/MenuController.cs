using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public static bool open = false;
    private static MenuController instance = null;
    public static MenuController Instance => instance;

    [Header("UI")]
    public Toggle fullscreenToggle;
    public Image toggleImage;
    public Sprite spriteOn;
    public Sprite spriteOff;
    public Slider slider;
    Resolution[] resolutions;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    public AudioMixer audioMixer;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }


    private void Start()
    {
        bool isFullscreen = Screen.fullScreen;

        fullscreenToggle.onValueChanged.RemoveAllListeners();
        fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
        UpdateSprite(isFullscreen);
        fullscreenToggle.onValueChanged.AddListener(SetFullScreen);

        InitResolutionMenu();
    }

    private void InitResolutionMenu()
    {
        // Liste "safe" de base
        Resolution[] baseResolutions = new Resolution[]
        {
            new Resolution { width = 3840, height = 2160 },
            new Resolution { width = 2560, height = 1440 },
            new Resolution { width = 1920, height = 1080 },
            new Resolution { width = 1600, height = 900 },
            new Resolution { width = 1280, height = 720 }
        };

        // On garde uniquement celles supportées par l’écran
        resolutions = baseResolutions
            .Where(r => r.width <= Screen.currentResolution.width &&
                        r.height <= Screen.currentResolution.height)
            .ToArray();

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add($"{resolutions[i].width} x {resolutions[i].height}");

            if (resolutions[i].width == Screen.width &&
                resolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    private void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;

        if (toggleImage != null)
            toggleImage.sprite = isFullScreen ? spriteOn : spriteOff;
    }

    private void UpdateSprite(bool isFullScreen)
    {
        if (toggleImage != null)
            toggleImage.sprite = isFullScreen ? spriteOn : spriteOff;
    }

    public void SetVolume(float value)
    {
        // Protection contre 0 : on met un tout petit nombre
        if (value <= 0.0001f)
            value = 0.0001f;

        float dB = Mathf.Log10(value) * 20;
        audioMixer.SetFloat("MasterVolume", dB);

        // Sauvegarde
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    public void PlayClick()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound(AudioManager.Instance.click);
    }
}