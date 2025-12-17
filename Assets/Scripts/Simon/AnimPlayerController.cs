using UnityEngine;

public class AnimPlayerController : MonoBehaviour
{
    Animator anim;
    Rigidbody rb;
    public PlayerController player;

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
        anim.SetFloat("Speed", Mathf.Abs(player.GetComponent<Rigidbody>().linearVelocity.x));
        anim.SetBool("IsGrounded", player.IsGrounded);
        anim.SetBool("IsJumping", player.IsJumpingAnim);
        anim.SetBool("IsWallJumping", player.IsWallJumping);
        anim.SetBool("IsFalling", player.IsFallingAnim);
        anim.SetBool("IsWallSliding", player.IsWallSliding);
        anim.SetBool("IsDashing", player.IsDashing);
        anim.SetBool("IsDead", IsDead);

    }
}
