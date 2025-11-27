using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class LevelManager : MonoBehaviour
{
    PlayerInput playerInput;

    InputAction togglePlayAction;

    public IceManager iceManager;

    // Action
    public static Action<bool> OnPlayPauseTime;

    bool onPause;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        togglePlayAction = playerInput.actions["TogglePlay"];
    }

    void OnEnable()
    {
        togglePlayAction.performed += OnTogglePlay;
        iceManager.PlayerDie += GameOver;
    }

    void OnDisable()
    {
        togglePlayAction.performed -= OnTogglePlay;
        iceManager.PlayerDie -= GameOver;
    }

    // Actions

    void OnTogglePlay(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        PlayPauseTime();
    }

    public void GameOver()
    {
        // TO DO :  do a true GameOver
        PlayPauseTime();
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
