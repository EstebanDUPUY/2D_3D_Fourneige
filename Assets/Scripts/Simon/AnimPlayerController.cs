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
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void Update() 
    {
        float moveSpeed = Mathf.Abs(rb.linearVelocity.x);
        anim.SetFloat("Speed", moveSpeed);

        anim.SetBool("IsJumping", rb.linearVelocity.y > 0);
        anim.SetBool("IsFalling", rb.linearVelocity.y < 0);
        anim.SetBool("IsDashing", isDashing);
        anim.SetBool("IsWallJumping", isWallJumping);
        anim.SetBool("IsDead", isDead);

        if (dashTriggered)
        {
            anim.SetTrigger("DashTrigger");
            dashTriggered = false; // pour éviter de spam
        }

        if (deadTriggered)
        {
            anim.SetTrigger("DieTrigger");
            deadTriggered = false;
        }
    }
}
