using System.Collections;
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
            // Ici Gérer le die
        }
        Reset();
    }

    public void Fall()
    {
        Debug.Log("Fall");
        rb.useGravity = true;
        isUp = false;
    }

    public void ResetGravity()
    {
        rb.useGravity = false;
    }

    IEnumerator Reset()
    {
        Debug.Log("Reset");
        gameObject.SetActive(false);
        yield return new WaitForSeconds(1f);
        ResetGravity();
        transform.position = position;
        isUp = true;
    }
}
