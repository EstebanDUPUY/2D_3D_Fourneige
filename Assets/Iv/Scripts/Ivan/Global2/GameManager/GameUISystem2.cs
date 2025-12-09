using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class GameUISystem2 : MonoBehaviour
{
    [Header("Action")]
    [SerializeField]
    Button playButton;

    [SerializeField]
    Button quitButton;

    [Header("Panel Buttons")]
    [Header("Instructions")]
    [SerializeField]
    Button instructionsButton;

    [SerializeField]
    GameObject instructionsPanel;

    [Header("Options")]
    [SerializeField]
    Button optionsButton;

    [SerializeField]
    GameObject optionsPanel;

    [Header("Credits")]
    [SerializeField]
    Button creditsButton;

    [SerializeField]
    GameObject creditsPanel;

    [Header("Home")]
    [SerializeField]
    GameObject homePanel;

    [SerializeField]
    Button[] homeButtons;

    [Header("Score")]
    [SerializeField]
    Button scoresButton;

    [SerializeField]
    GameObject scoresPanel;

    [Header("ConfirmQuit")]
    [SerializeField]
    Button yesQuitButton;

    [SerializeField]
    Button noQuitButton;

    [SerializeField]
    GameObject confirmQuitPanel;

    [Header("ResetAllScore")]
    [SerializeField]
    Button resetAllScoreButton;

    [SerializeField]
    Button yesResetScoreButton;

    [SerializeField]
    Button noResetScoreButton;

    [SerializeField]
    GameObject confirmResetScorePanel;

    [Header("ScoreByLevel")]
    [SerializeField]
    TextMeshProUGUI levelName;

    [SerializeField]
    TextMeshProUGUI topScoresTMPro;

    [SerializeField]
    Button ScoreByLevelButton;

    [SerializeField]
    Button ScoreByLevelCloseButton;

    [SerializeField]
    Button NextLevelScoreButton;

    [SerializeField]
    Button PreviousLevelScoreButton;

    [SerializeField]
    GameObject ScoreByLevelPanel;

    [Header("LoadGamePanel")]
    [SerializeField]
    GameObject loadGamePanel;

    [SerializeField]
    Button createGameButton;

    [Header("CreateGame")]
    [SerializeField]
    GameObject createGamePanel;

    [Header("ConfirmCreateGame")]
    [SerializeField]
    GameObject confirmCreateGamePanel;

    [SerializeField]
    TextMeshProUGUI confirmPlayerNameTMPro;

    [SerializeField]
    Button backLoadGameButton;

    [SerializeField]
    Button yesCreateGameButton;

    [SerializeField]
    Button noCreateGameButton;

    [Header("Nom de la clé PlayerPrefs")]
    public string keyPlayerName = "PlayerName";

    [Header("Ever3GamesCreated")]
    [SerializeField]
    GameObject ever3GamesCreatedPanel;

    [SerializeField]
    Button closeEver3GamesCreatedButton;

    [Header("Membres")]
    [SerializeField]
    Panel currentPanel = Panel.Home;

    SceneConfig sceneConfig;

    int currentIndexLevel;

    enum Panel
    {
        Home,
        Instructions,
        Options,
        Scores,
        Credits,

        LoadGame,
        CreateGame,
    }

    void OnEnable()
    {
        InputFieldManager2.ValidateName += ActiveConfirmCreateGamePanel;
    }

    void OnDisable()
    {
        InputFieldManager2.ValidateName -= ActiveConfirmCreateGamePanel;
    }

    void Awake()
    {
        sceneConfig = GameManager2.instance.sceneConfig;
        // Action
        playButton.onClick.AddListener(() => OnPlayClick());

        quitButton.onClick.AddListener(() => confirmQuitPanel.SetActive(true));

        // Panel

        foreach (Button homeButton in homeButtons)
        {
            homeButton.onClick.AddListener(() => OnHomeClick());
        }

        optionsButton.onClick.AddListener(() => OnOptionsClick());
        instructionsButton.onClick.AddListener(() => OnInstructionsClick());
        scoresButton.onClick.AddListener(() => OnScoresClick());
        creditsButton.onClick.AddListener(() => OnCreditsClick());

        yesQuitButton.onClick.AddListener(() => QuitApplication());
        noQuitButton.onClick.AddListener(() => confirmQuitPanel.SetActive(false));

        resetAllScoreButton.onClick.AddListener(() => confirmResetScorePanel.SetActive(true));
        yesResetScoreButton.onClick.AddListener(() => ResetScore());
        noResetScoreButton.onClick.AddListener(() => confirmResetScorePanel.SetActive(false));

        ScoreByLevelButton.onClick.AddListener(() => ScoreByLevelPanel.SetActive(true));
        ScoreByLevelCloseButton.onClick.AddListener(() => ScoreByLevelPanel.SetActive(false));

        NextLevelScoreButton.onClick.AddListener(() => NextLevelScoreShow());
        PreviousLevelScoreButton.onClick.AddListener(() => PreviousLevelScoreShow());

        noCreateGameButton.onClick.AddListener(() => confirmCreateGamePanel.SetActive(false));
        yesCreateGameButton.onClick.AddListener(() => CreateGame());

        createGameButton.onClick.AddListener(() => GoCreateGame());

        backLoadGameButton.onClick.AddListener(() => BackLoadGameButton());

        closeEver3GamesCreatedButton.onClick.AddListener(() =>
            ever3GamesCreatedPanel.SetActive(false)
        );

        currentIndexLevel = 0;
        WriteTopScores();
        PreviousLevelScoreButton.enabled = false;
        if (sceneConfig.levels.Count() == 0)
        {
            NextLevelScoreButton.enabled = false;
        }
    }

    void ActiveConfirmCreateGamePanel()
    {
        confirmCreateGamePanel.SetActive(true);
        confirmPlayerNameTMPro.text = GameManager2.instance.playerName;
    }

    // Evenement On Click
    // Actions

    public void QuitApplication()
    {
        Debug.Log("Quitter Application"); // TO DO : Delete for Build
        Application.Quit();
    }

    public void OnPlayClick()
    {
        ChangeCurrentPanel(Panel.LoadGame);
        // Debug.Log("Play First Scene"); // TO DO : Delete for Build
        // GameManager2.instance.GoToFirstScene();
    }

    // Panel
    public void OnHomeClick()
    {
        ChangeCurrentPanel(Panel.Home);
    }

    public void OnInstructionsClick()
    {
        ChangeCurrentPanel(Panel.Instructions);
    }

    public void OnOptionsClick()
    {
        ChangeCurrentPanel(Panel.Options);
    }

    public void OnScoresClick()
    {
        ChangeCurrentPanel(Panel.Scores);
    }

    void NextLevelScoreShow()
    {
        currentIndexLevel++;
        WriteTopScores();
        if (currentIndexLevel >= sceneConfig.levels.Count() - 1)
        {
            NextLevelScoreButton.enabled = false;
        }
        PreviousLevelScoreButton.enabled = true;
    }

    void PreviousLevelScoreShow()
    {
        currentIndexLevel--;
        WriteTopScores();
        if (currentIndexLevel <= sceneConfig.levels.Count() - 1)
        {
            PreviousLevelScoreButton.enabled = false;
        }
        NextLevelScoreButton.enabled = true;
    }

    void WriteTopScores()
    {
        levelName.text = "Level_" + (currentIndexLevel + 1); // on démarre de zéro & 0 -> 1
        List<int> scores = GameManager2.GetScoreForLevel(currentIndexLevel);
        string text = GetTextValueForTopScoreLevel(scores);
        topScoresTMPro.text = text;
    }

    string GetTextValueForTopScoreLevel(List<int> scores)
    {
        int count = Mathf.Min(3, scores.Count);
        string text;
        if (count == 0)
        {
            text = "Aucun score enregistré pour ce niveau.";
            return text;
        }

        text = "Top 3 :\n";

        for (int i = 0; i < count; i++)
        {
            text += $"{i + 1}. {scores[i]} pts\n";
        }

        return text;
    }

    void BackLoadGameButton()
    {
        // createGamePanel.SetActive(false);
        // loadGamePanel.SetActive(true);
        ChangeCurrentPanel(Panel.LoadGame);
    }

    void GoCreateGame()
    {
        int occupied = 0;
        for (int i = 0; i < 3; i++)
        {
            if (PlayerPrefs.HasKey("Slot" + i + "_PlayerName"))
                occupied++;
        }

        if (occupied >= 3)
        {
            // Afficher un message pour supprimer une game avant d'en créer une nouvelle
            Debug.Log(
                "Les 3 slots sont déjà occupés ! Supprimez une partie pour créer une nouvelle."
            );
            ever3GamesCreatedPanel.SetActive(true);
            return;
        }
        ChangeCurrentPanel(Panel.CreateGame);
    }

    void CreateGame()
    {
        if (string.IsNullOrEmpty(GameManager2.instance.playerName))
        {
            Debug.LogError("PlayerName doit être défini dans le Game Manager");
            return;
        }
        // Trouver le premier slot libre
        int slotIndex = -1;
        for (int i = 0; i < 3; i++)
        {
            if (!PlayerPrefs.HasKey("Slot" + i + "_PlayerName"))
            {
                slotIndex = i;
                break;
            }
        }

        if (slotIndex == -1)
        {
            Debug.LogError("Aucun slot libre trouvé !");
            return;
        }

        // Créer la nouvelle game
        Game newGame = new Game();
        newGame.playerName = GameManager2.instance.playerName;
        newGame.levelReached = 0;
        newGame.totalScore = 0;

        // Sauvegarder la game dans PlayerPrefs
        PlayerPrefs.SetString("Slot" + slotIndex + "_PlayerName", newGame.playerName);
        PlayerPrefs.SetInt("Slot" + slotIndex + "_Level", newGame.levelReached);
        PlayerPrefs.SetInt("Slot" + slotIndex + "_Score", newGame.totalScore);
        PlayerPrefs.Save();

        // Charger la première scène
        GameManager2.instance.GoToFirstScene();
    }

    void AddNameToList(string name)
    {
        string list = PlayerPrefs.GetString("AllPlayerNames", "");
        list += name + ";";
        PlayerPrefs.SetString("AllPlayerNames", list);
    }

    void ResetScore()
    {
        for (int i = 0; i < sceneConfig.levels.Count(); i++)
        {
            PlayerPrefs.DeleteKey(sceneConfig.keyTopScoreLevels + i);
        }
        PlayerPrefs.Save();
        confirmResetScorePanel.SetActive(false);
    }

    public void OnCreditsClick()
    {
        ChangeCurrentPanel(Panel.Credits);
    }

    // Functions for manage

    void ChangeCurrentPanel(Panel panel)
    {
        // hide current panel
        ShowPanel(currentPanel, false);
        // show and save new current panel
        ShowPanel(panel, true);
    }

    void ShowPanel(Panel panel, bool show)
    {
        switch (panel)
        {
            case Panel.Home:
                homePanel.SetActive(show);
                if (show)
                {
                    currentPanel = Panel.Home;
                }
                break;
            case Panel.Instructions:
                instructionsPanel.SetActive(show);
                if (show)
                {
                    currentPanel = Panel.Instructions;
                }
                break;
            case Panel.Scores:
                scoresPanel.SetActive(show);
                if (show)
                {
                    currentPanel = Panel.Scores;
                }
                break;
            case Panel.Options:
                optionsPanel.SetActive(show);
                if (show)
                {
                    currentPanel = Panel.Options;
                }
                break;
            case Panel.Credits:
                creditsPanel.SetActive(show);
                if (show)
                {
                    currentPanel = Panel.Credits;
                }
                break;
            case Panel.LoadGame:
                loadGamePanel.SetActive(show);
                if (show)
                {
                    currentPanel = Panel.LoadGame;
                }
                break;
            case Panel.CreateGame:
                createGamePanel.SetActive(show);
                if (show)
                {
                    currentPanel = Panel.CreateGame;
                }
                break;
        }
    }
}
