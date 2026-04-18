using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting.YamlDotNet.Core;
using UnityEngine;

[DefaultExecutionOrder(-90)]
public class WaveGenerator : MonoBehaviour
{
    [Header("Signal Parameters (runtime, driven by WaveInputController)")]
    [SerializeField, Range(0, Constants.INTEGER_RANGE)] private int amplitude = Constants.INTEGER_RANGE / 2;
    [SerializeField, Range(0, Constants.INTEGER_RANGE)] private int frequency = Constants.INTEGER_RANGE / 2;
    [SerializeField, Range(0, Constants.INTEGER_RANGE)] private int pan = Constants.INTEGER_RANGE / 2;

    [Header("References")]
    [SerializeField] private LevelScope levelScope;
    [SerializeField] private GameConfig config;

    private struct WaveSample { public float virtualZ, x, y; }

    private readonly List<WaveSample> _samples = new List<WaveSample>();
    private float _phase;
    private float _smAmp, _smFreq, _smPan;
    private bool  _initialized;

    public int Amplitude { get => amplitude; set => amplitude = Mathf.Clamp(value, 0, Constants.INTEGER_RANGE); }
    public int Frequency { get => frequency; set => frequency = Mathf.Clamp(value, 0, Constants.INTEGER_RANGE); }
    public int Pan       { get => pan;       set => pan = Mathf.Clamp(value, 0, Constants.INTEGER_RANGE); }

    public float amplitude_min = 0.1f;
    public float amplitude_max = 10f;
    public float frequency_min = 0.1f;
    public float frequency_max = 1f;

    private float PanLateralScale        => config?.waveGenerator?.panLateralScale        ?? 0.2f;
    private float Bpm                    => config?.waveGenerator?.bpm                    ?? 120f;
    private float SampleDensity          => config?.waveGenerator?.sampleDensity          ?? 4f;
    private float ParamSmoothingDistance => config?.waveGenerator?.paramSmoothingDistance ?? 2f;
    private float LookAhead              => config?.level?.lookAhead                      ?? 30f;
    private float DecayLength            => config?.level?.decayLength                    ?? 5f;
    private float EstimatedSpeed         => config?.progress?.baseScrollSpeed             ?? 10f;
    private float VirtualDistance        => levelScope != null ? levelScope.VirtualDistance : 0f;

    public void SetLevelScope(LevelScope scope) { levelScope = scope; _initialized = false; }
    public void SetConfig(GameConfig c)         { config = c;         _initialized = false; }
    public FMODUnity.StudioEventEmitter bgm_emitter;

    private void OnDisable() => _initialized = false;


    private void Update()
    {
        if (!Application.isPlaying) return;
        Tick(Time.deltaTime);

        float bgm_pan = (float)pan / Constants.INTEGER_RANGE;
        if (bgm_emitter != null)
        {
            bgm_emitter.SetParameter("Pan", bgm_pan);
        }
    }

    public float Mapped_frequency()
    {
        return (float)frequency / Constants.INTEGER_RANGE * (frequency_max - frequency_min) + frequency_min;
    }

    public float Mapped_amplitude()
    {
        return (float)amplitude / Constants.INTEGER_RANGE * (amplitude_max - amplitude_min) + amplitude_min;
    }

    public float Mapped_pan()
    {
        return (float)pan / Constants.INTEGER_RANGE * 2f - 1f;
    }

    public void Tick(float dt)
    {
        _phase = (_phase + Mapped_frequency() * Bpm / 60f * Mathf.PI * 2f * dt) % (Mathf.PI * 2f);
        if (levelScope == null) return;
        EnsureInitialized();
        CullBehind();
        SpawnAhead();
        SyncTransformToFront();
    }

    private void SyncTransformToFront()
    {
        if (_samples.Count == 0) return;
        var front = _samples[_samples.Count - 1];
        transform.localPosition = new Vector3(front.x, front.y, front.virtualZ - VirtualDistance);
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _smAmp = Mapped_amplitude(); _smFreq = Mapped_frequency(); _smPan = Mapped_pan();
        _samples.Clear();

        float spacing = 1f / SampleDensity;
        float back    = VirtualDistance - DecayLength;
        float front   = VirtualDistance + LookAhead;
        float x       = -_smPan * PanLateralScale * DecayLength;

        // Backtrack phase: samples nearer the front were emitted more recently.
        // A sample at virtualZ vz was emitted (front - vz) virtual-distance units ago,
        // which at EstimatedSpeed is (front - vz) / speed seconds ago.
        float speed   = Mathf.Max(0.1f, EstimatedSpeed);
        float omega   = _smFreq * Bpm / 60f * Mathf.PI * 2f;
        for (float vz = back; vz <= front + 1e-4f; vz += spacing)
        {
            float phaseAtEmission = _phase - (front - vz) / speed * omega;
            float y = _smAmp * Mathf.Sin(phaseAtEmission);
            _samples.Add(new WaveSample { virtualZ = vz, x = x, y = y });
            x += _smPan * PanLateralScale * spacing;
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
        float spacing = 1f / SampleDensity;
        float alpha   = ParamSmoothingDistance > 0f
            ? 1f - Mathf.Exp(-spacing / ParamSmoothingDistance) : 1f;

        WaveSample last = _samples[_samples.Count - 1];
        while (last.virtualZ + spacing <= frontVZ + 1e-4f)
        {
            _smAmp  += (Mapped_amplitude() - _smAmp)  * alpha;
            _smFreq += (Mapped_frequency() - _smFreq) * alpha;
            _smPan  += (Mapped_pan() - _smPan)  * alpha;

            float nextVZ = last.virtualZ + spacing;
            float y = _smAmp * Mathf.Sin(_phase);
            float x = last.x + _smPan * PanLateralScale * spacing;
            last = new WaveSample { virtualZ = nextVZ, x = x, y = y };
            _samples.Add(last);
        }
    }

    public NativeArray<Vector3> GetWavePoints(int resolution, Allocator allocator)
    {
        var points = new NativeArray<Vector3>(resolution, allocator, NativeArrayOptions.UninitializedMemory);
        float zMin = -DecayLength, zMax = LookAhead;
        for (int i = 0; i < resolution; i++)
        {
            float t = resolution > 1 ? i / (resolution - 1f) : 0f;
            points[i] = SampleAtLocalZ(Mathf.Lerp(zMin, zMax, t));
        }
        return points;
    }

    public Vector3 SampleAtLocalZ(float localZ)
    {
        EnsureInitialized();
        if (_samples.Count == 0) return new Vector3(0f, 0f, localZ);

        float vz = localZ + VirtualDistance;
        if (vz <= _samples[0].virtualZ)              { var s = _samples[0]; return new Vector3(s.x, s.y, localZ); }
        if (vz >= _samples[_samples.Count-1].virtualZ) { var s = _samples[_samples.Count-1]; return new Vector3(s.x, s.y, localZ); }

        int lo = 0, hi = _samples.Count - 1;
        while (hi - lo > 1) { int mid = (lo + hi) >> 1; if (_samples[mid].virtualZ <= vz) lo = mid; else hi = mid; }
        var a = _samples[lo]; var b = _samples[hi];
        float span = b.virtualZ - a.virtualZ;
        float t2   = span > 1e-6f ? (vz - a.virtualZ) / span : 0f;
        return new Vector3(Mathf.Lerp(a.x, b.x, t2), Mathf.Lerp(a.y, b.y, t2), localZ);
    }

    public float SampleDerivativeAtHero()
    {
        float h = 1f / Mathf.Max(1f, SampleDensity);
        return (SampleAtLocalZ(+h).y - SampleAtLocalZ(-h).y) / (2f * h);
    }

    public float GetEffectivePanAtHero()
    {
        if (PanLateralScale <= 1e-6f) return 0f;
        float h = 1f / Mathf.Max(1f, SampleDensity);
        float xSlope = (SampleAtLocalZ(+h).x - SampleAtLocalZ(-h).x) / (2f * h);
        return Mathf.Clamp(xSlope / PanLateralScale, -1f, 1f);
    }

    private void OnDrawGizmos()
    {
        if (!GameDebug.ShowGizmos) return;

        Vector3 origin = transform.parent != null ? transform.parent.position : Vector3.zero;
        float   vd     = VirtualDistance;

        if (_initialized)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _samples.Count - 1; i++)
            {
                var s0 = _samples[i]; var s1 = _samples[i + 1];
                Gizmos.DrawLine(
                    origin + new Vector3(s0.x, s0.y, s0.virtualZ - vd),
                    origin + new Vector3(s1.x, s1.y, s1.virtualZ - vd));
            }
        }

        // Generator always shown at its synced world position (ribbon front)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Decay boundary is fixed relative to HeroBundle origin
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin - Vector3.forward * DecayLength, 0.5f);
    }
}
