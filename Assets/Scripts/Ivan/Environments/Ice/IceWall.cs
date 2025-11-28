using System.Collections;
using UnityEngine;

public class IceWall : EnvironmentGame
{
    public IceWallData iceWallData;

    public float dragOnIce = 45f;
    private float speed = 2f;
    public float range = 5f;

    public Vector3 velocity;

    float startY;

    Rigidbody rbPlayer;

    void Awake()
    {
        startY = transform.position.y;
    }

    void OnCollisionEnter(Collision other)
    {
        PlayerIceSystem player = other.gameObject.GetComponent<PlayerIceSystem>();
        if (player != null)
        {
            rbPlayer = player.rb;
            iceWallData.PlayerEnter(this);
            // StartCoroutine(MoveWall());
        }
    }

    void OnCollisionExit(Collision other)
    {
        PlayerIceSystem player = other.gameObject.GetComponent<PlayerIceSystem>();
        if (player != null)
            iceWallData.PlayerExit();
    }

    IEnumerator MoveWall()
    {
        float targetY = startY + range;

        while (transform.position.y < targetY)
        {
            velocity = Vector3.up * speed * Time.deltaTime;
            transform.Translate(velocity);
            // rbPlayer.transform.Translate(velocity);

            yield return null; // attendre la prochaine frame
        }

        while (transform.position.y > startY)
        {
            velocity = -Vector3.up * speed * Time.deltaTime;
            transform.Translate(velocity);
            // rbPlayer.transform.Translate(velocity);
            yield return null;
        }
    }
}
