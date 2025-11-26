using UnityEngine;

public class IceSurface : MonoBehaviour
{
    public IceSurfaceData iceSurfaceData; // Le ScriptableObject qui définit la glace

    void OnCollisionEnter(Collision other)
    {
        PlayerTest player = other.gameObject.GetComponent<PlayerTest>();
        if (player != null)
            iceSurfaceData.TriggerEnter(this); // Déclenche l'événement pour ce player
    }

    void OnCollisionExit(Collision other)
    {
        PlayerTest player = other.gameObject.GetComponent<PlayerTest>();
        if (player != null)
            iceSurfaceData.TriggerExit(this); // Déclenche l'événement pour ce player
    }
}
