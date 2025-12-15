using UnityEngine;

public class SulfurCaveTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerDamageSystem>().Explode();
            //PlayerDamageSystem.Die?.Invoke();
            Debug.Log("SulfurCaveTrigger -> Player entered sulfur zone, Die invoked");
        }
    }
}