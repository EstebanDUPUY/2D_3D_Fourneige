using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager2 : MonoBehaviour
{
    public SceneConfig sceneConfig;
    public static GameManager2 instance;
    public string homeScene = "HomeScene";
    public string firstScene = "Test";
    const string HighScoreKEY = "HighScores";

    public string playerName;

    public int currentIndexLevel;
    public int test;

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
        if (sceneConfig == null)
        {
            sceneConfig = Resources.Load<SceneConfig>("ScriptableObjects/Config/SceneConfig");
        }
    }

    // Use just for test scene independently in dev mode
    // in prod, the GameManager2.instance should be create in HomeScene
    public static void InstantiateIfNeededInDevMode()
    {
        if (instance == null)
        {
            // Create new gameObject in the scene
            GameObject gm = new GameObject("GameManager2");
            instance = gm.AddComponent<GameManager2>();
            instance.playerName = "PlayerNameTest";
            // DontDestroyOnLoad(gm);
        }
    }

    // Load Scene

    public void ComeToHomeScene()
    {
        SceneManager.LoadScene(sceneConfig.homeScene);
    }

    public void GoToFirstScene()
    {
        currentIndexLevel = 0;
        SceneManager.LoadScene(sceneConfig.levels[0]);
    }

    public void LoadScene(string nameScene)
    {
        SceneManager.LoadScene(nameScene);
    }

    public void LoadNextScene()
    {
        instance.currentIndexLevel += 1;
        instance.LoadScene(instance.sceneConfig.levels[instance.currentIndexLevel]);
    }

    public bool HasNextScene()
    {
        if (instance.currentIndexLevel < instance.sceneConfig.levels.Count() - 1)
        {
            return true;
        }
        return false;
    }

    public static List<int> LoadScores()
    {
        if (!PlayerPrefs.HasKey(HighScoreKEY))
            return new List<int>();

        string json = PlayerPrefs.GetString(HighScoreKEY);
        ScoreListWrapper wrapper = JsonUtility.FromJson<ScoreListWrapper>(json);

        return wrapper?.scores ?? new List<int>();
    }

    public static List<int> GetScoreForLevel(int indexLevel)
    {
        string scoreKey = instance.sceneConfig.keyTopScoreLevels + indexLevel;
        // if (!PlayerPrefs.HasKey(scoreKey))
        //     return new List<int>();

        // string json = PlayerPrefs.GetString(scoreKey);
        // ScoreListWrapper wrapper = JsonUtility.FromJson<ScoreListWrapper>(json);

        // return wrapper?.scores ?? new List<int>();

        string json = PlayerPrefs.GetString(scoreKey, "");
        if (string.IsNullOrEmpty(json))
            return new List<int>();

        return JsonUtility.FromJson<ScoreListWrapper>(json).scores;
    }

    public void LoadGame(Game game)
    {
        if (game == null)
        {
            Debug.LogError("LoadGame : game est null !");
            return;
        }

        // Mettre à jour les infos du joueur
        playerName = game.playerName;
        currentIndexLevel = Mathf.Clamp(game.levelReached, 0, sceneConfig.levels.Count() - 1);

        Debug.Log(
            $"Chargement de la game : {playerName}, niveau {currentIndexLevel}, score {game.totalScore}"
        );

        // Charger la scène correspondante au niveau atteint
        SceneManager.LoadScene(sceneConfig.levels[currentIndexLevel]);
    }

    [System.Serializable]
    public class ScoreListWrapper
    {
        public List<int> scores;
    }
}
