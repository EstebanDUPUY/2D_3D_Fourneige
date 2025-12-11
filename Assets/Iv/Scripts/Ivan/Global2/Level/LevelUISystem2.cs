using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelUISystem2 : MonoBehaviour
{
    [SerializeField]
    LevelManager2 levelManager;

    [Header("Action")]
    [SerializeField]
    Button playButton;

    [SerializeField]
    Button quitButton;

    [Header("Panel Buttons")]
    [SerializeField]
    Button[] homeButtons;

    [SerializeField]
    Button[] backPauseButtons;

    [SerializeField]
    Button instructionsButton;

    [SerializeField]
    Button optionsButton;

    [SerializeField]
    Button yesHomeButton;

    [SerializeField]
    Button noHomeButton;

    [SerializeField]
    Button yesQuitButton;

    [SerializeField]
    Button noQuitButton;

    [Header("Panel")]
    [SerializeField]
    GameObject pausePanel;

    [SerializeField]
    GameObject instructionsPanel;

    [SerializeField]
    GameObject optionsPanel;

    [SerializeField]
    GameObject confirmHomePanel;

    [SerializeField]
    GameObject confirmQuitPanel;

    [SerializeField]
    Panel currentPanel = Panel.Pause;

    enum Panel
    {
        Pause,
        Instructions,
        Options,
    }

    public Action playClick;

    void Awake()
    {
        // To Do
        // Delete for prod below
        // For test the scene independently we authorize the creation GameMAnager.instance there
        // Not use that in prod
        if (null == GameManager2.instance)
        {
            Debug.LogWarning(
                "GameManager2.instance is NULL at start. "
                    + "The LevelManager2 create it for Dev Mode with GameManager2.InstantiateIfNeededInDevMode()"
            );
            GameManager2.InstantiateIfNeededInDevMode();
        }
        // Delete for prod above

        // Action
        playButton.onClick.AddListener(() => OnPlayClick());
        quitButton.onClick.AddListener(() => confirmQuitPanel.SetActive(true));

        // Panel

        foreach (Button homeButton in homeButtons)
        {
            homeButton.onClick.AddListener(() => confirmHomePanel.SetActive(true));
        }
        foreach (Button backPauseButton in backPauseButtons)
        {
            backPauseButton.onClick.AddListener(() => ChangeCurrentPanel(Panel.Pause));
        }

        optionsButton.onClick.AddListener(() => ChangeCurrentPanel(Panel.Options));
        instructionsButton.onClick.AddListener(() => ChangeCurrentPanel(Panel.Instructions));

        yesHomeButton.onClick.AddListener(() => GameManager2.instance.ComeToHomeScene());
        noHomeButton.onClick.AddListener(() => confirmHomePanel.SetActive(false));

        yesQuitButton.onClick.AddListener(() => QuitApplication());
        noQuitButton.onClick.AddListener(() => confirmQuitPanel.SetActive(false));
    }

    void OnEnable()
    {
        LevelManager2.OnPlayPauseTime += TooglePlay;
    }

    void OnDisable()
    {
        LevelManager2.OnPlayPauseTime -= TooglePlay;
    }

    public void TooglePlay(bool onPause)
    {
        pausePanel.SetActive(onPause);
    }

    // Evenement On Click
    // Actions

    public void OnPlayClick()
    {
        // pausePanel.SetActive(false);
        levelManager.PlayPauseTime();
        // playClick?.Invoke();
    }

    public void QuitApplication()
    {
        Debug.Log("Quitter Application"); // TO DO : Delete for Build
        Application.Quit();
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
            case Panel.Pause:
                pausePanel.SetActive(show);
                if (show)
                {
                    currentPanel = Panel.Pause;
                }
                break;
            case Panel.Instructions:
                instructionsPanel.SetActive(show);
                if (show)
                {
                    currentPanel = Panel.Instructions;
                }
                break;
            case Panel.Options:
                optionsPanel.SetActive(show);
                if (show)
                {
                    currentPanel = Panel.Options;
                }
                break;
        }
    }
}
