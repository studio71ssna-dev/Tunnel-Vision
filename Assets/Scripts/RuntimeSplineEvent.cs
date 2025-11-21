using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class RuntimeSplineEvent
{
    [Tooltip("Normalized position along the spline (0-1)")]
    [Range(0f, 1f)] public float splinePosition;

    [Tooltip("Pause duration in seconds")]
    public float pauseDuration = 1f;

    [Tooltip("UnityEvent triggered when this point is reached")]
    public UnityEvent onReachedEvent;
}
