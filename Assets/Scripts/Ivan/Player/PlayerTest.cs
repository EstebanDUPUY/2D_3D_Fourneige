using System;
using UnityEngine;

public class PlayerTest : MonoBehaviour
{
    public Action<PlayerTest> PlayerModeChange;
    Rigidbody rb;

    public enum PlayerMode
    {
        Fire,
        Ice,
    }

    public enum PlayerOnEnvironment
    {
        IceSurface,
        IceBridge,
        IceMovingWall,
        SulfurCave,
    }

    PlayerMode playerMode;
    PlayerOnEnvironment playerOnEnvironment;

    void Awake()
    {
        playerMode = PlayerMode.Fire;
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

    public void SetPlayerOnEvironment(PlayerOnEnvironment playerOnEnvironment)
    {
        this.playerOnEnvironment = playerOnEnvironment;
    }
}
