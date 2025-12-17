using UnityEngine;

public class IceSurfaceSpeed : MonoBehaviour
{
    private float bonus = 3.0f;
    private float malus = 0.25f;

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();

            if (player.IsIceForm)
            {
                player.SetSpeedMultiplier(bonus);
                Debug.Log("bonusIce : " + bonus);
            }
            else
            {
                player.SetSpeedMultiplier(malus);
                Debug.Log("malusFire : " + malus);
            }
        }
    }

    void OnCollisionStay(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();

            if (player.IsIceForm)
            {
                player.SetSpeedMultiplier(bonus);
                Debug.Log("bonusIce : " + bonus);
            }
            else
            {
                player.SetSpeedMultiplier(malus);
                Debug.Log("malusFire : " + malus);
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
