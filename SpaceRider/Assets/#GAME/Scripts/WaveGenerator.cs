using Unity.Collections;
using UnityEngine;

/// <summary>
/// Black-box wave generator. Owns the signal parameters exposed to the player
/// (amplitude, frequency, pan) and produces the wave spine as a flat array of
/// world-space positions. The ribbon/spline system consumes this output.
/// </summary>
public class WaveGenerator : MonoBehaviour
{
    [Header("Signal Parameters")]
    [SerializeField] private float amplitude = 1f;
    [SerializeField] private float frequency = 1f;
    [SerializeField, Range(-1f, 1f)] private float pan = 0f;

    [Header("Timing")]
    [SerializeField] private float bpm = 120f;

    [Header("References")]
    [SerializeField] private LevelScope levelScope;

    private float _phase; // radians, advances each frame

    /// <summary>Vertical scale of the wave oscillation.</summary>
    public float Amplitude  { get => amplitude;  set => amplitude = value; }
    /// <summary>Oscillation cycles per unit of level length.</summary>
    public float Frequency  { get => frequency;  set => frequency = value; }
    /// <summary>Lateral bias [-1, 1] that tilts the ribbon left/right (pan).</summary>
    public float Pan        { get => pan;         set => pan = Mathf.Clamp(value, -1f, 1f); }
    /// <summary>Total length of the level along the Z axis, read from <see cref="LevelScope"/>.</summary>
    public float LevelLength => levelScope != null ? levelScope.LevelLength : 100f;
    /// <summary>Tempo in beats per minute. One beat advances the wave by one full cycle.</summary>
    public float Bpm        { get => bpm;         set => bpm = Mathf.Max(0f, value); }

    private void Update()
    {
        // Advance phase: one beat = one full cycle (2π). Wrap to avoid float drift.
        _phase = (_phase + bpm / 60f * Mathf.PI * 2f * Time.deltaTime) % (Mathf.PI * 2f);
    }

    /// <summary>
    /// Computes and returns <paramref name="resolution"/> evenly-spaced wave-spine positions
    /// in world space, sampled from Z = 0 to Z = <see cref="LevelLength"/>.
    /// <para>
    /// The caller owns the returned <see cref="NativeArray{T}"/> and is responsible
    /// for calling <c>Dispose()</c> on it.
    /// </para>
    /// </summary>
    /// <param name="resolution">Number of sample points along the wave.</param>
    /// <param name="allocator">
    /// Memory allocator for the array. Use <see cref="Allocator.Temp"/> for single-frame
    /// reads, <see cref="Allocator.TempJob"/> when scheduling a job, or
    /// <see cref="Allocator.Persistent"/> to keep the array alive across frames.
    /// </param>
    public NativeArray<Vector3> GetWavePoints(int resolution, Allocator allocator)
    {
        var points = new NativeArray<Vector3>(resolution, allocator, NativeArrayOptions.UninitializedMemory);

        // Default: sine wave.
        // TODO: replace with the proper signal wave computation.
        //
        // Contract each element must satisfy:
        //   points[i].z  = i / (resolution - 1f) * levelLength   — forward position along the level
        //   points[i].x  = lateral displacement driven by pan and wave shape
        //   points[i].y  = vertical displacement driven by amplitude and wave shape
        //
        // The hero's acceleration is derived from the second derivative of Y w.r.t. Z,
        // so wave continuity matters (prefer smooth functions, not piecewise linear).
        for (int i = 0; i < resolution; i++)
        {
            float t      = resolution > 1 ? i / (resolution - 1f) : 0f;
            float z      = t * LevelLength;
            float theta  = t * frequency * Mathf.PI * 2f - _phase;
            float sinVal = Mathf.Sin(theta);

            // Pan rotates the oscillation plane: at pan=0 it's purely vertical (Y),
            // at pan=±1 it's purely lateral (X).
            points[i] = new Vector3(pan * sinVal * amplitude, sinVal * amplitude, z);
        }

        return points;
    }
}
