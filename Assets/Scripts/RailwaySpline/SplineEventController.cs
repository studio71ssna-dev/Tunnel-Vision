using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Cinemachine;

/// <summary>
/// Drives a dolly virtual camera along a SplineContainer and fires ScriptableObject-based SplineEventPoint assets.
/// When an event has an associated CinemachineVirtualCamera, the event camera's priority is raised while the event runs.
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public class SplineEventController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Spline that represents the rail")]
    [SerializeField] private SplineContainer _splinePath;

    [Tooltip("A Cinemachine Virtual Camera that has a CinemachineSplineDolly component. This is the moving dolly camera.")]
    [SerializeField] private CinemachineCamera _dollyVirtualCamera;

    [Tooltip("ScriptableObject event assets (designer-friendly). They will be converted to runtime events on Awake.")]
    [SerializeField] private SplineEventPoint[] _eventAssets = Array.Empty<SplineEventPoint>();

    private RuntimeSplineEvent[] _eventPoints;

    [Header("Movement")]
    [Tooltip("Normalized speed along spline (units: normalized position per second). Adjust to taste.")]
    [SerializeField] private float _speed = 0.1f;

    [Tooltip("If true, will loop when reaching the end of the spline")]
    [SerializeField] private bool _loop = true;

    [Tooltip("Auto-start movement on enable")]
    [SerializeField] private bool _autoStart = true;

    private CinemachineSplineDolly _splineDolly;
    private int _currentEventIndex = 0;
    private bool _isPaused = false;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        if (_splinePath == null)
            Debug.LogWarning("SplineEventController: SplineContainer not assigned.", this);

        if (_dollyVirtualCamera == null)
            Debug.LogError("SplineEventController: Dolly Virtual Camera is required.", this);

        // get the CinemachineSplineDolly component from the dolly camera GameObject
        _splineDolly = _dollyVirtualCamera.GetComponent<CinemachineSplineDolly>();
        if (_splineDolly == null)
            Debug.LogError("SplineEventController: Dolly virtual camera must have a CinemachineSplineDolly component.", this);

        // Assign the spline to dolly
        _splineDolly.Spline = _splinePath;

        // Convert ScriptableObjects to runtime events
        _eventPoints = new RuntimeSplineEvent[_eventAssets.Length];
        for (int i = 0; i < _eventAssets.Length; i++)
            _eventPoints[i] = _eventAssets[i].ToRuntime();

        SortEventPoints();
    }

    private void OnEnable()
    {
        // Ensure dolly camera has a baseline low priority so other event cams can override.
        if (_dollyVirtualCamera != null)
            _dollyVirtualCamera.Priority = 10;

        // Activate dolly as current baseline camera
        CameraSwitcher.ActivateCamera(_dollyVirtualCamera, _dollyVirtualCamera.Priority);

        if (_autoStart) StartMovement().Forget();
    }

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private void SortEventPoints()
    {
        Array.Sort(_eventPoints, (a, b) => a.splinePosition.CompareTo(b.splinePosition));
    }

    private async UniTaskVoid StartMovement()
    {
        _cts = new CancellationTokenSource();
        await UniTask.Yield(PlayerLoopTiming.Update);

        // Ensure starting values
        _currentEventIndex = 0;
        _isPaused = false;

        while (!_cts.IsCancellationRequested)
        {
            if (_currentEventIndex >= _eventPoints.Length)
            {
                if (_loop)
                {
                    ResetPath();
                }
                else
                {
                    // if not looping and no more events, still progress until end then stop
                    if (_splineDolly.CameraPosition >= 1f) break;
                }
            }

            if (!_isPaused)
            {
                _splineDolly.CameraPosition += _speed * Time.deltaTime;
                // clamp so we don't overflow
                if (_splineDolly.CameraPosition > 1f) _splineDolly.CameraPosition = 1f;

                while (_currentEventIndex < _eventPoints.Length &&
                       _splineDolly.CameraPosition >= _eventPoints[_currentEventIndex].splinePosition)
                {
                    await HandleEventPoint(_eventPoints[_currentEventIndex]);
                }

                // if we've reached the end and not looping, break
                if (!_loop && _splineDolly.CameraPosition >= 1f)
                    break;
            }

            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }

    private async UniTask HandleEventPoint(RuntimeSplineEvent point)
    {
        _isPaused = true;

        // If the runtime event carries a camera reference, try to cast and activate it
        CinemachineCamera eventCam = null;
        int eventPriority = 20; // default priority
        if (point.eventCameraReference != null)
        {
            eventCam = point.eventCameraReference as CinemachineCamera;
            // If the asset stored was missing a priority field, we keep default. ScriptableObject has eventCameraPriority already
            // (we pass priority during conversion by using the asset; runtime struct keeps only reference — so we retrieve from asset)
            // To keep it simple/safe: try to find matching asset in _eventAssets by position and use its priority.
            for (int i = 0; i < _eventAssets.Length; i++)
            {
                if (Mathf.Approximately(_eventAssets[i].splinePosition, point.splinePosition))
                {
                    eventPriority = _eventAssets[i].eventCameraPriority;
                    break;
                }
            }
        }

        // Activate event camera (if provided)
        if (eventCam != null)
            CameraSwitcher.ActivateCamera(eventCam, eventPriority);

        // Invoke gameplay events
        point.onReachedEvent?.Invoke();

        try
        {
            // Pause for the specified duration (non-blocking)
            await UniTask.WaitForSeconds(point.pauseDuration, cancellationToken: _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // canceled — nothing else to do
        }

        // After pause, revert to dolly camera (so movement viewpoint returns to rail)
        if (_dollyVirtualCamera != null)
            CameraSwitcher.RevertTo(_dollyVirtualCamera, _dollyVirtualCamera.Priority);

        _currentEventIndex++;
        _isPaused = false;
    }

    private void ResetPath()
    {
        _currentEventIndex = 0;
        if (_splineDolly != null)
            _splineDolly.CameraPosition = 0f;
    }

    private void OnValidate()
    {
        if (_dollyVirtualCamera != null && _splineDolly == null)
            _splineDolly = _dollyVirtualCamera.GetComponent<CinemachineSplineDolly>();
    }

    // Public API
    public void PauseMovement() => _isPaused = true;
    public void ResumeMovement() => _isPaused = false;
    public void SetSpeed(float newSpeed) => _speed = newSpeed;
}
