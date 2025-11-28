using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [Header("-----------------Audio Source-----------------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("-----------------Audio Source-----------------")]
    public AudioClip musicIntro;
    public AudioClip musicSimon;
    public AudioClip musicMaeva;
    public AudioClip doorOpen;
    public AudioClip clickButton;
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

        // Source pour les bruitages
        SFXSource = gameObject.AddComponent<AudioSource>();
        SFXSource.loop = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    void PlayMusicForScene(string sceneName)
    {
        if (sceneName == "MainMenu" && musicSource.clip != musicIntro)
        {
            musicSource.clip = musicIntro;
            musicSource.Play();
        }
        else if (sceneName == "Intro" && musicSource.clip != musicIntro)
        {
            musicSource.clip = musicIntro;
            musicSource.Play();
        }
        else if (sceneName == "SimonModifs" && musicSource.clip != musicSimon)
        {
            musicSource.clip = musicSimon;
            musicSource.Play();
        }
        else if (sceneName == "MaevaModifs" && musicSource.clip != musicMaeva)
        {
            musicSource.clip = musicMaeva;
            musicSource.Play();
        }
    }

    // 🔊 Jouer un bruitage
    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }
}
