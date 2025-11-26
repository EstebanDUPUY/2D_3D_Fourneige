using UnityEngine;

public class PlayerIceSystem : MonoBehaviour
{
    public IceSurfaceData iceSurfaceData;
    public PlayerTest player;

    private void OnEnable()
    {
        iceSurfaceData.OnPlayerEnter += HandlePlayerOnIce;
        iceSurfaceData.OnPlayerExit += HandlePlayerOutOfIce;
    }

    private void OnDisable()
    {
        iceSurfaceData.OnPlayerEnter -= HandlePlayerOnIce;
        iceSurfaceData.OnPlayerEnter -= HandlePlayerOutOfIce;
    }

    private void HandlePlayerOutOfIce(IceSurface iceSurface)
    {
        Debug.Log("Player ne glisse plus sur la glace !");
    }

    private void HandlePlayerOnIce(IceSurface iceSurface)
    {
        // TO DO : Appliquer la glisse, modifier Rigidbody etc.
        // player.rb.drag = iceSurfaceData.dragOnIce;
        // player.moveSpeed *= iceSurfaceData.speedMultiplier;

        Debug.Log("Player glisse sur la glace !");
    }
}
