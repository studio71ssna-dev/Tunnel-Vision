using Singletons;
using Unity.Cinemachine;
using UnityEngine;

public class AimManager : MonoBehaviour
{
    [Header("Cinemachine Aim Camera")]
    [SerializeField] private CinemachineCamera aimCamera;
    [SerializeField] private int highPriority = 20;
    [SerializeField] private int lowPriority = 0;

    [Header("Vignette")]
    [SerializeField] private CanvasGroup vignetteCanvasGroup;
    [SerializeField] private float vignetteSpeed = 10f;

    [Header("Tunnel Vision (FOV Shrink on Fire)")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float fireFOV = 45f;
    [SerializeField] private float shrinkSpeed = 30f;
    [SerializeField] private float recoverSpeed = 18f;

    private float _currentFOV;
    private bool _fireKickRequested;
    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;

        if (_mainCamera != null)
        {
            _currentFOV = normalFOV;
            _mainCamera.fieldOfView = normalFOV;
        }
    }

    private void Update()
    {
        bool isAiming = InputManager.Instance != null && InputManager.Instance.IsAiming;

        // 1️⃣ Cinemachine camera priority (aim zoom camera)
        if (aimCamera != null)
        {
            aimCamera.Priority = isAiming ? highPriority : lowPriority;
        }

        // 2️⃣ Vignette (aim state)
        if (vignetteCanvasGroup != null)
        {
            float targetAlpha = isAiming ? 1f : 0f;
            vignetteCanvasGroup.alpha = Mathf.Lerp(
                vignetteCanvasGroup.alpha,
                targetAlpha,
                Time.deltaTime * vignetteSpeed
            );
        }

        // 3️⃣ TUNNEL VISION (ACTUAL CAMERA FOV)
        if (_mainCamera != null)
        {
            float targetFOV = _fireKickRequested ? fireFOV : normalFOV;
            float speed = _fireKickRequested ? shrinkSpeed : recoverSpeed;

            _currentFOV = Mathf.Lerp(_currentFOV, targetFOV, Time.deltaTime * speed);
            _mainCamera.fieldOfView = _currentFOV;

            // Once we reach the shrink target, allow recovery
            if (_fireKickRequested && Mathf.Abs(_currentFOV - fireFOV) < 0.5f)
            {
                _fireKickRequested = false;
            }
        }
    }

    // 🔥 Called by WeaponController every shot
    public void RequestFireFOVKick()
    {
        _fireKickRequested = true;
    }

    // 🔄 Called when reload starts
    public void ForceRecoverFOV()
    {
        _fireKickRequested = false;
        _currentFOV = normalFOV;

        if (_mainCamera != null)
            _mainCamera.fieldOfView = normalFOV;
    }
}
