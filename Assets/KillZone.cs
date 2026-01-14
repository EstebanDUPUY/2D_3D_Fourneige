using UnityEngine;

public class KillZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerDamageSystem playerDamage = other
                .GetComponent<PlayerController>()
                .GetComponent<PlayerDamageSystem>();
            if (null != playerDamage)
            {
                playerDamage.DieNow();
            }
        }
    }
}
