using UnityEngine;

public class IceManager : MonoBehaviour
{
    public IceSurfaceData iceSurfaceData;
    public PlayerTest player;

    // Manage Event

    private void OnEnable()
    {
        iceSurfaceData.OnPlayerEnter += PlayerOnIceSurface;

        player.PlayerModeChange += PlayerModeCompareToEnvironment;
    }

    private void OnDisable()
    {
        iceSurfaceData.OnPlayerEnter -= PlayerOnIceSurface;
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
                // case PlayerTest.PlayerOnEnvironment.IceBridge:
                //     Debug.Log("Die -> Fire on Ice Bridge");
                //     break;
                // case PlayerTest.PlayerOnEnvironment.SulfurCave:
                //     Debug.Log("Die -> Fire on Sulfer Cave");
                //     break;
            }
        }
    }

    private void PlayerOnIceSurface()
    {
        player.SetPlayerOnEvironment(PlayerTest.PlayerOnEnvironment.IceSurface);
        PlayerModeCompareToEnvironment(player);
    }

    private void PlayerOnIceBridge()
    {
        player.SetPlayerOnEvironment(PlayerTest.PlayerOnEnvironment.IceBridge);
        PlayerModeCompareToEnvironment(player);
    }

    private void PlayerOnIceMovingWall()
    {
        player.SetPlayerOnEvironment(PlayerTest.PlayerOnEnvironment.IceMovingWall);
        PlayerModeCompareToEnvironment(player);
    }

    private void PlayerOnSulfurCave()
    {
        player.SetPlayerOnEvironment(PlayerTest.PlayerOnEnvironment.SulfurCave);
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
