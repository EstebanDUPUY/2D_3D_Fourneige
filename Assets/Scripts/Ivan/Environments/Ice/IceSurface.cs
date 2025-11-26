using UnityEngine;

public class IceSurface : MonoBehaviour
{
    public IceSurfaceData iceSurfaceData; // Le ScriptableObject qui définit la glace

    void OnCollisionEnter(Collision other)
    {
        PlayerTest player = other.gameObject.GetComponent<PlayerTest>();
        if (player != null)
            iceSurfaceData.PlayerEnter();
    }
}
