using UnityEngine;

public class AnimPlayerController : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    public PlayerController player;
    public bool IsJumping;
    public bool IsWallJumping;
    public bool IsDead;
    public bool IsWallSliding;
    public bool IsDashing;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        anim.SetFloat("Speed", Mathf.Abs(player.GetComponent<Rigidbody>().linearVelocity.x));
        anim.SetBool("IsGrounded", player.IsGrounded);
        anim.SetBool("IsJumping", player.IsJumpingAnim);
        anim.SetBool("IsFalling", player.IsFallingAnim);
        anim.SetBool("IsWallSliding", player.IsWallSliding);
        anim.SetBool("IsDashing", player.IsDashing);
        anim.SetBool("IsDead", false);
        anim.SetBool("IsWallJumping", player.IsWallJumping);
    }
}
