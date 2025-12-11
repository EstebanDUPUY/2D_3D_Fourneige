using System;
using UnityEngine;

public class PlayerIceSystem : MonoBehaviour
{
    public Action<PlayerIceSystem> PlayerModeChange;
    public Rigidbody rb;


    public float linearDampingNormal = 0f;
    public float speedMultiplierInit = 1f;
    public float speedMultiplier = 1f;

    // public enum PlayerMode
    // {
    //     Fire,
    //     Ice,
    // }

    public enum PlayerOnEnvironment
    {
        IceSurface,
        IceBridge,
        IceWall,
        SulfurCave,
        None,
    }

    // PlayerMode playerMode;
    PlayerOnEnvironment playerOnEnvironment;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // playerMode = PlayerMode.Fire;
        playerOnEnvironment = PlayerOnEnvironment.None;

        // rb = GetComponent<Rigidbody>();
    }

    // void SwitchMode()
    // {
    //     // Gestion Input Esteban

    //     if (playerMode == PlayerMode.Fire)
    //     {
    //         playerMode = PlayerMode.Ice;
    //     }
    //     else
    //     {
    //         playerMode = PlayerMode.Fire;
    //     }

    //     PlayerModeChange?.Invoke(this);
    // }

    // GETTER

    public Rigidbody GetRigidbody()
    {
        return rb;
    }

    // public PlayerMode GetPlayerMode()
    // {
    //     return playerMode;
    // }

    public PlayerOnEnvironment GetPlayerOnEnvironment()
    {
        return playerOnEnvironment;
    }

    // SETTER

    // public void SetPlayerMode(PlayerMode playerMode)
    // {
    //     this.playerMode = playerMode;
    // }

    public void SetPlayerOnEnvironment(PlayerOnEnvironment playerOnEnvironment)
    {
        this.playerOnEnvironment = playerOnEnvironment;
    }
}
