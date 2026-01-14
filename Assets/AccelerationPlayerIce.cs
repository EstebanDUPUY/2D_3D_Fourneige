using UnityEngine;

public class AccelerationPlayerIce : MonoBehaviour
{
    [SerializeField]
    private float bonusSpeed = 8f;

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();

            if (player.IsIceForm)
                player.bonusSlopeSpeed = bonusSpeed;
            else
            {
                PlayerDamageSystem playerDamage = player.GetComponent<PlayerDamageSystem>();
                if (null != playerDamage)
                {
                    playerDamage.DieNow();
                }
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
                PlayerDamageSystem playerDamage = player.GetComponent<PlayerDamageSystem>();
                if (null != playerDamage)
                {
                    playerDamage.DieNow();
                }
            }
        }
    }

    void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            other.gameObject.GetComponent<PlayerController>().bonusSlopeSpeed = 1f;
        }
    }
}
