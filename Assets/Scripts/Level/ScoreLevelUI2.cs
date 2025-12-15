using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreLevelUI : MonoBehaviour
{
    public GameObject scorePanel;
    public TextMeshProUGUI scoreTMPro;
    public TextMeshProUGUI messageScoreTMPro;
    public TextMeshProUGUI isTopScorerTMPro;
    public TextMeshProUGUI topScoresTMPro;

    // Menu
    LevelManager2 levelManager;

    public Button homeButton;
    public Button playAgainButton;
    public Button nextLevelButton;

    public int scoreForGold;
    public string messageForGold = "Rang Or";

    public int scoreForSilver;
    public string messageForSilver = "Rang Silver";

    public int scoreForBronze;
    public string messageForBronze = "Rang Bronze";

    public string messageForNormal = "Rang Normal";

    void Awake()
    {
        levelManager = GetComponent<LevelManager2>();

        scorePanel.SetActive(false);

        homeButton.onClick.AddListener(() => GameManager2.instance.ComeToHomeScene());
        playAgainButton.onClick.AddListener(() =>
            GameManager2.instance.LoadScene(
                GameManager2.instance.sceneConfig.levels[GameManager2.instance.currentIndexLevel]
            )
        );
        if (GameManager2.instance.HasNextScene())
        {
            nextLevelButton.onClick.AddListener(() => GameManager2.instance.LoadNextScene());
        }
        else if (null != nextLevelButton)
        {
            nextLevelButton.gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        ScoreSystem2.ForShowScore += ShowScore;
    }

    void OnDisable()
    {
        ScoreSystem2.ForShowScore -= ShowScore;
    }

    void ShowScore(int score, List<PlayerScore> topScores, bool isNewHighScore)
    {
        scorePanel.SetActive(true);

        // Score actuel
        scoreTMPro.text = score + " Pts";

        // Message selon le score
        if (score >= scoreForGold)
            messageScoreTMPro.text = messageForGold;
        else if (score >= scoreForSilver)
            messageScoreTMPro.text = messageForSilver;
        else if (score >= scoreForBronze)
            messageScoreTMPro.text = messageForBronze;
        else
            messageScoreTMPro.text = messageForNormal;

        // --- Message Top Scorer ---
        if (isNewHighScore)
            isTopScorerTMPro.text = "Bravo, vous faites partie des meilleurs joueurs.";
        else
        {
            isTopScorerTMPro.text = "Pas de Top 3 pour cette fois.";
        }

        // --- Affichage du Top 3 ---
        string topText = "Top 3 :\n";

        int count = Mathf.Min(3, topScores.Count);

        for (int i = 0; i < count; i++)
        {
            PlayerScore entry = topScores[i];

            bool isHighlighted = (
                entry.score == score
                && entry.name == GameManager2.instance.playerName
                && isNewHighScore
            );

            if (isHighlighted)
            {
                // ligne en doré
                topText += $"<color=#FFD700>{i + 1}. {entry.name} - {entry.score} pts</color>\n";
            }
            else
            {
                topText += $"{i + 1}. {entry.name} - {entry.score} pts\n";
            }
        }

        topScoresTMPro.text = topText;
    }
}
