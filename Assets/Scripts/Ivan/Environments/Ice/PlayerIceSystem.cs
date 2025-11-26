using Unity.VisualScripting;
using UnityEngine;

public class PlayerIceSystem : MonoBehaviour
{
    public IceSurfaceData iceSurfaceData;
    public PlayerTest player;

    private void OnEnable()
    {
        iceSurfaceData.OnPlayerEnter += PlayerOnIceSurface;
        iceSurfaceData.OnPlayerExit += PlayerLeaveIceSurface;
    }

    private void OnDisable()
    {
        iceSurfaceData.OnPlayerEnter -= PlayerOnIceSurface;
        iceSurfaceData.OnPlayerEnter -= PlayerLeaveIceSurface;
    }

    private void PlayerOnIceSurface(IceSurface iceSurface)
    {
        if (player.GetPlayerMode() == PlayerTest.PlayerMode.Ice)
        {
            Debug.Log("Player glisse sur la glace !");
            // Si il faut ici mettre application sur rigidbody player pour mouvement.
            // comme lineardamping
        }
        else
        {
            player.Die();

            // TO DO
            // Level Manager Call CheckPoint
        }
    }

    private void PlayerLeaveIceSurface(IceSurface iceSurface)
    {
        Debug.Log("Player ne glisse plus sur la glace !");
    }
}
