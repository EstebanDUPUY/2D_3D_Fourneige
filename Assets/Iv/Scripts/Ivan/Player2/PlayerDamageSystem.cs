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
        //player = GetComponent<PlayerController>();
        // rb = GetComponent<PlayerIceSystem>().GetRigidbody();
        rb = GetComponent<Rigidbody>();
    }

    public void DieNow()
    {
        Die?.Invoke();
    }
}
