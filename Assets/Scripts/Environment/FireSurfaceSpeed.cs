using UnityEngine;

public class FireSurfaceSpeed : MonoBehaviour
{
    private float bonus = 3.0f;
    private float malus = 0.75f;

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();

            if (!player.IsIceForm)
            {
                player.SetSpeedMultiplier(bonus);
            }
            else
            {
                player.SetSpeedMultiplier(malus);
            }
        }
    }

    void OnCollisionStay(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();

            if (!player.IsIceForm)
            {
                player.SetSpeedMultiplier(bonus);
            }
            else
            {
                player.SetSpeedMultiplier(malus);
            }
        }
    }

    void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            other.gameObject.GetComponent<PlayerController>().ResetSpeedMultiplier();
        }
    }
}
