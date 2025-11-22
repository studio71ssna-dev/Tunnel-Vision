using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class RuntimeSplineEvent
{
    [Tooltip("Normalized position along the spline (0-1)")]
    [Range(0f, 1f)] public float splinePosition;

    [Tooltip("Pause duration in seconds (how long gameplay/rail movement should pause)")]
    public float pauseDuration = 1f;

    [Tooltip("Event invoked when this point is reached (gameplay effects, spawns, sound, etc.)")]
    public UnityEvent onReachedEvent;

    // Optional reference to a virtual camera to switch to when triggered (can be null)
    public UnityEngine.Object eventCameraReference; // holds a reference to the asset (converted at runtime)
}
