using UnityEngine;

public class FireSurfaceSpeed : MonoBehaviour
{
    private float bonusFire = 8.0f;
    private float malusIce = 0.25f;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (!player.IsIceForm)
                player.bonusSlopeSpeed = bonusFire;
            // player.SetBonus(bonusFire);
            else
                player.bonusSlopeSpeed = malusIce;
            // player.SetBonus(malusIce);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (!player.IsIceForm)
                player.bonusSlopeSpeed = bonusFire;
            else
                player.bonusSlopeSpeed = malusIce;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerController>().bonusSlopeSpeed = 1f;
            // other.GetComponent<PlayerController>().ResetBonus();
        }
    }
}
