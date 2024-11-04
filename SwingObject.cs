using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwingObject : MonoBehaviour
{
    public Transform holdPosition;  // The position that swings
    public float moveSpeed = 5f;    // Speed of the swing motion
    public float maxSwingAngle = 45f; // Maximum angle for swinging left and right
    public float swingSpeed = 2f;    // Speed of the swing motion

    private bool isSwinging = false;  // Whether the hold position is swinging
    private InputAction swingAction;   // Action for swinging

    private void Awake()
    {
        // Create an InputAction for swinging (M key)
        swingAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/m");
    }

    private void OnEnable()
    {
        // Enable the action
        swingAction.Enable();
    }

    private void OnDisable()
    {
        // Disable the action
        swingAction.Disable();
    }

    void Update()
    {
        HandleSwingMotion();
    }

    // Handles the swinging motion of the hold position
    private void HandleSwingMotion()
    {
        if (swingAction.IsPressed())  // Check if the "M" key is pressed
        {
            // Calculate new rotation angle based on swing speed
            float swingAngle = maxSwingAngle * Mathf.Sin(Time.time * swingSpeed);
            
            // Rotate the holdPosition
            holdPosition.localRotation = Quaternion.Euler(0, swingAngle, 0);
            
            isSwinging = true;  // Set swinging to true when moving
        }
        else if (isSwinging)
        {
            // Reset rotation back to neutral when swinging stops
            holdPosition.localRotation = Quaternion.Lerp(
                holdPosition.localRotation, Quaternion.identity, Time.deltaTime * moveSpeed);
            
            // Check if the holdPosition has returned to neutral rotation
            if (Quaternion.Angle(holdPosition.localRotation, Quaternion.identity) < 0.01f)
                isSwinging = false;
        }
    }
}
