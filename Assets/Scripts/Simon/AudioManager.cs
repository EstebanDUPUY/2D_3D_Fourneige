using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [Header("-----------------Audio Source-----------------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;
    public AudioMixerGroup masterMixerGroup;

    [Header("-----------------Music-----------------")]
    public AudioClip musicMenu;
    public AudioClip musicLevel1;
    public AudioClip musicLevel2;
    public AudioClip musicLevel3;
    public AudioClip musicLevel4;
    public AudioClip levelComplete;


    [Header("-----------------SFX-----------------")]
    public AudioClip click;
    public AudioClip dashClip;
    public AudioClip footstepFire;
    public AudioClip footstepIce;
    public AudioClip iceSound;
    public AudioClip fireSound;
    public AudioClip hurt;
    public AudioClip jumpClip;
    public AudioClip entranceLevel;
    public AudioClip selectLevel; 

    private static AudioManager instance = null;
    public static AudioManager Instance => instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // 🎵 MUSIC SOURCE
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.outputAudioMixerGroup = masterMixerGroup;

        // 🔊 SFX SOURCE
        SFXSource = gameObject.AddComponent<AudioSource>();
        SFXSource.loop = false;
        SFXSource.playOnAwake = false;
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
        else if (sceneName == "Test 1" && musicSource.clip != musicLevel1)
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
    }

    // 🔊 Jouer un bruitage
    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }
}