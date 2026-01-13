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
        if (resolutionDropdown == null)
        {
            Debug.LogError("Resolution Dropdown non assigné !");
            return;
        }

        // --- Configuration du Fullscreen ---
        bool isFullscreen = Screen.fullScreen;
        fullscreenToggle.onValueChanged.RemoveAllListeners();
        fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
        UpdateSprite(isFullscreen);
        fullscreenToggle.onValueChanged.AddListener(SetFullScreen);

        // --- Initialisation des Résolutions ---
        // On appelle ta fonction ici au lieu du code de test !
        InitResolutionMenu();
        
        // Optionnel : Charger le volume sauvegardé
        if (slider != null) {
            float savedVol = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
            slider.value = savedVol;
            SetVolume(savedVol);
        }
    }

    private void InitResolutionMenu()
    {
        // Liste des résolutions.
        List<Resolution> baseResolutions = new List<Resolution>
        {
            new Resolution { width = 2560, height = 1600 },
            new Resolution { width = 2560, height = 1440 },
            new Resolution { width = 1920, height = 1200 },
            new Resolution { width = 1920, height = 1080 },
            new Resolution { width = 1680, height = 1050 },
            new Resolution { width = 1600, height = 900 },
            new Resolution { width = 1440, height = 900 },
            new Resolution { width = 1366, height = 768 },
            new Resolution { width = 1280, height = 800 },
            new Resolution { width = 1280, height = 720 }
        };

        // On ne garde que ce que l'écran actuel peut supporter physiquement
        resolutions = baseResolutions
            .Where(r => r.width <= Screen.currentResolution.width && 
                        r.height <= Screen.currentResolution.height)
            .OrderByDescending(r => r.width) // Trie du plus grand au plus petit
            .ToArray();

        // Sécurité au cas où aucune résolution ne match
        if (resolutions.Length == 0)
        {
            resolutions = new Resolution[] { Screen.currentResolution };
        }

        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            // Détection de la résolution actuelle pour l'index par défaut
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
        resolutionDropdown.RefreshShownValue();
        
        // On nettoie les anciens listeners avant d'en ajouter un nouveau
        resolutionDropdown.onValueChanged.RemoveAllListeners();
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