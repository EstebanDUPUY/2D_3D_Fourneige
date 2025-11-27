using UnityEngine;

public class IceWall : EnvironmentGame
{
    public IceWallData iceWallData;

    public float dragOnIce = 45f;
    public float speedMultiplier = 2f;
    public bool killFireForm = true;

    void OnCollisionEnter(Collision other)
    {
        PlayerIceSystem player = other.gameObject.GetComponent<PlayerIceSystem>();
        if (player != null)
            iceWallData.PlayerEnter(this);
    }

    void OnCollisionExit(Collision other)
    {
        PlayerIceSystem player = other.gameObject.GetComponent<PlayerIceSystem>();
        if (player != null)
            iceWallData.PlayerExit();
    }
}
