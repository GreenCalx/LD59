using Unity.Collections;
using UnityEngine;

/// <summary>
/// Black-box wave generator. Produces wave-spine positions in a local window
/// around the hero (local Z ∈ [-DecayLength, +LookAhead]) while the spatial
/// pattern scrolls past at rate LevelScope.VirtualDistance.
/// </summary>
[DefaultExecutionOrder(-90)]
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

    private float _phase;

    public float Amplitude { get => amplitude; set => amplitude = value; }
    public float Frequency { get => frequency; set => frequency = value; }
    public float Pan       { get => pan;       set => pan = Mathf.Clamp(value, -1f, 1f); }
    public float Bpm       { get => bpm;       set => bpm = Mathf.Max(0f, value); }

    public void SetLevelScope(LevelScope scope) => levelScope = scope;

    private void Update()
    {
        _phase = (_phase + bpm / 60f * Mathf.PI * 2f * Time.deltaTime) % (Mathf.PI * 2f);
    }

    /// <summary>
    /// Samples evenly-spaced wave-spine positions across the local window
    /// [-DecayLength, +LookAhead]. Caller owns the returned NativeArray.
    /// </summary>
    public NativeArray<Vector3> GetWavePoints(int resolution, Allocator allocator)
    {
        var points = new NativeArray<Vector3>(resolution, allocator, NativeArrayOptions.UninitializedMemory);
        float zMin = -DecayLength;
        float zMax = +LookAhead;

        for (int i = 0; i < resolution; i++)
        {
            float t = resolution > 1 ? i / (resolution - 1f) : 0f;
            float z = Mathf.Lerp(zMin, zMax, t);
            points[i] = SampleAtLocalZ(z);
        }

        return points;
    }

    /// <summary>
    /// Samples the wave at a given local Z (relative to the hero).
    /// The spatial pattern scrolls as VirtualDistance advances.
    /// </summary>
    public Vector3 SampleAtLocalZ(float localZ)
    {
        float virtualZ = localZ + VirtualDistance;
        float theta    = virtualZ * frequency * Mathf.PI * 2f - _phase;
        float sinVal   = Mathf.Sin(theta);
        return new Vector3(pan * sinVal * amplitude, sinVal * amplitude, localZ);
    }

    /// <summary>
    /// ∂y/∂virtualZ at the hero (local Z = 0). Used by ProgressDriver as the signal modifier.
    /// </summary>
    public float SampleDerivativeAtHero()
    {
        float theta = VirtualDistance * frequency * Mathf.PI * 2f - _phase;
        return Mathf.Cos(theta) * frequency * Mathf.PI * 2f * amplitude;
    }

    private float VirtualDistance => levelScope != null ? levelScope.VirtualDistance : 0f;
    private float LookAhead       => levelScope != null ? levelScope.LookAhead       : 30f;
    private float DecayLength     => levelScope != null ? levelScope.DecayLength     : 5f;
}
