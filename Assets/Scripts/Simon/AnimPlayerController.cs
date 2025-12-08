using UnityEngine;

public class AnimPlayerController : MonoBehaviour
{
    Animator anim;
    Rigidbody rb;

    public bool IsDashing;
    public bool IsWallJumping;
    public bool IsDead;
    public bool IsWallSliding;


    void Start() 
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void Update() 
    {
        float moveSpeed = Mathf.Abs(rb.linearVelocity.x);
        anim.SetFloat("Speed", moveSpeed);

        anim.SetBool("IsJumping", rb.linearVelocity.y > 0);
        anim.SetBool("IsFalling", rb.linearVelocity.y < 0);
        anim.SetBool("IsDashing", IsDashing);
        anim.SetBool("IsWallJumping", IsWallJumping);
        anim.SetBool("IsDead", IsDead);
        anim.SetBool("IsWallSliding", IsWallSliding);
    }
}
