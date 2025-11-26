using UnityEngine;

public class PlayerTest : MonoBehaviour
{
    PlayerIceSystem playerIceSystem;

    void Awake()
    {
        playerIceSystem = GetComponent<PlayerIceSystem>();
    }

    // GETTER

    public PlayerIceSystem GetIceSystem()
    {
        return playerIceSystem;
    }
}
