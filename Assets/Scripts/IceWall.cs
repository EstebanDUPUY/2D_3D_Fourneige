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
        //PlayerIceSystem player = other.gameObject.GetComponent<PlayerIceSystem>();
        //if (player != null)
        //{
        //    rbPlayer = player.rb;
        //    movePlayer = true;
        //    iceWallData.PlayerEnter(this);
        //    if (!isMoving)
        //    {
        //        StartCoroutine(MoveWall());
        //    }
        //}
        StartCoroutine(MoveWall());
    }

    //void OnCollisionExit(Collision other)
    //{
    //    PlayerIceSystem player = other.gameObject.GetComponent<PlayerIceSystem>();
    //    if (player != null)
    //    {
    //        iceWallData.PlayerExit();
    //        movePlayer = false;
    //    }
    //}
  
    IEnumerator MoveWall()
    {
        isMoving = true;
        float targetY = startY + range;

        while (transform.position.y < targetY)
        {
            velocity = Vector3.up * speed * Time.deltaTime;
            transform.Translate(velocity);

            //if (movePlayer)
            //{
            //    rbPlayer.transform.Translate(velocity);
            //}

            yield return null; // attendre la prochaine frame
        }

        while (transform.position.y > startY)
        {
            velocity = -Vector3.up * speed * Time.deltaTime;
            transform.Translate(velocity);
            // rbPlayer.transform.Translate(velocity);
            yield return null;
        }
        isMoving = false;
    }
}
