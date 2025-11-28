using System;
using UnityEngine;

public class PlayerDamageSystem : MonoBehaviour
{
    public static Action Die;

    // PlayerController player;
    Rigidbody rb;

    public float speedExplode;

    void Awake()
    {
        // player = GetComponent<PlayerController>();
        rb = GetComponent<PlayerIceSystem>().GetRigidbody();
    }

    public void Explode()
    {
        Debug.Log("PlayerDamageSystem Explode Call");
        // rb.AddForce(-Vector3.right * 20f, ForceMode.Impulse);
        // rb.AddExplosionForce(2000f, rb.transform.position, 10f, 0f, ForceMode.Impulse);
        rb.linearVelocity = (Vector3.right + Vector3.up * 0.07f).normalized * 62f;

        Die?.Invoke();
    }
}
