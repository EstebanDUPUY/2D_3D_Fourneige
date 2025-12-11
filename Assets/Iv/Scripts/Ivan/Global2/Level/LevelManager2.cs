using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class LevelManager2 : MonoBehaviour
{
    // public string nextNameScene;

    public PlayerController player;

    PlayerInput playerInput;

    InputAction togglePlayAction;
    InputAction simuleLevelComplete;

    // Action
    public static Action<bool> OnPlayPauseTime;
    public static Action OnLevelReset;
    public static Action OnLevelComplete;
    public static LevelManager2 Instance;

    public TriggerCompleteLevel2 triggerCompleteLevel;

    bool onPause;

    void Awake()
    {
        // Utile si on est partie sur le home et revenu
        onPause = false;
        Time.timeScale = 1f;

        Instance = this;
        playerInput = GetComponent<PlayerInput>();
        togglePlayAction = playerInput.actions["TogglePlay"];
        // simuleLevelComplete = playerInput.actions["SimuleLevelComplete"];
    }

    void OnEnable()
    {
        togglePlayAction.performed += OnTogglePlay;
        // simuleLevelComplete.performed += SimuleCompleteLevel;
        PlayerDamageSystem.Die += GameOver;
        triggerCompleteLevel.OnLevelCompletedTrigger += CompleteLevel;
    }

    void OnDisable()
    {
        togglePlayAction.performed -= OnTogglePlay;
        // simuleLevelComplete.performed -= SimuleCompleteLevel;
        PlayerDamageSystem.Die -= GameOver;
        triggerCompleteLevel.OnLevelCompletedTrigger -= CompleteLevel;
    }

    // Actions

    void OnTogglePlay(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        PlayPauseTime();
    }

    public static void GameOver()
    {
        Debug.Log("LevelManager2 -> GameOver() call");
        Instance.StartCoroutine(Instance.GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        OnLevelReset?.Invoke();
        Debug.Log("LevelManager2 -> GameOverRoutine with reset level call");
    }

    public void PlayPauseTime()
    {
        Debug.Log("Toggle Play");
        onPause = !onPause;
        if (onPause)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
        OnPlayPauseTime?.Invoke(onPause);
    }

    void SimuleCompleteLevel(InputAction.CallbackContext context)
    {
        // TO DO -> Remplacer par gestion trigger
        // Pour le moment on simule avec un bouton
        // plus tard ca sera un trigger dans le niveau
        CompleteLevel();
    }

    public void CompleteLevel()
    {
        OnLevelComplete?.Invoke();
        // player.SetCanMove(false);
    }
}
