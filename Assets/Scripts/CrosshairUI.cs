using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // NEW: Required for the new Input System

public class CrosshairUI : MonoBehaviour
{
    private RectTransform crosshairRectTransform;
    private PlayerControls controls;

    void Awake()
    {
        crosshairRectTransform = GetComponent<RectTransform>();

        // NEW: Initialize PlayerControls here
        controls = new PlayerControls();
    }

    void OnEnable()
    {
        // NEW: Enable the input controls
        controls.Enable();
    }

    void OnDisable()
    {
        // NEW: Disable the input controls when the object is disabled
        controls.Disable();
    }

    void Update()
    {
        // NEW: Read the mouse position using the new Input System
        // This assumes your PlayerControls has a 'Player' Action Map
        // and a 'MousePosition' Action that reads a Vector2 (which it does based on WeaponController)
        Vector2 currentMousePosition = controls.Player.MousePosition.ReadValue<Vector2>();

        // Set the position of the crosshair UI element to the current mouse position.
        crosshairRectTransform.position = currentMousePosition;
    }
}