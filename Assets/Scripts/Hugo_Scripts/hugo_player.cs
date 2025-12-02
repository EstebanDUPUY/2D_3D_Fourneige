using UnityEngine;
using UnityEngine.InputSystem;

public class hugo_player : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 moveInput;
    public float moveSpeed = 500;
    public float gravity = -0.1f;
    void Start()
    {
        rb = GetComponent<Rigidbody>();

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rb.linearVelocity = new Vector3(moveInput.x, gravity, 0) * Time.fixedDeltaTime * moveSpeed;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        if (context.canceled)
        {
            moveInput = Vector3.zero;
        }
    }
}
