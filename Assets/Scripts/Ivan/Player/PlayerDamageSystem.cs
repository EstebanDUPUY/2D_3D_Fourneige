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
        rb.linearVelocity = (Vector3.right + Vector3.up * 0.07f).normalized * 62f;

        Die?.Invoke();
    }
}
