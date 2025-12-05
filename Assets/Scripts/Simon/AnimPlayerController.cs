using UnityEngine;

public class AnimPlayerController : MonoBehaviour
{
    Animator anim;
    Rigidbody rb;

    public bool isDashing;
    public bool isWallJumping;
    public bool isDead;

    public bool dashTriggered;
    public bool deadTriggered;

    void Start() 
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void Update() 
    {
        float moveSpeed = Mathf.Abs(rb.linearVelocity.x);
        anim.SetFloat("Speed", moveSpeed);

        anim.SetBool("isJumping", rb.linearVelocity.y > 0);
        anim.SetBool("isFalling", rb.linearVelocity.y < 0);
        anim.SetBool("isDashing", isDashing);
        anim.SetBool("isWallJumping", isWallJumping);
        anim.SetBool("isDead", isDead);

        if (dashTriggered)
        {            
            Debug.Log("→ DASH TRIGGER SENT");
            anim.SetTrigger("dashTrigger");
            dashTriggered = false; // pour éviter de spam
        }

        if (deadTriggered)
        {
            Debug.Log("→ DEAD TRIGGER SENT");
            anim.SetTrigger("deadTrigger");
            deadTriggered = false;
        }
    }
}
