using UnityEngine;
using UnityEngine.Events;
using Unity.Cinemachine;

[CreateAssetMenu(fileName = "SplineEventPoint", menuName = "RailShooter/Spline Event Point", order = 0)]
public class SplineEventPoint : ScriptableObject
{
    [Header("Trigger")]
    [Tooltip("Normalized position along the spline (0 to 1)")]
    [Range(0f, 1f)] public float splinePosition = 0.5f;

    [Tooltip("How long to pause movement/gameplay when this triggers (seconds)")]
    public float pauseDuration = 1f;

    [Tooltip("If not empty, this virtual camera will be activated (priority raised) when this event triggers")]
    public CinemachineCamera eventCamera;

    [Tooltip("Priority to assign to eventCamera when activated (higher wins). Default 20.")]
    public int eventCameraPriority = 20;

    [Tooltip("Invoke these gameplay events when this event point is reached")]
    public UnityEvent onReachedEvent;

    // convert ScriptableObject -> runtime structure
    public RuntimeSplineEvent ToRuntime()
    {
        var r = new RuntimeSplineEvent()
        {
            splinePosition = splinePosition,
            pauseDuration = pauseDuration,
            onReachedEvent = onReachedEvent,
            eventCameraReference = eventCamera
        };
        return r;
    }
}
