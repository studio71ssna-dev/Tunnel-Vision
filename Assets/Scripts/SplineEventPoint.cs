using UnityEngine;

[CreateAssetMenu(fileName = "SplineEventPoint", menuName = "Cinemachine/Spline Event Point")]
public class SplineEventPoint : ScriptableObject
{
    [Tooltip("Normalized position along the spline (0-1)")]
    [Range(0, 1)] public float splinePosition;

    [Tooltip("Pause duration in seconds")]
    public float pauseDuration = 1f;

    public UnityEngine.Events.UnityEvent onReachedEvent;
}
