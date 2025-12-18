using UnityEngine;

public class LavaFlow : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerDamageSystem playerDamage = other.GetComponent<PlayerDamageSystem>();
            playerDamage.DieNow();
        }
    }
}
