using System.Collections;
using UnityEngine;

public class IceWall : EnvironmentGame
{
    public IceWallData iceWallData;

    public float dragOnIce = 45f;
    public float speed = 4f;
    public float range = 10f;

    float startX;

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
        float targetX = startX + range;

        while (transform.position.x < targetX)
        {
            transform.Translate(Vector3.right * speed * Time.deltaTime);

            yield return null; // attendre la prochaine frame
        }

        while (transform.position.x > startX)
        {
            transform.Translate(-Vector3.right * speed * Time.deltaTime);
            yield return null;
        }
    }
}
