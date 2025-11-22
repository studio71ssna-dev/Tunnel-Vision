using UnityEngine;
using Unity.Cinemachine;

public static class CameraSwitcher
{
    private static CinemachineCamera _current = null;
    private static int _currentPriority = 0;

    /// <summary>
    /// Immediately activates the provided virtual camera by setting its priority.
    /// Lowers the previous camera's priority to 0 (effectively deactivating it).
    /// </summary>
    public static void ActivateCamera(CinemachineCamera newCam, int newPriority = 20)
    {
        if (newCam == null) return;
        if (_current == newCam)
        {
            // still ensure priority is correct
            _current.Priority = newPriority;
            _currentPriority = newPriority;
            return;
        }

        // lower previous
        if (_current != null)
            _current.Priority = 0;

        // raise new
        newCam.Priority = newPriority;

        _current = newCam;
        _currentPriority = newPriority;
    }

    /// <summary>
    /// Force revert by activating targetCam (commonly the dolly / default camera).
    /// Pass expected priority for the restored camera.
    /// </summary>
    public static void RevertTo(CinemachineCamera targetCam, int priority = 10)
    {
        ActivateCamera(targetCam, priority);
    }

    /// <summary>
    /// Get the currently active virtual camera (may be null).
    /// </summary>
    public static CinemachineCamera CurrentCamera => _current;
    public static int CurrentPriority => _currentPriority;
}
