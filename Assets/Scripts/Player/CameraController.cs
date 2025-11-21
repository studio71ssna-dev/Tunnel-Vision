using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _defaultFOV = 60f;
    [SerializeField] private float _zoomMultiplier = 0.4f; // Zoom makes view 40% of normal
    [SerializeField] private float _zoomSpeed = 10f;

    private Camera _cam;
    private bool _isAiming = false;
    private float _currentBaseFOV;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _currentBaseFOV = _defaultFOV;
    }

    // LINK TO: InputManager -> OnScopeOutput
    public void SetScopeState(bool isAiming)
    {
        _isAiming = isAiming;
    }

    // OPTIONAL: Call this from GameManager as timer ticks down
    public void SetTunnelVision(float percent01)
    {
        // As percent goes to 0, FOV shrinks from 60 to 30
        _currentBaseFOV = Mathf.Lerp(30f, _defaultFOV, percent01);
    }

    private void Update()
    {
        // 1. Calculate Target FOV
        float targetFOV = _currentBaseFOV;

        if (_isAiming)
        {
            targetFOV *= _zoomMultiplier;
        }

        // 2. Smoothly Interpolate (The Cinemachine replacement)
        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, Time.deltaTime * _zoomSpeed);
    }
}