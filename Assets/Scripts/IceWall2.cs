using System.Collections;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class IceWall2 : MonoBehaviour
{
    //public IceWallData iceWallData;

    public float dragOnIce = 45f;
    private float speed = 2f;
    public float range = 5f;

    public Vector3 velocity;

    float startY;

    Rigidbody rbPlayer;
    bool movePlayer;
    bool isMoving;

    void Awake()
    {
        startY = transform.position.y;
    }

    void OnCollisionEnter(Collision other)
    {
        // StartCoroutine(MoveWall());
        if (!isMoving)
        {
            StartCoroutine("MoveWall");
        }
    }

    IEnumerator MoveWall()
    {
        isMoving = true;
        float targetY = startY + range;

        while (transform.position.y < targetY)
        {
            velocity = Vector3.up * speed * Time.deltaTime;
            transform.Translate(velocity);
            yield return null; // attendre la prochaine frame
        }

        while (transform.position.y > startY)
        {
            velocity = -Vector3.up * speed * Time.deltaTime;
            transform.Translate(velocity);
            yield return null;
        }
        isMoving = false;
    }
}
