using UnityEngine;

public class PlayerDamageSystem : MonoBehaviour
{
    PlayerController player;
    Rigidbody rb;

    public float speedExplode;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        rb = player.GetComponent<PlayerIceSystem>().GetRigidbody();
    }

    public void Explode()
    {
        Debug.Log("PlayerDamageSystem Explode Call");
        // rb.AddForce(Vector3.right * speedExplode, ForceMode.Force);
    }
}
