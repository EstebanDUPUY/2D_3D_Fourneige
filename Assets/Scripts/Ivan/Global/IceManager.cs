using System;
using UnityEngine;

public class IceManager : MonoBehaviour
{
    public IceSurfaceData iceSurfaceData;
    public SulfurCaveData sulfurCaveData;
    public IceBridgeData iceBridgeData;
    public IceWallData iceWallData;

    public PlayerController player;
    PlayerIceSystem playerIceSystem;
    PlayerDamageSystem playerDamageSystem;

    Rigidbody rb;

    EnvironmentGame environmentGame;

    public Action PlayerDie;

    void Awake()
    {
        rb = player.GetComponent<Rigidbody>();
        playerIceSystem = player.GetComponent<PlayerIceSystem>();
        playerDamageSystem = player.GetComponent<PlayerDamageSystem>();
    }

    // Manage Event

    private void OnEnable()
    {
        iceSurfaceData.OnPlayerEnter += PlayerOnIceSurface;
        sulfurCaveData.OnPlayerEnter += PlayerOnSulfurCave;
        iceBridgeData.OnPlayerEnter += PlayerOnIceBridge;
        iceWallData.OnPlayerEnter += PlayerOnIceWall;

        iceWallData.OnPlayerExit += PlayerOutEnvironment;
        iceBridgeData.OnPlayerExit += PlayerOutEnvironment;
        sulfurCaveData.OnPlayerExit += PlayerOutEnvironment;
        iceSurfaceData.OnPlayerExit += PlayerOutEnvironment;

        // playerIceSystem.PlayerModeChange += PlayerModeCompareToEnvironment;

        player.SwitchMode += PlayerModeCompareToEnvironment;
    }

    private void OnDisable()
    {
        iceSurfaceData.OnPlayerEnter -= PlayerOnIceSurface;
        sulfurCaveData.OnPlayerEnter -= PlayerOnSulfurCave;
        iceBridgeData.OnPlayerEnter -= PlayerOnIceBridge;
        iceWallData.OnPlayerEnter -= PlayerOnIceWall;

        iceWallData.OnPlayerExit -= PlayerOutEnvironment;
        iceBridgeData.OnPlayerExit -= PlayerOutEnvironment;
        sulfurCaveData.OnPlayerExit -= PlayerOutEnvironment;
        iceSurfaceData.OnPlayerExit -= PlayerOutEnvironment;

        // playerIceSystem.PlayerModeChange -= PlayerModeCompareToEnvironment;
        player.SwitchMode -= PlayerModeCompareToEnvironment;
    }

    void PlayerModeCompareToEnvironment()
    {
        if (player.currentState == PlayerController.States.Fire)
        {
            switch (playerIceSystem.GetPlayerOnEnvironment())
            {
                // case PlayerIceSystem.PlayerOnEnvironment.IceSurface:
                //     Debug.Log("Die -> Fire on Ice Surface");
                //     break;
                case PlayerIceSystem.PlayerOnEnvironment.IceWall:
                    rb.linearDamping = playerIceSystem.linearDampingNormal;
                    Debug.Log("Linear Damping Normal -> Fire on Ice Moving Wall");
                    break;
                // case PlayerIceSystem.PlayerOnEnvironment.IceBridge:
                //     Debug.Log("Die -> Fire on Ice Bridge");
                //     break;
                case PlayerIceSystem.PlayerOnEnvironment.SulfurCave:
                    playerDamageSystem.Explode();
                    Debug.Log("THE CALL");
                    // PlayerDie?.Invoke();
                    Debug.Log("Die -> Fire on Sulfer Cave");
                    break;
            }
        }
        else
        {
            if (null == environmentGame)
            {
                rb.linearDamping = playerIceSystem.linearDampingNormal;
            }
            else
            {
                switch (playerIceSystem.GetPlayerOnEnvironment())
                {
                    case PlayerIceSystem.PlayerOnEnvironment.IceSurface:
                        Debug.Log("Ice on Ice Surface");
                        rb.linearDamping = playerIceSystem.linearDampingNormal;
                        break;
                    case PlayerIceSystem.PlayerOnEnvironment.IceWall:
                        IceWall iceWall = environmentGame.GetComponent<IceWall>();
                        rb.linearDamping = iceWall.dragOnIce;
                        Debug.Log("Ice on Ice Moving Wall");
                        break;
                    case PlayerIceSystem.PlayerOnEnvironment.IceBridge:
                        rb.linearDamping = playerIceSystem.linearDampingNormal;
                        Debug.Log("Ice Fire on Ice Bridge");
                        break;
                    case PlayerIceSystem.PlayerOnEnvironment.SulfurCave:
                        rb.linearDamping = playerIceSystem.linearDampingNormal;
                        Debug.Log("Ice Fire on Sulfer Cave");
                        break;
                }
            }
        }
    }

    private void PlayerOnIceSurface(IceSurface iceSurface)
    {
        playerIceSystem.SetPlayerOnEnvironment(PlayerIceSystem.PlayerOnEnvironment.IceSurface);
        environmentGame = iceSurface;
        PlayerModeCompareToEnvironment();
    }

    private void PlayerOnIceBridge(IceBridge iceBridge)
    {
        playerIceSystem.SetPlayerOnEnvironment(PlayerIceSystem.PlayerOnEnvironment.IceBridge);
        environmentGame = iceBridge;
        PlayerModeCompareToEnvironment();
    }

    private void PlayerOnIceWall(IceWall iceWall)
    {
        playerIceSystem.SetPlayerOnEnvironment(PlayerIceSystem.PlayerOnEnvironment.IceWall);
        environmentGame = iceWall;
        PlayerModeCompareToEnvironment();
    }

    private void PlayerOnSulfurCave(SulfurCave sulfurCave)
    {
        playerIceSystem.SetPlayerOnEnvironment(PlayerIceSystem.PlayerOnEnvironment.SulfurCave);
        environmentGame = sulfurCave;
        PlayerModeCompareToEnvironment();
    }

    private void PlayerOutEnvironment()
    {
        playerIceSystem.SetPlayerOnEnvironment(PlayerIceSystem.PlayerOnEnvironment.None);
        environmentGame = null;
        PlayerModeCompareToEnvironment();
    }

    // Other Functions

    // void HandleDie()
    // {
    //     playerIceSystem.Die();

    //     // TO DO
    //     // Level Manager Call CheckPoint
    // }
}
