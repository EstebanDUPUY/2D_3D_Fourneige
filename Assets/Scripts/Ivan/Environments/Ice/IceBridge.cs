using UnityEngine;

public class IceBridge : EnvironmentGame
{
    public IceBridgeData iceBridgeData;

    public float dragOnIce = 0.2f;
    public float speedMultiplier = 2f;
    public bool killFireForm = true;

    void OnCollisionEnter(Collision other)
    {
        PlayerIceSystem player = other.gameObject.GetComponent<PlayerIceSystem>();
        if (player != null)
            iceBridgeData.PlayerEnter(this);
    }
}
