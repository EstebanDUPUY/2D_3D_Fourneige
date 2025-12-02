using UnityEngine;

public class IceVFXManager : MonoBehaviour
{
    [Header("Sulfur Cave FX")]
    public ParticleSystem sulfurExplosionFX;

    public void PlayFX(ParticleSystem fx, Vector3 position)
    {
        if (fx == null)
            return;
        ParticleSystem explosion = Instantiate(fx, position, Quaternion.identity);
        Destroy(explosion.gameObject, 1f);
    }

    public void PlaySulfurExplosion(Vector3 position)
    {
        PlayFX(sulfurExplosionFX, position);
    }
}
