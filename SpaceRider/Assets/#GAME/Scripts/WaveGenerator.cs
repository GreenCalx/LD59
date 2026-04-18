using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Produces wave samples that propagate along the ribbon.
///
/// The generator spawns new samples at the front (local Z = +LookAhead) using the
/// current (smoothed) signal parameters, and culls samples that have scrolled past
/// the back (local Z &lt; -DecayLength). Each sample's shape is frozen at spawn,
/// so changing amplitude / frequency / pan affects only newly-spawned waves — the
/// existing ribbon keeps its geometry and scrolls past unchanged.
///
/// Pan accumulates into a lateral X offset per sample (integration), giving the
/// ribbon a persistent steering trajectory. A first-order low-pass in virtualZ
/// smooths parameter changes over <see cref="paramSmoothingDistance"/>.
/// </summary>
[DefaultExecutionOrder(-90)]
public class WaveGenerator : MonoBehaviour
{
    [Header("Signal Parameters (targets — smoothed into new samples)")]
    [SerializeField] private float amplitude = 1f;
    [SerializeField] private float frequency = 1f;
    [SerializeField, Range(-1f, 1f)] private float pan = 0f;
    [Tooltip("Lateral deflection per unit of virtualZ per unit of pan. " +
             "The ribbon integrates pan over distance, so steering is persistent.")]
    [SerializeField] private float panLateralScale = 0.2f;

    [Header("Timing")]
    [SerializeField] private float bpm = 120f;

    [Header("Propagation")]
    [Tooltip("Samples stored per unit of virtualZ. Higher = smoother ribbon, more memory.")]
    [SerializeField, Min(1f)] private float sampleDensity = 4f;
    [Tooltip("VirtualZ distance over which parameter changes ramp toward the new target. " +
             "0 = instantaneous (sharp steps), higher values = smoother transitions.")]
    [SerializeField, Min(0f)] private float paramSmoothingDistance = 2f;

    [Header("References")]
    [SerializeField] private LevelScope levelScope;

    private struct WaveSample
    {
        public float virtualZ;
        public float x;
        public float y;
    }

    private readonly List<WaveSample> _samples = new List<WaveSample>();
    private float _phase;
    private float _smAmp;
    private float _smFreq;
    private float _smPan;
    private bool  _initialized;

    public float Amplitude              { get => amplitude;               set => amplitude = value; }
    public float Frequency              { get => frequency;               set => frequency = value; }
    public float Pan                    { get => pan;                     set => pan = Mathf.Clamp(value, -1f, 1f); }
    public float PanLateralScale        { get => panLateralScale;         set => panLateralScale = value; }
    public float Bpm                    { get => bpm;                     set => bpm = Mathf.Max(0f, value); }
    public float SampleDensity          { get => sampleDensity;           set => sampleDensity = Mathf.Max(1f, value); }
    public float ParamSmoothingDistance { get => paramSmoothingDistance;  set => paramSmoothingDistance = Mathf.Max(0f, value); }

    public void SetLevelScope(LevelScope scope) { levelScope = scope; _initialized = false; }

    private void OnDisable() => _initialized = false;

    private void Update()
    {
        if (!Application.isPlaying) return;
        Tick(Time.deltaTime);
    }

    /// <summary>Advances phase and the sample buffer by dt seconds. Exposed for tests and custom loops.</summary>
    public void Tick(float dt)
    {
        _phase = (_phase + bpm / 60f * Mathf.PI * 2f * dt) % (Mathf.PI * 2f);
        if (levelScope == null) return;

        EnsureInitialized();
        CullBehind();
        SpawnAhead();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        _smAmp  = amplitude;
        _smFreq = frequency;
        _smPan  = pan;
        _samples.Clear();

        float spacing = 1f / sampleDensity;
        float back    = VirtualDistance - DecayLength;
        float front   = VirtualDistance + LookAhead;

        // Start x so the ribbon passes through x=0 at the hero (virtualZ = VirtualDistance).
        float x = -_smPan * panLateralScale * DecayLength;

        for (float vz = back; vz <= front + 1e-4f; vz += spacing)
        {
            float y = _smAmp * Mathf.Sin(vz * Mathf.PI * 2f * _smFreq - _phase);
            _samples.Add(new WaveSample { virtualZ = vz, x = x, y = y });
            x += _smPan * panLateralScale * spacing;
        }
        _initialized = true;
    }

    private void CullBehind()
    {
        float backVZ = VirtualDistance - DecayLength;
        int cull = 0;
        while (cull + 1 < _samples.Count && _samples[cull + 1].virtualZ < backVZ) cull++;
        if (cull > 0) _samples.RemoveRange(0, cull);
    }

    private void SpawnAhead()
    {
        if (_samples.Count == 0) return;

        float frontVZ = VirtualDistance + LookAhead;
        float spacing = 1f / sampleDensity;
        float alpha   = paramSmoothingDistance > 0f
            ? 1f - Mathf.Exp(-spacing / paramSmoothingDistance)
            : 1f;

        WaveSample last = _samples[_samples.Count - 1];
        while (last.virtualZ + spacing <= frontVZ + 1e-4f)
        {
            _smAmp  += (amplitude - _smAmp)  * alpha;
            _smFreq += (frequency - _smFreq) * alpha;
            _smPan  += (pan       - _smPan)  * alpha;

            float nextVZ = last.virtualZ + spacing;
            float y = _smAmp * Mathf.Sin(nextVZ * Mathf.PI * 2f * _smFreq - _phase);
            float x = last.x + _smPan * panLateralScale * spacing;
            last = new WaveSample { virtualZ = nextVZ, x = x, y = y };
            _samples.Add(last);
        }
    }

    /// <summary>
    /// Samples evenly-spaced wave-spine positions across the local window
    /// [-DecayLength, +LookAhead] by interpolating the propagating sample buffer.
    /// Caller owns the returned NativeArray.
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

    /// <summary>Interpolates a ribbon position at a given local Z from the propagating buffer.</summary>
    public Vector3 SampleAtLocalZ(float localZ)
    {
        EnsureInitialized();
        if (_samples.Count == 0) return new Vector3(0f, 0f, localZ);

        float virtualZ = localZ + VirtualDistance;

        if (virtualZ <= _samples[0].virtualZ)
        {
            var s0 = _samples[0];
            return new Vector3(s0.x, s0.y, localZ);
        }
        if (virtualZ >= _samples[_samples.Count - 1].virtualZ)
        {
            var sl = _samples[_samples.Count - 1];
            return new Vector3(sl.x, sl.y, localZ);
        }

        int lo = 0, hi = _samples.Count - 1;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) >> 1;
            if (_samples[mid].virtualZ <= virtualZ) lo = mid;
            else hi = mid;
        }
        var a = _samples[lo];
        var b = _samples[hi];
        float span = b.virtualZ - a.virtualZ;
        float t2   = span > 1e-6f ? (virtualZ - a.virtualZ) / span : 0f;
        return new Vector3(
            Mathf.Lerp(a.x, b.x, t2),
            Mathf.Lerp(a.y, b.y, t2),
            localZ);
    }

    /// <summary>Vertical slope ∂y/∂virtualZ at the hero, computed as a finite difference on the buffer.</summary>
    public float SampleDerivativeAtHero()
    {
        float h = 1f / Mathf.Max(1f, sampleDensity);
        return (SampleAtLocalZ(+h).y - SampleAtLocalZ(-h).y) / (2f * h);
    }

    /// <summary>
    /// Lateral slope of the ribbon at the hero, normalized back into pan units [-1, 1].
    /// Represents the "felt" pan: what the ribbon is actually doing under the hero's feet,
    /// regardless of current input. Use this for banking feedback.
    /// </summary>
    public float GetEffectivePanAtHero()
    {
        if (panLateralScale <= 1e-6f) return 0f;
        float h = 1f / Mathf.Max(1f, sampleDensity);
        float xSlope = (SampleAtLocalZ(+h).x - SampleAtLocalZ(-h).x) / (2f * h);
        return Mathf.Clamp(xSlope / panLateralScale, -1f, 1f);
    }

    private float VirtualDistance => levelScope != null ? levelScope.VirtualDistance : 0f;
    private float LookAhead       => levelScope != null ? levelScope.LookAhead       : 30f;
    private float DecayLength     => levelScope != null ? levelScope.DecayLength     : 5f;
}
