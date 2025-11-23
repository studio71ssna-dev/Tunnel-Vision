using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Weapon Reference")]
    [SerializeField] private WeaponController weaponController;

    [Header("Vignette Effect")]
    [Tooltip("Drag the UI Panel/Image with the CanvasGroup component here")]
    [SerializeField] private CanvasGroup vignetteCanvasGroup;
    [Range(0f, 1f)]
    [SerializeField] private float maxVignetteIntensity = 0.9f; // How dark it gets at 0 ammo

    [Header("FOV Settings")]
    [Tooltip("Maximum (default) FOV")]
    [SerializeField] private int maxFOV = 60;
    [Tooltip("Minimum (zoomed-in) FOV")]
    [SerializeField] private int minFOV = 40;

    [Header("Speed")]
    [SerializeField] private float zoomSpeed = 6f;
    [SerializeField] private float zoomOutSpeed = 8f;

    private Camera _cam;
    private float _targetFOV;
    private float _targetVignetteAlpha;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null)
        {
            Debug.LogWarning("CameraController: no Camera found on this GameObject.");
        }

        // Initialize defaults
        _targetFOV = _cam != null ? _cam.fieldOfView : maxFOV;
        _targetVignetteAlpha = 0f;
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
        if (_cam != null)
        {
            _cam.fieldOfView = maxFOV;
            _targetFOV = maxFOV;
        }

        // Ensure vignette starts invisible
        if (vignetteCanvasGroup != null)
        {
            vignetteCanvasGroup.alpha = 0f;
        }
    }

    private void Update()
    {
        // 1. Handle FOV Zoom
        if (_cam != null)
        {
            float fovSpeed = _targetFOV > _cam.fieldOfView ? zoomOutSpeed : zoomSpeed;
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, _targetFOV, Time.deltaTime * fovSpeed);
        }

        // 2. Handle Vignette Fade
        if (vignetteCanvasGroup != null)
        {
            // Use same speed as zoom for sync feel
            float vignetteSpeed = _targetVignetteAlpha < vignetteCanvasGroup.alpha ? zoomOutSpeed : zoomSpeed;
            vignetteCanvasGroup.alpha = Mathf.Lerp(vignetteCanvasGroup.alpha, _targetVignetteAlpha, Time.deltaTime * vignetteSpeed);
        }
    }

    // Called when weapon ammo changes
    private void OnAmmoChanged(int current, int max)
    {
        if (max <= 0)
        {
            _targetFOV = maxFOV;
            _targetVignetteAlpha = 0f;
            return;
        }

        // Compute ratio of bullets used (0 = full ammo, 1 = empty)
        float usedRatio = (max - current) / (float)max;

        // 1. Set FOV Target
        _targetFOV = Mathf.Lerp(maxFOV, minFOV, usedRatio);

        // 2. Set Vignette Target
        _targetVignetteAlpha = Mathf.Lerp(0f, maxVignetteIntensity, usedRatio);
    }

    // Called when reload starts/ends
    private void OnReloadingState(bool isReloading)
    {
        if (isReloading)
        {
            // Optional: You can choose to reset immediately here if you prefer
            // _targetFOV = maxFOV;
            // _targetVignetteAlpha = 0f;
            return;
        }

        // When reload finishes, reset everything
        _targetFOV = maxFOV;
        _targetVignetteAlpha = 0f;
    }
}