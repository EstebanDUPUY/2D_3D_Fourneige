using Mono.Cecil;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    // region for declared variables, where every repetitively used variable is declared
    #region DECLARED VARIABLES

    [Header("References")]
    public Rigidbody rb;
    public SpriteRenderer spriteRenderer;
    public bool isFacingRight = true;

    [Header("Basic Motions")] // header that designates the basic motions section (like left and right movement)
    public float moveSpeed;
    public float aceleration;
    public float startingSpeedBoost;
    public float deceleration;

    private float horizontalMovement;

    [Header("Basic Character Settings")] // header that designates the basic character settings (like gravity, friction and weight)
    public float gravity;
    public float weight;
    public float groundFriction;
    public float wallFriction;



    public enum States { fire, ice}; // enum that declares both states: fire and ice
    public States currentState; // State that declares the current state of the player

    #endregion

    // region for unity methods like Start and Update
    #region START, UPDATE, ETC...
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        currentState = States.fire;
    }

    private void Update()
    {
    
    }
    
    private void FixedUpdate() // function for physics maths
    {
        VelocityMaths();
    }
    #endregion

    // region for sprite rotation
    #region ROTATE SPRITE

    

    #endregion 

    // region for ZQSD movements
    #region BASIC MOTIONS

    public void MoveInput(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            horizontalMovement = ctx.ReadValue<Vector2>().x;
        if (ctx.canceled)
            horizontalMovement = 0f;
    }
    
    public void VelocityMaths() // private function to do the maths for velocity
    {
        rb.linearVelocity = new Vector3(horizontalMovement * moveSpeed, rb.linearVelocity.y);
    }
    #endregion

    // region where state switching is handled
    #region STATE SWITCHING
    public void SwitchStateInput(InputAction.CallbackContext ctx) // function that switches the player's state on key press
    {
        if (ctx.performed && currentState == States.fire) // if key pressed and the current state of the player equals to fire
            currentState = States.ice; // switch state to ice
        else if (ctx.performed && currentState == States.ice) // if key pressed and the current state of the player equals to ice
            currentState = States.fire; // switch state to fire
        else // if none of the two above are used
            Debug.LogError("//Custom// -> StateSwitching doesn't work properly"); // write an error inside the console
    }
    #endregion
}
