/*using UnityEngine;

public class GeyserCollider : MonoBehaviour
{
    [SerializeField] float force;
    FireObstacleController controller;
    [SerializeField] bool isBody = true;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SeekController(other);
            controller.GeyserAction(force, true, isBody);
        }
    }

    void OnTriggerExit(Collider other)
    {
         if (other.CompareTag("Player"))
        {
            SeekController(other);
            controller.GeyserAction(force, false, isBody);
        }
    }

    void SeekController(Collider other)
    {
        if (!controller)
            controller = other.GetComponent<FireObstacleController>();
    }
}*/
