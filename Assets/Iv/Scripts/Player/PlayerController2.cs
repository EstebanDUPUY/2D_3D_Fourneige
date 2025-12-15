using UnityEngine;

public class PlayerController2 : MonoBehaviour
{
    public Rigidbody rb;

    bool moving = true;

    void OnEnable()
    {
        LevelManager2.OnLevelComplete += Stop;
    }

    void OnDisable()
    {
        LevelManager2.OnLevelComplete -= Stop;
    }

    void Update()
    {
        if (moving)
        {
            rb.transform.Translate(Vector3.right * 7 * Time.deltaTime);
        }
    }

    void Stop()
    {
        moving = false;
    }
}
