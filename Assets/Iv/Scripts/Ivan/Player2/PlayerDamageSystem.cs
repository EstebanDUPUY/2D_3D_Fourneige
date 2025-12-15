using System;
using UnityEngine;

public class PlayerDamageSystem : MonoBehaviour
{
    public static Action<float> Die;

    // PlayerController player;
    Rigidbody rb;

    public float speedExplode;

    void Awake()
    {
        //player = GetComponent<PlayerController>();
        // rb = GetComponent<PlayerIceSystem>().GetRigidbody();
        rb = GetComponent<Rigidbody>();
    }

    public void Explode(float time)
    {
        Vector3 baseDir = (Vector3.right + Vector3.down * 0.6f).normalized;

        // Vérifie si le joueur se déplace vers la gauche ou la droite
        float directionSign = rb.linearVelocity.x < 0 ? -1f : 1f;

        // On inverse seulement la partie horizontale du vecteur
        Vector3 finalDir = new Vector3(baseDir.x * directionSign, baseDir.y, baseDir.z);

        rb.linearVelocity = -finalDir * speedExplode;
        rb.linearDamping = 1.8f;

        Die?.Invoke(time);
    }
}
