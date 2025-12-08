using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public string homeScene = "HomeScene";
    public string firstScene = "FirstScene";

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Use just for test scene independently in dev mode
    // in prod, the GameManager.instance should be create in HomeScene
    public static void InstantiateIfNeededInDevMode()
    {
        if (instance == null)
        {
            // Create new gameObject in the scene
            GameObject gm = new GameObject("GameManager");
            instance = gm.AddComponent<GameManager>();
            // DontDestroyOnLoad(gm);
        }
    }

    // Load Scene

    public void ComeToHomeScene()
    {
        SceneManager.LoadScene(homeScene);
    }

    public void GoToFirstScene()
    {
        SceneManager.LoadScene(firstScene);
    }
}
