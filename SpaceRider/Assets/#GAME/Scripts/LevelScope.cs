using UnityEngine;

/// <summary>
/// Progress + window authority for the treadmill-mode level.
/// No longer owns world geometry — everything it describes is virtual or local.
/// </summary>
public class LevelScope : MonoBehaviour
{
    [Header("Level")]
    [SerializeField, Min(0f)] private float levelLength = 100f;

    [Header("Ribbon Window (local Z around hero)")]
    [SerializeField, Min(0f)] private float lookAhead   = 30f;
    [SerializeField, Min(0f)] private float decayLength = 5f;

    [Header("Runtime (written by ProgressDriver)")]
    [SerializeField] private float virtualDistance;
    [SerializeField] private float scrollSpeed;

    /// <summary>Total virtual distance to the finish line. Unit: meters.</summary>
    public float LevelLength { get => levelLength; set => levelLength = Mathf.Max(0f, value); }

    /// <summary>How far in front of the hero the ribbon extends, in local Z.</summary>
    public float LookAhead { get => lookAhead; set => lookAhead = Mathf.Max(0f, value); }

    /// <summary>How far behind the hero the ribbon extends (fade zone), in local Z.</summary>
    public float DecayLength { get => decayLength; set => decayLength = Mathf.Max(0f, value); }

    /// <summary>Current progress along the virtual level. Advanced by ProgressDriver.</summary>
    public float VirtualDistance
    {
        get => virtualDistance;
        set => virtualDistance = Mathf.Clamp(value, 0f, levelLength);
    }

    /// <summary>Current treadmill scroll speed in meters per second. Written by ProgressDriver.</summary>
    public float ScrollSpeed { get => scrollSpeed; set => scrollSpeed = value; }

    /// <summary>0 at start, 1 at the finish line. Returns 1 if LevelLength is 0.</summary>
    public float Progress01 =>
        levelLength <= 0f ? 1f : Mathf.Clamp01(virtualDistance / levelLength);

    /// <summary>True once VirtualDistance reaches or exceeds LevelLength.</summary>
    public bool IsFinished => virtualDistance >= levelLength;
}
