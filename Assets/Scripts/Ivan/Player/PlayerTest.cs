using System;
using UnityEngine;

public class PlayerTest : MonoBehaviour
{
    Action PlayerModeChange;
    PlayerIceSystem playerIceSystem;
    Rigidbody rb;

    public enum PlayerMode
    {
        Fire,
        Ice,
    }

    PlayerMode playerMode;

    void Awake()
    {
        playerMode = PlayerMode.Ice;
        playerIceSystem = GetComponent<PlayerIceSystem>();
        rb = GetComponent<Rigidbody>();
    }

    public void Die()
    {
        Debug.Log("** PLAYER TEST -> Die()");
    }

    // GETTER

    public PlayerIceSystem GetIceSystem()
    {
        return playerIceSystem;
    }

    public Rigidbody GetRigidbody()
    {
        return rb;
    }

    public PlayerMode GetPlayerMode()
    {
        return playerMode;
    }
}
