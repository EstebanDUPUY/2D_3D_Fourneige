using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerReset playerReset = other.GetComponent<PlayerReset>();
            playerReset.SaveCheckpoint(this);
        }
    }
}
