using UnityEngine;

public class AccelerationPlayerFire : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (!player.IsIceForm)
                player.bonusSlopeSpeed = 8f;
            else
                Debug.Log("DEAD");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerController>().bonusSlopeSpeed = 1f;
        }
    }
}
