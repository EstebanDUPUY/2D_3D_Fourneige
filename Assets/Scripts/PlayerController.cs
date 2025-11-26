using Mono.Cecil;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // region for declared variables, where every repetitively used variable is declared
    #region DECLARED VARIABLES

    [Header("Basic Motions")] // header that designates the basic motions section (like left and right movement)
    public float moveSpeed;
    public float aceleration;
    public float startingSpeedBoost;
    public float deceleration;

    [Header("Basic Character Settings")] // header that designates the basic character settings (like gravity, friction and weight)
    public float blabla;



    public enum States { fire, ice}; // enum that declares both states: fire and ice
    public States currentState; // State that declares the current state of the player

    #endregion

    // region for unity methods like Start and Update
    #region START, UPDATE, ETC...
    private void Awake()
    {
        
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
