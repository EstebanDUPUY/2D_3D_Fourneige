using UnityEngine;

public class Anim_Player_Controller : MonoBehaviour
{
    Animator anim;

    void Start() 
    {
        anim = GetComponent<Animator>();
    }

    void Update() {
        float moveSpeed = Mathf.Abs(rb.velocity.x); // vitesse horizontale
        anim.SetFloat("Speed", moveSpeed);

        anim.SetBool("IsJumping", !isGrounded && rb.velocity.y > 0);
        anim.SetBool("IsFalling", !isGrounded && rb.velocity.y < 0);
        anim.SetBool("IsDashing", isDashing);
        anim.SetBool("IsWallJumping", isWallJumping);
        anim.SetBool("IsDead", isDead);

        // Pour triggers ponctuels comme Dash ou Die
        if(dashTriggered) anim.SetTrigger("DashTrigger");
        if(deadTriggered) anim.SetTrigger("DieTrigger");
    }
}