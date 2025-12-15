using UnityEngine;

public class SulfurCaveTrigger : MonoBehaviour
{
    // Chemin dans le dossier Resources (ex: Assets/Resources/Particles/ExplosionParticle.prefab)
    private string particleResourcePath = "Particles/SulfurExplosion";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (!player.IsIceForm)
            {
                // Charger le prefab depuis Resources
                GameObject explosionParticlesPrefab = Resources.Load<GameObject>(
                    particleResourcePath
                );

                if (explosionParticlesPrefab != null)
                {
                    // Instancier le particle system à la position du joueur
                    var ps = Instantiate(
                        explosionParticlesPrefab,
                        other.transform.position,
                        Quaternion.identity
                    );
                    // var particle = ps.GetComponent<ParticleSystem>();
                    // if (particle != null)
                    // {
                    //     particle.Play();
                    // }
                }
                else
                {
                    Debug.LogWarning(
                        "Prefab de particules introuvable dans Resources/" + particleResourcePath
                    );
                }

                // Déclencher l'explosion du joueur
                other.GetComponent<PlayerDamageSystem>().Explode();
            }
        }
    }
}
