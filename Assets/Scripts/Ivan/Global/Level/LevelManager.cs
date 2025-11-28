using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class LevelManager : MonoBehaviour
{
    PlayerInput playerInput;

    InputAction togglePlayAction;

    public IceManager iceManager;

    // Action
    public static Action<bool> OnPlayPauseTime;
    public static Action OnLevelReset;

    public static LevelManager Instance;

    bool onPause;

    void Awake()
    {
        Instance = this;
        playerInput = GetComponent<PlayerInput>();
        togglePlayAction = playerInput.actions["TogglePlay"];
    }

    void OnEnable()
    {
        togglePlayAction.performed += OnTogglePlay;
        PlayerDamageSystem.Die += GameOver;
    }

    void OnDisable()
    {
        togglePlayAction.performed -= OnTogglePlay;
        PlayerDamageSystem.Die -= GameOver;
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
        Instance.StartCoroutine(Instance.GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(2f);
        OnLevelReset?.Invoke();
    }

    public void PlayPauseTime()
    {
        Debug.Log("Toogle Play");
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
}
