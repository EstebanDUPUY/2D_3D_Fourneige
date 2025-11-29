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
        rb.linearDamping = 0;
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

        rb.transform.position = startPos;
        rb.transform.rotation = startRot;
    }
}
