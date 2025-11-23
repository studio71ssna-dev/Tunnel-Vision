using UnityEngine;
using Singletons;

[RequireComponent(typeof(CharacterController))] // *** NEW: Enforces Physics controller ***
public class PlayerController : MonoBehaviour
{
    #region General Variables
    [Header("Movement Config")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _gravity = -9.81f;

    [Header("Look Config")]
    [SerializeField] private Transform _cameraContainer; // *** NEW: Drag your Camera Holder here ***
    [SerializeField] private float _lookSensitivity = 2f;
    [SerializeField] private float _lookXLimit = 85f; // Prevents neck breaking

    private CharacterController _characterController;
    private Vector3 _velocity; // Stores vertical velocity for gravity
    private float _xRotation = 0f; // Internal tracker for up/down look
    #endregion

    #region General Methods

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        MovePlayer();
        LookAround();
        ApplyGravity();
    }
    #endregion

    #region Created Methods
    private void MovePlayer()
    {
        // 1. Get Input
        Vector2 input = InputManager.Instance.MoveDirection;

        // 2. Calculate Direction relative to where we are facing
        Vector3 move = transform.right * input.x + transform.forward * input.y;

        // 3. Move using CharacterController (Handles collisions automatically)
        _characterController.Move(move * _moveSpeed * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        // Reset gravity if on ground
        if (_characterController.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; // Small downward force to keep snapped to ground
        }

        // Apply gravity over time
        _velocity.y += _gravity * Time.deltaTime;

        // Apply vertical movement
        _characterController.Move(_velocity * Time.deltaTime);
    }

    private void LookAround()
    {
        // 1. Horizontal Look (Rotates the Player Body)
        float mouseX = InputManager.Instance.HorizontalLook * _lookSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        // 2. Vertical Look (Rotates the Camera Container only)
        // Note: We check if cameraContainer exists to avoid errors
        if (_cameraContainer != null)
        {
            float mouseY = InputManager.Instance.VerticalLook * _lookSensitivity * Time.deltaTime;

            _xRotation -= mouseY; // Minus because Unity rotations are inverted for Pitch
            _xRotation = Mathf.Clamp(_xRotation, -_lookXLimit, _lookXLimit);

            _cameraContainer.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        }
    }
    #endregion
}