using System;
using UnityEngine;

public class PlayerTest : MonoBehaviour
{
    public Action<PlayerTest> PlayerModeChange;
    Rigidbody rb;

    public float linearDampingNormal = 0f;
    public float speedMultiplierInit = 1f;
    public float speedMultiplier = 1f;

    public enum PlayerMode
    {
        Fire,
        Ice,
    }

    public enum PlayerOnEnvironment
    {
        IceSurface,
        IceBridge,
        IceWall,
        SulfurCave,
        None,
    }

    PlayerMode playerMode;
    PlayerOnEnvironment playerOnEnvironment;

    void Awake()
    {
        playerMode = PlayerMode.Ice;
        playerOnEnvironment = PlayerOnEnvironment.IceSurface;

        rb = GetComponent<Rigidbody>();
    }

    public void Die()
    {
        Debug.Log("** PLAYER TEST -> Die()");
    }

    void SwitchMode()
    {
        // Gestion Input Esteban

        if (playerMode == PlayerMode.Fire)
        {
            playerMode = PlayerMode.Ice;
        }
        else
        {
            playerMode = PlayerMode.Fire;
        }

        PlayerModeChange?.Invoke(this);
    }

    // GETTER

    public Rigidbody GetRigidbody()
    {
        return rb;
    }

    public PlayerMode GetPlayerMode()
    {
        return playerMode;
    }

    public PlayerOnEnvironment GetPlayerOnEnvironment()
    {
        return playerOnEnvironment;
    }

    // SETTER

    public void SetPlayerMode(PlayerMode playerMode)
    {
        this.playerMode = playerMode;
    }

    public void SetPlayerOnEnvironment(PlayerOnEnvironment playerOnEnvironment)
    {
        this.playerOnEnvironment = playerOnEnvironment;
    }
}
