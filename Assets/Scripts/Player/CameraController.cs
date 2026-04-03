using UnityEngine;
using Unity.Cinemachine; // IMPORTANT: Namespace for Cinemachine 3.0

public class CameraController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The Virtual Camera used for standard movement (Not the Aim cam)")]
    [SerializeField] private CinemachineCamera _virtualCamera;
    [SerializeField] private WeaponController _weaponController;

    [Header("Settings")]
    [SerializeField] private float _defaultFOV = 60f;
    [SerializeField] private float _zoomFOV = 45f;
    [SerializeField] private float _zoomSpeed = 10f;

    private float _targetFOV;

    private void Start()
    {
        // Default to current setting if not assigned
        if (_virtualCamera != null)
        {
            _targetFOV = _defaultFOV;
            _virtualCamera.Lens.FieldOfView = _defaultFOV;
        }
    }

    private void Update()
    {
        if (_virtualCamera == null) return;

        // Smoothly interpolate the Lens FOV
        float currentFOV = _virtualCamera.Lens.FieldOfView;
        float newFOV = Mathf.Lerp(currentFOV, _targetFOV, Time.deltaTime * _zoomSpeed);

        // Apply back to Cinemachine
        _virtualCamera.Lens.FieldOfView = newFOV;
    }

    // Connect this to WeaponController.OnAmmoChanged
    public void OnAmmoChanged(int current, int max)
    {
        if (max <= 0) return;

        // Calculate "Stress" (Lower ammo = Lower FOV = slight zoom in)
        float stress = (float)(max - current) / max;

        // Lerp between Default (60) and Zoom (45) based on empty mag
        _targetFOV = Mathf.Lerp(_defaultFOV, _zoomFOV, stress);
    }
}