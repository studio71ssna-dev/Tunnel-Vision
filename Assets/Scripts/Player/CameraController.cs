using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Weapon Reference")]
    [SerializeField] private WeaponController weaponController;

    [Header("FOV Settings")]
    [Tooltip("Maximum (default) FOV")]
    [SerializeField] private int maxFOV = 60;
    [Tooltip("Minimum (zoomed-in) FOV")]
    [SerializeField] private int minFOV = 40;

    [Header("Speed")]
    [SerializeField] private float zoomSpeed = 6f; // how fast to lerp fov when zooming in (toward minFOV)
    [SerializeField] private float zoomOutSpeed = 8f; // how fast to lerp fov when zooming out (toward maxFOV)

    private Camera _cam;
    private float _targetFOV;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null)
        {
            Debug.LogWarning("CameraController: no Camera found on this GameObject.");
        }

        // Initialize target to current or configured max
        _targetFOV = _cam != null ? _cam.fieldOfView : maxFOV;
    }

    private void OnEnable()
    {
        if (weaponController != null)
        {
            weaponController.OnAmmoChanged.AddListener(OnAmmoChanged);
            weaponController.OnReloadingState.AddListener(OnReloadingState);
        }
    }

    private void OnDisable()
    {
        if (weaponController != null)
        {
            weaponController.OnAmmoChanged.RemoveListener(OnAmmoChanged);
            weaponController.OnReloadingState.RemoveListener(OnReloadingState);
        }
    }

    private void Start()
    {
        // Ensure initial FOV is the configured maxFOV
        if (_cam != null)
        {
            _cam.fieldOfView = maxFOV;
            _targetFOV = _cam.fieldOfView;
        }
    }

    private void Update()
    {
        if (_cam == null) return;

        // Choose speed depending on whether we're zooming out (target > current) or zooming in
        float speed = _targetFOV > _cam.fieldOfView ? zoomOutSpeed : zoomSpeed;

        // Smoothly move FOV toward target using the chosen speed
        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, _targetFOV, Time.deltaTime * speed);
    }

    // Called when weapon ammo changes: current and max
    private void OnAmmoChanged(int current, int max)
    {
        if (max <= 0)
        {
            _targetFOV = maxFOV;
            return;
        }

        // Compute ratio of bullets used (0..1)
        float usedRatio = (max - current) / (float)max;
        // Map to FOV between maxFOV and minFOV
        _targetFOV = Mathf.Lerp(maxFOV, minFOV, usedRatio);
    }

    // Called when reload starts/ends
    private void OnReloadingState(bool isReloading)
    {
        if (isReloading)
        {
            // Do not change FOV while reload is in progress — remain at current zoom level
            return;
        }

        // When reload finishes, reset to initial FOV
        _targetFOV = maxFOV;
    }
}