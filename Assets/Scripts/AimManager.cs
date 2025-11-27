using Singletons;
using Unity.Cinemachine;
using UnityEngine;

public class AimManager : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera aimCamera;
    [Tooltip("Standard Priority is usually 10. We set Aim to higher than that.")]
    [SerializeField] private int highPriority = 20;
    [SerializeField] private int lowPriority = 0;

    [Header("Vignette Effect (Tunnel Vision)")]
    [SerializeField] private CanvasGroup vignetteCanvasGroup;
    [SerializeField] private float vignetteSpeed = 10f;

    private void Update()
    {
        // 1. Check Input
        bool isAiming = InputManager.Instance != null && InputManager.Instance.IsAiming;

        // 2. Handle Cinemachine Priority
        if (aimCamera != null)
        {
            // If aiming, AimCam becomes active (Priority 20 > 10). 
            // If not, it sleeps (Priority 0 < 10).
            aimCamera.Priority = isAiming ? highPriority : lowPriority;
        }

        // 3. Handle UI Vignette (Dark edges)
        if (vignetteCanvasGroup != null)
        {
            float targetAlpha = isAiming ? 1f : 0f;
            vignetteCanvasGroup.alpha = Mathf.Lerp(vignetteCanvasGroup.alpha, targetAlpha, Time.deltaTime * vignetteSpeed);
        }
    }
}