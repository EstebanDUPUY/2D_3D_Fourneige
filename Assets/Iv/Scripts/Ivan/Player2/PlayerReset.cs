using UnityEngine;

public class PlayerReset : MonoBehaviour
{
    PlayerController player;
    Vector3 savePos;
    Quaternion saveRot;

    Rigidbody rb;

    public Checkpoint lastCheckpoint;

    void OnEnable()
    {
        LevelManager2.OnLevelReset += Reset;
    }

    void OnDisable()
    {
        LevelManager2.OnLevelReset -= Reset;
    }

    void Awake()
    {
        player = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0;
        // Save la pos et rot du player au depart
        savePos = transform.position;
        saveRot = transform.rotation;
    }

    public void SaveCheckpoint(Checkpoint checkpoint)
    {
        lastCheckpoint = checkpoint;
        savePos = lastCheckpoint.transform.position;
        saveRot = lastCheckpoint.transform.rotation;
    }

    void Reset()
    {
        Debug.Log("PlayerReset -> Reset() avant");
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.MovePosition(savePos);
        rb.MoveRotation(saveRot);
        player.StopMoving = false;

        Debug.Log("PlayerReset -> Reset() apres");
    }
}
