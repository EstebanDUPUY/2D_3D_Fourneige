using UnityEngine;

public class IceWall : EnvironmentGame
{
    public IceWallData iceWallData;

    public float dragOnIce = 45f;
    public float speedMultiplier = 2f;
    public bool killFireForm = true;

    void OnCollisionEnter(Collision other)
    {
        PlayerTest player = other.gameObject.GetComponent<PlayerTest>();
        if (player != null)
            iceWallData.PlayerEnter(this);
    }

    void OnCollisionExit(Collision other)
    {
        PlayerTest player = other.gameObject.GetComponent<PlayerTest>();
        if (player != null)
            iceWallData.PlayerExit();
    }
}
