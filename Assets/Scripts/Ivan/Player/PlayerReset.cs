using UnityEngine;

public class ResetPlayer : MonoBehaviour
{
    Vector3 startPos;
    Quaternion startRot;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startPos = transform.position;
        startRot = transform.rotation;

        // autres init si besoin
    }

    void OnEnable()
    {
        LevelManager.OnLevelReset += Reset;
    }

    void OnDisable()
    {
        LevelManager.OnLevelReset -= Reset;
    }

    void Reset()
    {
        Debug.Log("ResetPlayer -> Reset()");
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = startPos;
        transform.rotation = startRot;
    }
}
