using UnityEngine;

public class IceManager : MonoBehaviour
{
    public IceSurfaceData iceSurfaceData;
    public SulfurCaveData sulfurCaveData;
    public IceBridgeData iceBridgeData;
    public PlayerTest player;

    EnvironmentGame environmentGame;

    // Manage Event

    private void OnEnable()
    {
        iceSurfaceData.OnPlayerEnter += PlayerOnIceSurface;
        sulfurCaveData.OnPlayerEnter += PlayerOnSulfurCave;
        iceBridgeData.OnPlayerEnter += PlayerOnIceBridge;

        player.PlayerModeChange += PlayerModeCompareToEnvironment;
    }

    private void OnDisable()
    {
        iceSurfaceData.OnPlayerEnter -= PlayerOnIceSurface;
        sulfurCaveData.OnPlayerEnter -= PlayerOnSulfurCave;
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
                // case PlayerTest.PlayerOnEnvironment.IceMovingWall:
                //     Debug.Log("Die -> Fire on Ice Moving Wall");
                //     break;
                case PlayerTest.PlayerOnEnvironment.IceBridge:
                    Debug.Log("Die -> Fire on Ice Bridge");
                    break;
                case PlayerTest.PlayerOnEnvironment.SulfurCave:
                    Debug.Log("Die -> Fire on Sulfer Cave");
                    break;
            }
        }
    }

    private void PlayerOnIceSurface(IceSurface iceSurface)
    {
        player.SetPlayerOnEvironment(PlayerTest.PlayerOnEnvironment.IceSurface);
        environmentGame = iceSurface;
        PlayerModeCompareToEnvironment(player);
    }

    private void PlayerOnIceBridge(IceBridge iceBridge)
    {
        player.SetPlayerOnEvironment(PlayerTest.PlayerOnEnvironment.IceBridge);
        environmentGame = iceBridge;
        PlayerModeCompareToEnvironment(player);
    }

    // private void PlayerOnIceMovingWall()
    // {
    //     player.SetPlayerOnEvironment(PlayerTest.PlayerOnEnvironment.IceMovingWall);
    //     PlayerModeCompareToEnvironment(player);
    // }

    private void PlayerOnSulfurCave(SulfurCave sulfurCave)
    {
        player.SetPlayerOnEvironment(PlayerTest.PlayerOnEnvironment.SulfurCave);
        environmentGame = sulfurCave;
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
