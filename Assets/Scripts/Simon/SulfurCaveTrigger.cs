using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class SulfurCaveTrigger : MonoBehaviour
{
    private string particleResourcePath = "Particles/SulfurExplosion";
    private float time = 3f;
    bool everExplode;

    private void OnTriggerEnter(Collider other)
    {
        HandleExplode(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!everExplode)
        {
            HandleExplode(other);
        }
    }

    void HandleExplode(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player.IsIceForm)
            return;

        // Charger le prefab depuis Resources
        GameObject explosionPrefab = Resources.Load<GameObject>(particleResourcePath);
        if (explosionPrefab == null)
        {
            Debug.LogWarning(
                "Prefab de particules introuvable dans Resources/" + particleResourcePath
            );
            return;
        }

        // Instancier le particle system
        GameObject psObj = Instantiate(
            explosionPrefab,
            other.transform.position + Vector3.up * 1f,
            Quaternion.identity
        );
        ParticleSystem ps = psObj.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            // Burst de 100 particules instantané
            // var emission = ps.emission;
            // emission.rateOverTime = 0;
            // emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 100) });

            ps.Play();
            StartCoroutine(StopExplode(time, ps));
        }

        player.GetComponent<PlayerDamageSystem>().DieNow(time);
        player.StopMoving = true;

        everExplode = true;
    }

    IEnumerator StopExplode(float time, ParticleSystem ps)
    {
        yield return new WaitForSeconds(time);
        ps.Stop();
        Destroy(ps.gameObject);
        everExplode = false;
    }
}
