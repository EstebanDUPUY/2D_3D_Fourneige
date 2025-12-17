using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Stalactite : MonoBehaviour
{
    public Rigidbody rb;
    Vector3 position;

    public bool isUp = true;

    void Awake()
    {
        position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerDamageSystem playerDamage = other.gameObject.GetComponent<PlayerDamageSystem>();
            playerDamage.DieNow(1f);
            // Ici Gérer le die
        }
        StartCoroutine(Reset());
    }

    public void Fall()
    {
        rb.useGravity = true;
        isUp = false;
    }

    IEnumerator Reset()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.enabled = false;
        yield return new WaitForSeconds(1f);
        renderer.enabled = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        transform.position = position;
        isUp = true;
    }
}
