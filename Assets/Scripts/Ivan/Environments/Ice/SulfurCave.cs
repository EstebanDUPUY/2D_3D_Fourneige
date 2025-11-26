using UnityEngine;

public class SulfurCave : EnvironmentGame
{
    public SulfurCaveData sulfurCaveData;

    void OnTriggerEnter(Collider other)
    {
        PlayerTest player = other.GetComponent<PlayerTest>();
        if (player != null)
            sulfurCaveData.PlayerEnter(this);
    }
}
