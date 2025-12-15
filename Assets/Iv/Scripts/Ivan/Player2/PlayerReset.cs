using UnityEngine;

public class ResetPlayer : MonoBehaviour
{
    PlayerController player;
    Vector3 startPos;
    Quaternion startRot;
    Rigidbody rb;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();
        startPos = transform.position;
        startRot = transform.rotation;
        rb.linearDamping = 0;
    }

    void OnEnable()
    {
        LevelManager2.OnLevelReset += Reset;
    }

    void OnDisable()
    {
        LevelManager2.OnLevelReset -= Reset;
    }

    void Reset()
    {
        Debug.Log("ResetPlayer -> Reset() avant");
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.MovePosition(startPos);
        rb.MoveRotation(startRot);
        player.StopMoving = false;

        Debug.Log("ResetPlayer -> Reset() apres");
    }
}
