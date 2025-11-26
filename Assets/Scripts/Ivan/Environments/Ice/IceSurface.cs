using UnityEngine;

public class IceSurface : MonoBehaviour
{
    public IceSurfaceData iceSurfaceData; // Le ScriptableObject qui définit la glace

    void OnTriggerEnter(Collider other)
    {
        PlayerTest player = other.GetComponent<PlayerTest>();
        if (player != null)
            iceSurfaceData.TriggerEnter(this); // Déclenche l'événement pour ce player
    }

    void OnTriggerExit(Collider other)
    {
        PlayerTest player = other.GetComponent<PlayerTest>();
        if (player != null)
            iceSurfaceData.TriggerExit(this); // Déclenche l'événement pour ce player
    }
}
