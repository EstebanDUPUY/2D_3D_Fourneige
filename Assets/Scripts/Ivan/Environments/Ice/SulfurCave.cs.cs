using UnityEngine;

public class SulfurCave : EnvironmentGame
{
    public SulfurCaveData sulfurCaveData;

    void OnCollisionEnter(Collision other)
    {
        PlayerTest player = other.gameObject.GetComponent<PlayerTest>();
        if (player != null)
            sulfurCaveData.PlayerEnter(this);
    }
}
