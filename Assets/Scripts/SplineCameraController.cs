using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(CinemachineSplineDolly))]
public class SplineCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SplineContainer _splinePath;
    [SerializeField] private CinemachineCamera _virtualCamera;

    [Header("Event Points (Scene-based)")]
    [SerializeField] private RuntimeSplineEvent[] _eventPoints;

    [Header("Movement Settings")]
    [SerializeField] private float _speed = 0.1f;
    [SerializeField] private bool _loop = true;
    [SerializeField] private bool _autoStart = true;

    private CinemachineSplineDolly _splineDolly;
    private int _currentEventIndex = 0;
    private bool _isPaused = false;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        _splineDolly = _virtualCamera.GetComponent<CinemachineSplineDolly>();
        _splineDolly.Spline = _splinePath;
         SortEventPoints();
    }

    private void OnEnable()
    {
        if (_autoStart) StartCameraMovement().Forget();
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

    private async UniTaskVoid StartCameraMovement()
    {
        _cts = new CancellationTokenSource();
        await UniTask.Yield(PlayerLoopTiming.Update);

        while (!_cts.IsCancellationRequested)
        {
            if (_currentEventIndex >= _eventPoints.Length)
            {
                if (_loop) ResetPath();
                else break;
            }

            if (!_isPaused)
            {
                _splineDolly.CameraPosition += _speed * Time.deltaTime;

                while (_currentEventIndex < _eventPoints.Length &&
                       _splineDolly.CameraPosition >= _eventPoints[_currentEventIndex].splinePosition)
                {
                    await HandleEventPoint(_eventPoints[_currentEventIndex]);
                }
            }

            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }

    private async UniTask HandleEventPoint(RuntimeSplineEvent point)
    {
        _isPaused = true;
        point.onReachedEvent?.Invoke();

        try
        {
            await UniTask.WaitForSeconds(point.pauseDuration, cancellationToken: _cts.Token);
            _currentEventIndex++;
            _isPaused = false;
        }
        catch (OperationCanceledException) { }
    }

    private void ResetPath()
    {
        _currentEventIndex = 0;
        _splineDolly.CameraPosition = 0f;
    }

    private void OnValidate()
    {
        if (_virtualCamera != null && _splineDolly == null)
            _splineDolly = _virtualCamera.GetComponent<CinemachineSplineDolly>();
    }

    // Public controls
    public void Pause() => _isPaused = true;
    public void Resume() => _isPaused = false;
    public void SetSpeed(float newSpeed) => _speed = newSpeed;
}
