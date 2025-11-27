using UnityEngine;

public class IceManager : MonoBehaviour
{
    public IceSurfaceData iceSurfaceData;
    public SulfurCaveData sulfurCaveData;
    public IceBridgeData iceBridgeData;
    public IceWallData iceWallData;
    public PlayerTest player;
    public Rigidbody rb;

    EnvironmentGame environmentGame;

    void Awake()
    {
        rb = player.GetComponent<Rigidbody>();
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

        player.PlayerModeChange += PlayerModeCompareToEnvironment;
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

        player.PlayerModeChange -= PlayerModeCompareToEnvironment;
    }

    void PlayerModeCompareToEnvironment(PlayerTest player)
    {
        if (player.GetPlayerMode() == PlayerTest.PlayerMode.Fire)
        {
            switch (player.GetPlayerOnEnvironment())
            {
                case PlayerTest.PlayerOnEnvironment.IceSurface:
                    Debug.Log("Die -> Fire on Ice Surface");
                    break;
                case PlayerTest.PlayerOnEnvironment.IceWall:
                    IceWall iceWall = environmentGame.GetComponent<IceWall>();
                    Debug.Log("Die -> Fire on Ice Moving Wall");
                    break;
                case PlayerTest.PlayerOnEnvironment.IceBridge:
                    Debug.Log("Die -> Fire on Ice Bridge");
                    break;
                case PlayerTest.PlayerOnEnvironment.SulfurCave:
                    Debug.Log("Die -> Fire on Sulfer Cave");
                    break;
            }
        }
        else
        {
            if (null == environmentGame)
            {
                rb.linearDamping = player.linearDampingNormal;
            }
            else
            {
                switch (player.GetPlayerOnEnvironment())
                {
                    case PlayerTest.PlayerOnEnvironment.IceSurface:
                        Debug.Log("Ice on Ice Surface");
                        rb.linearDamping = player.linearDampingNormal;
                        break;
                    case PlayerTest.PlayerOnEnvironment.IceWall:
                        IceWall iceWall = environmentGame.GetComponent<IceWall>();
                        rb.linearDamping = iceWall.dragOnIce;
                        Debug.Log("Ice on Ice Moving Wall");
                        break;
                    case PlayerTest.PlayerOnEnvironment.IceBridge:
                        rb.linearDamping = player.linearDampingNormal;
                        Debug.Log("Ice Fire on Ice Bridge");
                        break;
                    case PlayerTest.PlayerOnEnvironment.SulfurCave:
                        rb.linearDamping = player.linearDampingNormal;
                        Debug.Log("Ice Fire on Sulfer Cave");
                        break;
                }
            }
        }
    }

    private void PlayerOnIceSurface(IceSurface iceSurface)
    {
        player.SetPlayerOnEnvironment(PlayerTest.PlayerOnEnvironment.IceSurface);
        environmentGame = iceSurface;
        PlayerModeCompareToEnvironment(player);
    }

    private void PlayerOnIceBridge(IceBridge iceBridge)
    {
        player.SetPlayerOnEnvironment(PlayerTest.PlayerOnEnvironment.IceBridge);
        environmentGame = iceBridge;
        PlayerModeCompareToEnvironment(player);
    }

    private void PlayerOnIceWall(IceWall iceWall)
    {
        player.SetPlayerOnEnvironment(PlayerTest.PlayerOnEnvironment.IceWall);
        environmentGame = iceWall;
        PlayerModeCompareToEnvironment(player);
    }

    private void PlayerOnSulfurCave(SulfurCave sulfurCave)
    {
        player.SetPlayerOnEnvironment(PlayerTest.PlayerOnEnvironment.SulfurCave);
        environmentGame = sulfurCave;
        PlayerModeCompareToEnvironment(player);
    }

    private void PlayerOutEnvironment()
    {
        player.SetPlayerOnEnvironment(PlayerTest.PlayerOnEnvironment.None);
        environmentGame = null;
        PlayerModeCompareToEnvironment(player);
    }

    // Other Functions

    // void HandleDie()
    // {
    //     player.Die();

    //     // TO DO
    //     // Level Manager Call CheckPoint
    // }
}
