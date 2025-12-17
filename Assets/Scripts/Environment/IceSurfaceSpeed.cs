using UnityEngine;

public class IceSurfaceSpeed : MonoBehaviour
{
    private float bonusIce = 2.0f;
    private float malusFire = 1.0f;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player.IsIceForm)
                player.bonusSlopeSpeed = bonusIce;
            // player.SetBonus(bonusFire);
            else
                player.bonusSlopeSpeed = malusFire;
            // player.SetBonus(malusIce);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player.IsIceForm)
                player.bonusSlopeSpeed = bonusIce;
            // player.SetBonus(bonusFire);
            else
                player.bonusSlopeSpeed = malusFire;
            // player.SetBonus(malusIce);
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
