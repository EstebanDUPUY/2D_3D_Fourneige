using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class LevelManager : MonoBehaviour
{
    PlayerInput playerInput;

    InputAction togglePlayAction;

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
    }

    void OnDisable()
    {
        togglePlayAction.performed -= OnTogglePlay;
    }

    // Actions

    void OnTogglePlay(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
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
