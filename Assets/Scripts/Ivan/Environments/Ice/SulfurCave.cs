using UnityEngine;

public class SulfurCave : EnvironmentGame
{
    public SulfurCaveData sulfurCaveData;

    void OnTriggerEnter(Collider other)
    {
        PlayerIceSystem player = other.GetComponent<PlayerIceSystem>();
        if (player != null)
            sulfurCaveData.PlayerEnter(this);
    }
}
