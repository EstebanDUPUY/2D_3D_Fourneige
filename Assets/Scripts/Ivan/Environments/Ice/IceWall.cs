using System.Collections;
using UnityEngine;

public class IceWall : EnvironmentGame
{
    public IceWallData iceWallData;

    public float speed = 4f;
    public float range = 10f;
    public float dragOnIce = 45f;

    private float startX;
    private Rigidbody rbPlayer; // player sur le mur

    void Awake()
    {
        startX = transform.position.x;
    }

    void OnCollisionEnter(Collision other)
    {
        PlayerIceSystem player = other.gameObject.GetComponent<PlayerIceSystem>();
        if (player != null)
        {
            iceWallData.PlayerEnter(this);

            rbPlayer = player.GetComponent<Rigidbody>();
            rbPlayer.linearDamping = dragOnIce;

            // Commencer le mouvement seulement si le mur est au repos
            if (!IsInvoking("MoveWallRoutine"))
                StartCoroutine(MoveWall());
        }
    }

    void OnCollisionExit(Collision other)
    {
        PlayerIceSystem player = other.gameObject.GetComponent<PlayerIceSystem>();
        if (player != null && rbPlayer == player.GetComponent<Rigidbody>())
        {
            iceWallData.PlayerExit();

            rbPlayer.linearDamping = 0f;
            rbPlayer = null;
        }
    }

    IEnumerator MoveWall()
    {
        float targetX = startX + range;

        // Aller vers la droite
        while (transform.position.x < targetX)
        {
            Vector3 move = Vector3.right * speed * Time.deltaTime;
            transform.Translate(move);

            if (rbPlayer != null)
                rbPlayer.MovePosition(rbPlayer.position + move);

            yield return null;
        }

        // Revenir vers la gauche
        while (transform.position.x > startX)
        {
            Vector3 move = -Vector3.right * speed * Time.deltaTime;
            transform.Translate(move);

            if (rbPlayer != null)
                rbPlayer.MovePosition(rbPlayer.position + move);

            yield return null;
        }
    }
}
