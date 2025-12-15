using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[Serializable]
public class PlayerScore
{
    public string name;
    public int score;
}

[Serializable]
class ScoreListWrapper
{
    public List<PlayerScore> scores = new List<PlayerScore>();
}

public class ScoreSystem2 : MonoBehaviour
{
    GameManager2 instance;
    float elapsedTime = 0f;
    public float maxTime = 600f;

    // On envoie maintenant une LISTE DE PlayerScore
    public static Action<int, List<PlayerScore>, bool> ForShowScore;

    [SerializeField]
    TextMeshProUGUI timerTMPro;

    bool stopTime;

    void Awake()
    {
        // TO DO
        // Supprime ca en prod, c'est utile uniquement pour tester chaque niveau séparement
        GameManager2.InstantiateIfNeededInDevMode();
        instance = GameManager2.instance;
    }

    void OnEnable()
    {
        LevelManager2.OnLevelComplete += SaveScore;
    }

    void OnDisable()
    {
        LevelManager2.OnLevelComplete -= SaveScore;
    }

    void Update()
    {
        if (!stopTime)
        {
            elapsedTime += Time.deltaTime;
            if (timerTMPro != null)
                timerTMPro.text = elapsedTime.ToString("F1");
        }
    }

    // Calcul du score
    int CalculateScore()
    {
        float scoreFloat = maxTime * 2;

        if (elapsedTime < maxTime)
            scoreFloat -= elapsedTime;
        else
            scoreFloat = maxTime;

        return Mathf.RoundToInt(scoreFloat);
    }

    // Sauvegarde avec nom + score
    void SaveScore()
    {
        stopTime = true;

        int scoreInt = CalculateScore();
        Debug.Log("Score calculé : " + scoreInt);

        List<PlayerScore> topScores = LoadHighScores();
        bool isNewHighScore = false;

        // Nouveau score avec NOM
        PlayerScore newEntry = new PlayerScore
        {
            name = GameManager2.instance.playerName,
            score = scoreInt,
        };

        // Vérifier si score admissible au top 3
        if (topScores.Count < 3 || scoreInt > topScores[topScores.Count - 1].score)
        {
            isNewHighScore = true;

            topScores.Add(newEntry);

            // Tri du plus grand au plus petit et garder top 3
            topScores = topScores.OrderByDescending(s => s.score).Take(3).ToList();

            SaveHighScores(topScores);
        }

        // Envoi des données à l'UI
        ForShowScore?.Invoke(scoreInt, topScores, isNewHighScore);
    }

    // Charger le top score du PlayerPrefs
    List<PlayerScore> LoadHighScores()
    {
        string json = PlayerPrefs.GetString(GetKey(), "");
        if (string.IsNullOrEmpty(json))
            return new List<PlayerScore>();

        return JsonUtility.FromJson<ScoreListWrapper>(json).scores;
    }

    // Sauvegarder le top score du PlayerPrefs
    void SaveHighScores(List<PlayerScore> scores)
    {
        ScoreListWrapper wrapper = new ScoreListWrapper { scores = scores };

        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(GetKey(), json);
        PlayerPrefs.Save();

        Debug.Log("Save OK pour : " + GetKey());
    }

    // Reset si besoin
    void ResetHighScores()
    {
        PlayerPrefs.DeleteKey(GetKey());
        PlayerPrefs.Save();
        Debug.Log("HighScores réinitialisés pour : " + instance.currentIndexLevel);
    }

    // Clé unique pour chaque niveau
    string GetKey()
    {
        return instance.sceneConfig.keyTopScoreLevels + instance.currentIndexLevel;
    }
}
