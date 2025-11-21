using UnityEngine;

public class TurretController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Transform _cameraHolder;
    [SerializeField] private float _sensitivityX = 15f;
    [SerializeField] private float _sensitivityY = 15f;
    [SerializeField] private float _upperLimit = 80f;
    [SerializeField] private float _lowerLimit = -80f;

    private float _xRotation; // Pitch (Up/Down)
    private Vector2 _currentInput;

    // LINK THIS TO: InputBridge -> OnLookOutput
    public void UpdateLookInput(Vector2 input)
    {
        _currentInput = input;
    }

    private void Update()
    {
        // Apply rotation in Update for smoothness
        if (_currentInput == Vector2.zero) return;

        // 1. Horizontal Rotation (Yaws the whole body)
        float mouseX = _currentInput.x * _sensitivityX * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        // 2. Vertical Rotation (Pitches just the camera)
        float mouseY = _currentInput.y * _sensitivityY * Time.deltaTime;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, _lowerLimit, _upperLimit);

        if (_cameraHolder != null)
        {
            _cameraHolder.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        }
    }
}