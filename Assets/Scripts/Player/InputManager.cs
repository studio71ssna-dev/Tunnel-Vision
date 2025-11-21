using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class InputManager : MonoBehaviour
{
    [Header("Movement Signals")]
    // We pass the Vector2 directly to the TurretController
    public UnityEvent<Vector2> OnLookOutput;

    [Header("Combat Signals")]
    public UnityEvent OnFireOutput;
    public UnityEvent OnReloadOutput;

    // We pass the scroll value (Y-axis) to cycle weapons
    // Positive value = Next Weapon, Negative = Previous Weapon
    public UnityEvent<float> OnWeaponScrollOutput;

    // ---------------------------------------------------------
    // DRAG THESE METHODS INTO THE 'PLAYER INPUT' COMPONENT EVENTS
    // ---------------------------------------------------------

    public void OnLook(InputAction.CallbackContext context)
    {
        // Only process if the value actually changed
        if (context.performed || context.canceled)
        {
            Vector2 value = context.ReadValue<Vector2>();
            OnLookOutput.Invoke(value);
        }
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnFireOutput.Invoke();
        }
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnReloadOutput.Invoke();
        }
    }

    public void OnScroll(InputAction.CallbackContext context)
    {
        // 'performed' is triggered when the scroll wheel is moved
        if (context.performed)
        {
            float scrollValue = context.ReadValue<float>();

            // Optimization: Normalize the value immediately so your logic 
            // doesn't have to worry about 120 vs 1 vs 0.1
            // Returns 1, -1, or 0
            if (scrollValue != 0)
            {
                OnWeaponScrollOutput.Invoke(Mathf.Sign(scrollValue));
            }
        }
    }
}