using System.Net.Security;
using UnityEngine;
using UnityEngine.UI;

public class GameUISystem : MonoBehaviour
{
    [Header("Action")]
    [SerializeField]
    Button playButton;

    [SerializeField]
    Button quitButton;

    [Header("Panel Buttons")]
    [SerializeField]
    Button[] homeButtons;

    [SerializeField]
    Button instructionsButton;

    [SerializeField]
    Button optionsButton;

    [SerializeField]
    Button creditsButton;

    [SerializeField]
    Button yesQuitButton;

    [SerializeField]
    Button noQuitButton;

    [Header("Panel")]
    [SerializeField]
    GameObject homePanel;

    [SerializeField]
    GameObject instructionsPanel;

    [SerializeField]
    GameObject optionsPanel;

    [SerializeField]
    GameObject creditsPanel;

    [SerializeField]
    GameObject confirmQuitPanel;

    [SerializeField]
    Panel currentPanel = Panel.Home;

    enum Panel
    {
        Home,
        Instructions,
        Options,
        Credits,
    }

    void Awake()
    {
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

        creditsButton.onClick.AddListener(() => OnCreditsClick());

        yesQuitButton.onClick.AddListener(() => QuitApplication());
        noQuitButton.onClick.AddListener(() => confirmQuitPanel.SetActive(false));
    }

    // Evenement On Click
    // Actions

    public void OnPlayClick()
    {
        // Debug.Log("Play First Scene"); // TO DO : Delete for Build
        GameManager.instance.GoToFirstScene();
    }

    public void QuitApplication()
    {
        Debug.Log("Quitter Application"); // TO DO : Delete for Build
        Application.Quit();
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
        }
    }
}
