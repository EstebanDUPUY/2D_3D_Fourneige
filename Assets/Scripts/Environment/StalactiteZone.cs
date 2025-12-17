using UnityEngine;

public class StalactiteZone : MonoBehaviour
{
    public Stalactite stalactite;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (stalactite.isUp)
            {
                Debug.Log("is Up");
                stalactite.Fall();
            }
        }
    }
}
