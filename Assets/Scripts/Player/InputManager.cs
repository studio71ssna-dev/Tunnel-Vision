using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using SingletonManager;
using System;

namespace Singletons
{
    public class InputManager : SingletonPersistent
    {
        public static InputManager Instance => GetInstance<InputManager>();

        #region Properties of InputSystem Class
        public delegate void OnActionEvent();
        private InputAction MoveInput;
        private InputAction LookInput;
        #endregion

        #region General Methods
        public Vector2 MoveDirection { get; private set; }
        public float HorizontalLook { get; private set; }
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

            MoveInput = playerInput.actions.FindAction("Move");
            LookInput = playerInput.actions.FindAction("Look");

            if (MoveInput == null || LookInput == null)
                print("Input System actions missing on InputManager!");

            // Lock and hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            MoveAction();
            LookAction();
        }
        #endregion

        #region Event Methods
        private void MoveAction()
        {
            MoveDirection = MoveInput.ReadValue<Vector2>();
        }

        private void LookAction()
        {
            if (Mouse.current == null) return;

            Vector2 delta = LookInput.ReadValue<Vector2>();
            HorizontalLook = delta.x;   // ONLY horizontal rotation
        }

        public void ShootAction(InputAction.CallbackContext context)
        {
            // Invoke with true when button is pressed/held, false when released
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
            if (context.performed)
                OnReload?.Invoke();
        }

        public void SwapAction(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnSwap?.Invoke();
        }
        #endregion
    }
}