using SingletonManager;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Singletons
{
    public class InputManager : SingletonPersistent
    {
        public static InputManager Instance => GetInstance<InputManager>();

        #region Properties of InputSystem Class
        public delegate void OnActionEvent();
        private InputAction MoveInput;
        private InputAction LookInput;
        private InputAction AimInput; // *** NEW: Input Action for Aiming ***
        #endregion

        #region General Methods
        public Vector2 MoveDirection { get; private set; }
        public float HorizontalLook { get; private set; }
        public float VerticalLook { get; private set; }
        public bool IsAiming { get; private set; } // *** NEW: Public property other scripts can check ***
        #endregion

        #region Event Properties
        public event Action<bool> OnShoot;
        public event OnActionEvent OnReload;
        public event OnActionEvent OnSwap;
        #endregion

        #region General Methods
        private void Start()
        {
            var playerInput = GetComponent<PlayerInput>();

            if (playerInput == null)
            {
                Debug.LogError("InputManager: PlayerInput component missing!");
                return;
            }

            MoveInput = playerInput.actions.FindAction("Move");
            LookInput = playerInput.actions.FindAction("Look");
            AimInput = playerInput.actions.FindAction("Aim"); // *** NEW: Initialize Aim Action ***

            // Expanded error checking to include AimInput
            if (MoveInput == null || LookInput == null)
            {
                Debug.LogError("Input System actions (Move/Look) missing on InputManager!");
            }

            if (AimInput == null)
            {
                Debug.LogError("Input System action 'Aim' is missing! Make sure you added it to your Input Action Asset with the name 'Aim'.");
            }

            // Lock and hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            MoveAction();
            LookAction();
            AimAction(); // *** NEW: Update Aim state every frame ***
        }
        #endregion

        #region Event Methods
        private void MoveAction()
        {
            if (MoveInput != null)
                MoveDirection = MoveInput.ReadValue<Vector2>();
        }

        private void LookAction()
        {
            if (Mouse.current == null || LookInput == null) return;

            Vector2 delta = LookInput.ReadValue<Vector2>();
            HorizontalLook = delta.x;
            VerticalLook = delta.y;
        }

        // *** NEW: Handles the boolean state of aiming ***
        private void AimAction()
        {
            if (AimInput != null)
            {
                // .IsPressed() returns true as long as the button is held down
                IsAiming = AimInput.IsPressed();
            }
            else
            {
                IsAiming = false;
            }
        }

        public void ShootAction(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started || context.phase == InputActionPhase.Performed)
            {
                OnShoot?.Invoke(true);
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                OnShoot?.Invoke(false);
            }
        }

        public void ReloadAction(InputAction.CallbackContext context)
        {
            if (context.performed) OnReload?.Invoke();
        }

        public void SwapAction(InputAction.CallbackContext context)
        {
            if (context.performed) OnSwap?.Invoke();
        }
        #endregion
    }
}