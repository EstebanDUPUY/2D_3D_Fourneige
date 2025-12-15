using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [Header("-----------------Audio Source-----------------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;
    public AudioMixerGroup masterMixerGroup;

    [Header("-----------------Audio Source-----------------")]
    public AudioClip musicMenu;
    public AudioClip musicSelectLevel;
    public AudioClip musicLevel1;
    public AudioClip musicLevel2;
    public AudioClip musicLevel3;
    public AudioClip musicLevel4;
    public AudioClip musicLevel5;


    public AudioClip click;
    public AudioClip dash;
    public AudioClip entranceLevel;
    public AudioClip footstepFire;
    public AudioClip footstepIce;
    public AudioClip IceFX;
    public AudioClip fireFX;
    public AudioClip toggleFX;   
    public AudioClip happy;
    public AudioClip jump;
    public AudioClip swoosh1;
    public AudioClip swoosh2;
    public AudioClip swoosh3;
    public AudioClip text;   
    public AudioClip levelComplete;   

    private static AudioManager instance = null;
    public static AudioManager Instance => instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            instance = this;
        }
        DontDestroyOnLoad(this.gameObject);

        // Source pour la musique
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.outputAudioMixerGroup = masterMixerGroup;

        // Source pour les bruitages
        SFXSource = gameObject.AddComponent<AudioSource>();
        SFXSource.loop = false;
        SFXSource.outputAudioMixerGroup = masterMixerGroup;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    public void PlaySound(AudioClip clip) 
    { 
        SFXSource.PlayOneShot(clip);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    void PlayMusicForScene(string sceneName)
    {
        if (sceneName == "HomeScene" && musicSource.clip != musicMenu)
        {
            musicSource.clip = musicMenu;
            musicSource.Play();
        }
        else if (sceneName == "SelectLevel" && musicSource.clip != musicSelectLevel)
        {
            musicSource.clip = musicSelectLevel;
            musicSource.Play();
        }
        else if (sceneName == "Test" && musicSource.clip != musicLevel1)
        {
            musicSource.clip = musicLevel1;
            musicSource.Play();
        }
        else if (sceneName == "Test 2" && musicSource.clip != musicLevel2)
        {
            musicSource.clip = musicLevel2;
            musicSource.Play();
        }
        else if (sceneName == "Level" && musicSource.clip != musicLevel3)
        {
            musicSource.clip = musicLevel3;
            musicSource.Play();
        }
        else if (sceneName == "Level" && musicSource.clip != musicLevel4)
        {
            musicSource.clip = musicLevel4;
            musicSource.Play();
        }
        else if (sceneName == "Level" && musicSource.clip != musicLevel5)
        {
            musicSource.clip = musicLevel5;
            musicSource.Play();
        }
    }

    // 🔊 Jouer un bruitage
    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }
}
