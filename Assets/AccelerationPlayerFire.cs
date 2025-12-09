using UnityEngine;

public class AccelerationPlayerFire : MonoBehaviour
{
    [SerializeField] private float bonusSpeed = 8f;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (!player.IsIceForm)
                player.bonusSlopeSpeed = bonusSpeed;
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
