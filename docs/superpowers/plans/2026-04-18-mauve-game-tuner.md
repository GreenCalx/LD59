# MAUVE Game Tuner Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Centralize all game tuning variables into swappable ScriptableObject assets exposed through a MAUVE EditorWindow with debug gizmos toggle.

**Architecture:** Root `GameConfig` SO references six sub-config SOs (WaveInput, WaveGenerator, Level, Progress, Camera, Surfer). Each game script drops its individual serialized fields and reads from `config.*.*` directly. A static `GameDebug.ShowGizmos` bool (toggled in the MAUVE window) gates `OnDrawGizmos` in each script.

**Tech Stack:** Unity 6, C#, UnityEditor API, ScriptableObject, EditorWindow, Gizmos/Handles.

---

## Task 1: Create Config ScriptableObject Classes

**Files:**
- Create: `Assets/#GAME/Scripts/Config/GameConfig.cs`
- Create: `Assets/#GAME/Scripts/Config/WaveInputConfig.cs`
- Create: `Assets/#GAME/Scripts/Config/WaveGeneratorConfig.cs`
- Create: `Assets/#GAME/Scripts/Config/LevelConfig.cs`
- Create: `Assets/#GAME/Scripts/Config/ProgressConfig.cs`
- Create: `Assets/#GAME/Scripts/Config/CameraConfig.cs`
- Create: `Assets/#GAME/Scripts/Config/SurferConfig.cs`
- Create: `Assets/#GAME/Scripts/GameDebug.cs`

- [ ] **Step 1: Create `GameConfig.cs`**

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "MAUVE/Game Config")]
public class GameConfig : ScriptableObject
{
    public WaveInputConfig     waveInput;
    public WaveGeneratorConfig waveGenerator;
    public LevelConfig         level;
    public ProgressConfig      progress;
    public CameraConfig        camera;
    public SurferConfig        surfer;
}
```

- [ ] **Step 2: Create `WaveInputConfig.cs`**

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "WaveInputConfig", menuName = "MAUVE/Wave Input Config")]
public class WaveInputConfig : ScriptableObject
{
    [Header("Frequency")]
    public float freqMin       = 0.1f;
    public float freqMax       = 5f;
    public float freqInitial   = 1f;
    public float frequencyRate = 0.5f;

    [Header("Amplitude")]
    public float ampMin        = 0f;
    public float ampMax        = 5f;
    public float ampInitial    = 1f;
    public float amplitudeRate = 1f;

    [Header("Pan")]
    public float panInitial    = 0f;
    public float panRate       = 2f;
}
```

- [ ] **Step 3: Create `WaveGeneratorConfig.cs`**

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "WaveGeneratorConfig", menuName = "MAUVE/Wave Generator Config")]
public class WaveGeneratorConfig : ScriptableObject
{
    [Min(1f)] public float sampleDensity          = 4f;
    [Min(0f)] public float paramSmoothingDistance = 2f;
    public float panLateralScale = 0.2f;
    public float bpm             = 120f;
}
```

- [ ] **Step 4: Create `LevelConfig.cs`**

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "MAUVE/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Min(0f)] public float levelLength     = 1000f;
    [Min(0f)] public float lookAhead       = 30f;
    [Min(0f)] public float decayLength     = 5f;
    [Min(0f)] public float playfieldRadius = 8f;
}
```

- [ ] **Step 5: Create `ProgressConfig.cs`**

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "ProgressConfig", menuName = "MAUVE/Progress Config")]
public class ProgressConfig : ScriptableObject
{
    public float baseScrollSpeed    = 10f;
    public float signalGain         = 0.05f;
    public float minSpeedMultiplier = 0.5f;
    public float maxSpeedMultiplier = 2f;
}
```

- [ ] **Step 6: Create `CameraConfig.cs`**

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "CameraConfig", menuName = "MAUVE/Camera Config")]
public class CameraConfig : ScriptableObject
{
    [Header("Position")]
    public Vector3 offset             = new Vector3(0f, 2f, -8f);
    public float   positionSmoothTime = 0.18f;

    [Header("Rotation")]
    [Range(0f, 1f)] public float rollInfluence = 0.35f;
    public float lookSharpness = 8f;

    [Header("Field of View")]
    public float baseFov       = 60f;
    public float fovSpeedScale = 0.4f;
    public float fovSmoothTime = 0.25f;
}
```

- [ ] **Step 7: Create `SurferConfig.cs`**

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "SurferConfig", menuName = "MAUVE/Surfer Config")]
public class SurferConfig : ScriptableObject
{
    [Range(0f, 90f)] public float maxTiltDegrees = 45f;
    public bool alignToTangent = true;
}
```

- [ ] **Step 8: Create `GameDebug.cs`**

```csharp
public static class GameDebug
{
    public static bool ShowGizmos;
}
```

- [ ] **Step 9: Verify Unity compiles with no errors** — check Unity Editor Console for compile errors after saving all files.

---

## Task 2: Refactor LevelScope + Update LevelScopeTests

**Files:**
- Modify: `Assets/#GAME/Scripts/LevelScope.cs`
- Modify: `Assets/#GAME/Tests/LevelScopeTests.cs`

- [ ] **Step 1: Replace `LevelScope.cs`**

```csharp
using UnityEngine;

public class LevelScope : MonoBehaviour
{
    [SerializeField] private GameConfig config;

    [Header("Runtime (written by ProgressDriver)")]
    [SerializeField] private float virtualDistance;
    [SerializeField] private float scrollSpeed;

    public float LevelLength => config != null ? config.level.levelLength : 0f;
    public float LookAhead   => config != null ? config.level.lookAhead   : 30f;
    public float DecayLength => config != null ? config.level.decayLength : 5f;

    public float VirtualDistance
    {
        get => virtualDistance;
        set => virtualDistance = Mathf.Clamp(value, 0f, LevelLength > 0f ? LevelLength : float.MaxValue);
    }

    public float ScrollSpeed { get => scrollSpeed; set => scrollSpeed = value; }

    public float Progress01 =>
        LevelLength <= 0f ? 1f : Mathf.Clamp01(virtualDistance / LevelLength);

    public bool IsFinished => LevelLength > 0f && virtualDistance >= LevelLength;

    public void SetConfig(GameConfig c) { config = c; }

    private void OnDrawGizmos()
    {
        if (!GameDebug.ShowGizmos || config == null) return;
        float r = config.level.playfieldRadius;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        DrawWireCylinder(Vector3.zero, r, 80f);
    }

    private static void DrawWireCylinder(Vector3 center, float radius, float height)
    {
        const int steps = 32;
        float half = height * 0.5f;
        Vector3 top    = center + Vector3.up * half;
        Vector3 bottom = center - Vector3.up * half;
        Vector3 prev   = Vector3.right * radius;
        for (int i = 1; i <= steps; i++)
        {
            float angle = i / (float)steps * Mathf.PI * 2f;
            Vector3 curr = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(top + prev,    top + curr);
            Gizmos.DrawLine(bottom + prev, bottom + curr);
            if (i % 8 == 0) Gizmos.DrawLine(top + curr, bottom + curr);
            prev = curr;
        }
    }
}
```

- [ ] **Step 2: Replace `LevelScopeTests.cs`**

```csharp
using NUnit.Framework;
using UnityEngine;

public class LevelScopeTests
{
    private GameConfig MakeConfig(float length)
    {
        var lc = ScriptableObject.CreateInstance<LevelConfig>();
        lc.levelLength = length;
        lc.lookAhead   = 30f;
        lc.decayLength = 5f;
        var cfg = ScriptableObject.CreateInstance<GameConfig>();
        cfg.level = lc;
        return cfg;
    }

    private LevelScope MakeScope(float length)
    {
        var go    = new GameObject("LevelScope");
        var scope = go.AddComponent<LevelScope>();
        scope.SetConfig(MakeConfig(length));
        return scope;
    }

    [TearDown]
    public void Cleanup()
    {
        foreach (var go in Object.FindObjectsByType<LevelScope>(FindObjectsSortMode.None))
            Object.DestroyImmediate(go.gameObject);
    }

    [Test]
    public void Progress01_Is_Zero_At_Start()
    {
        var s = MakeScope(100f);
        Assert.AreEqual(0f, s.Progress01, 1e-6f);
    }

    [Test]
    public void Progress01_Is_Half_At_Midpoint()
    {
        var s = MakeScope(100f);
        s.VirtualDistance = 50f;
        Assert.AreEqual(0.5f, s.Progress01, 1e-6f);
    }

    [Test]
    public void Progress01_Clamps_At_One_When_Past_End()
    {
        var s = MakeScope(100f);
        s.VirtualDistance = 500f;
        Assert.AreEqual(1f, s.Progress01, 1e-6f);
    }

    [Test]
    public void Progress01_Is_One_When_LevelLength_Is_Zero()
    {
        var s = MakeScope(0f);
        Assert.AreEqual(1f, s.Progress01, 1e-6f);
    }

    [Test]
    public void IsFinished_True_When_VirtualDistance_Reaches_LevelLength()
    {
        var s = MakeScope(100f);
        s.VirtualDistance = 100f;
        Assert.IsTrue(s.IsFinished);
    }

    [Test]
    public void IsFinished_False_Before_End()
    {
        var s = MakeScope(100f);
        s.VirtualDistance = 99.99f;
        Assert.IsFalse(s.IsFinished);
    }
}
```

- [ ] **Step 3: Run LevelScope tests in Unity Test Runner**

Window → General → Test Runner → EditMode → Run All.
Expected: 6 tests pass.

---

## Task 3: Refactor WaveGenerator + Update WaveGeneratorTests

**Files:**
- Modify: `Assets/#GAME/Scripts/WaveGenerator.cs`
- Modify: `Assets/#GAME/Tests/WaveGeneratorTests.cs`

- [ ] **Step 1: Replace `WaveGenerator.cs`**

```csharp
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[DefaultExecutionOrder(-90)]
public class WaveGenerator : MonoBehaviour
{
    [Header("Signal Parameters (runtime, driven by WaveInputController)")]
    [SerializeField] private float amplitude = 1f;
    [SerializeField] private float frequency = 1f;
    [SerializeField, Range(-1f, 1f)] private float pan = 0f;

    [Header("References")]
    [SerializeField] private LevelScope levelScope;
    [SerializeField] private GameConfig config;

    private struct WaveSample { public float virtualZ, x, y; }

    private readonly List<WaveSample> _samples = new List<WaveSample>();
    private float _phase;
    private float _smAmp, _smFreq, _smPan;
    private bool  _initialized;

    public float Amplitude { get => amplitude; set => amplitude = value; }
    public float Frequency { get => frequency; set => frequency = value; }
    public float Pan       { get => pan;       set => pan = Mathf.Clamp(value, -1f, 1f); }

    private float PanLateralScale        => config != null ? config.waveGenerator.panLateralScale        : 0.2f;
    private float Bpm                    => config != null ? config.waveGenerator.bpm                    : 120f;
    private float SampleDensity          => config != null ? config.waveGenerator.sampleDensity          : 4f;
    private float ParamSmoothingDistance => config != null ? config.waveGenerator.paramSmoothingDistance : 2f;
    private float LookAhead              => config != null ? config.level.lookAhead                      : 30f;
    private float DecayLength            => config != null ? config.level.decayLength                   : 5f;
    private float VirtualDistance        => levelScope != null ? levelScope.VirtualDistance              : 0f;

    public void SetLevelScope(LevelScope scope) { levelScope = scope; _initialized = false; }
    public void SetConfig(GameConfig c)         { config = c;         _initialized = false; }

    private void OnDisable() => _initialized = false;

    private void Update()
    {
        if (!Application.isPlaying) return;
        Tick(Time.deltaTime);
    }

    public void Tick(float dt)
    {
        _phase = (_phase + Bpm / 60f * Mathf.PI * 2f * dt) % (Mathf.PI * 2f);
        if (levelScope == null) return;
        EnsureInitialized();
        CullBehind();
        SpawnAhead();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _smAmp = amplitude; _smFreq = frequency; _smPan = pan;
        _samples.Clear();

        float spacing = 1f / SampleDensity;
        float back    = VirtualDistance - DecayLength;
        float front   = VirtualDistance + LookAhead;
        float x       = -_smPan * PanLateralScale * DecayLength;

        for (float vz = back; vz <= front + 1e-4f; vz += spacing)
        {
            float y = _smAmp * Mathf.Sin(vz * Mathf.PI * 2f * _smFreq - _phase);
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
            _smAmp  += (amplitude - _smAmp)  * alpha;
            _smFreq += (frequency - _smFreq) * alpha;
            _smPan  += (pan       - _smPan)  * alpha;

            float nextVZ = last.virtualZ + spacing;
            float y = _smAmp * Mathf.Sin(nextVZ * Mathf.PI * 2f * _smFreq - _phase);
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

        if (_initialized)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _samples.Count - 1; i++)
            {
                var s0 = _samples[i]; var s1 = _samples[i + 1];
                Gizmos.DrawLine(
                    transform.position + new Vector3(s0.x, s0.y, s0.virtualZ - VirtualDistance),
                    transform.position + new Vector3(s1.x, s1.y, s1.virtualZ - VirtualDistance));
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.forward * LookAhead, 0.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position - Vector3.forward * DecayLength, 0.5f);
    }
}
```

- [ ] **Step 2: Replace `WaveGeneratorTests.cs`**

```csharp
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

public class WaveGeneratorTests
{
    private LevelScope    _scope;
    private WaveGenerator _gen;
    private GameConfig    _config;

    [SetUp]
    public void SetUp()
    {
        var lc = ScriptableObject.CreateInstance<LevelConfig>();
        lc.levelLength = 1000f; lc.lookAhead = 30f; lc.decayLength = 5f; lc.playfieldRadius = 8f;

        var wc = ScriptableObject.CreateInstance<WaveGeneratorConfig>();
        wc.sampleDensity = 4f; wc.paramSmoothingDistance = 2f; wc.panLateralScale = 0.2f; wc.bpm = 0f;

        _config = ScriptableObject.CreateInstance<GameConfig>();
        _config.level = lc; _config.waveGenerator = wc;

        var scopeGo = new GameObject("Scope");
        _scope = scopeGo.AddComponent<LevelScope>();
        _scope.SetConfig(_config);

        var genGo = new GameObject("Gen");
        _gen = genGo.AddComponent<WaveGenerator>();
        _gen.SetLevelScope(_scope);
        _gen.SetConfig(_config);
        _gen.Amplitude = 1f; _gen.Frequency = 1f; _gen.Pan = 0f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gen.gameObject);
        Object.DestroyImmediate(_scope.gameObject);
    }

    [Test]
    public void GetWavePoints_Spans_LocalZ_From_Negative_Decay_To_Plus_LookAhead()
    {
        var pts = _gen.GetWavePoints(32, Allocator.Temp);
        Assert.AreEqual(-_config.level.decayLength, pts[0].z, 1e-4f);
        Assert.AreEqual(+_config.level.lookAhead,   pts[pts.Length - 1].z, 1e-4f);
        pts.Dispose();
    }

    [Test]
    public void Wave_Scrolls_When_VirtualDistance_Changes()
    {
        _scope.VirtualDistance = 0f;
        var a = _gen.GetWavePoints(64, Allocator.Temp);
        float yBefore = a[32].y; a.Dispose();

        _scope.VirtualDistance = 0.25f;
        var b = _gen.GetWavePoints(64, Allocator.Temp);
        float yAfter = b[32].y; b.Dispose();

        Assert.AreNotEqual(yBefore, yAfter, "Wave Y must change when VirtualDistance advances.");
    }

    [Test]
    public void Parameter_Change_Does_Not_Reshape_Already_Spawned_Samples()
    {
        _gen.Amplitude = 1f;
        float yBefore = _gen.SampleAtLocalZ(10f).y;
        _gen.Amplitude = 100f;
        _gen.Tick(0f);
        Assert.AreEqual(yBefore, _gen.SampleAtLocalZ(10f).y, 1e-4f);
    }

    [Test]
    public void New_Amplitude_Only_Reaches_Samples_Spawned_After_Change()
    {
        _gen.Amplitude = 1f;
        _gen.Tick(0f);
        _scope.VirtualDistance = _config.level.lookAhead + _config.level.decayLength + 1f;
        _gen.Amplitude = 10f;
        _gen.Tick(0f);

        float peak = 0f;
        for (int i = 0; i < 64; i++)
        {
            float z = Mathf.Lerp(_config.level.lookAhead * 0.5f, _config.level.lookAhead * 0.95f, i / 63f);
            peak = Mathf.Max(peak, Mathf.Abs(_gen.SampleAtLocalZ(z).y));
        }
        Assert.Greater(peak, 2f, "Newly-spawned samples should reflect the larger amplitude.");
    }
}
```

- [ ] **Step 3: Run WaveGenerator tests** — Test Runner → EditMode → Run All. Expected: 4 pass.

---

## Task 4: Refactor WaveInputController

**Files:**
- Modify: `Assets/#GAME/Scripts/WaveInputController.cs`

- [ ] **Step 1: Replace `WaveInputController.cs`**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-95)]
public class WaveInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveGenerator waveGenerator;
    [SerializeField] private GameConfig    config;

    [Header("Input Actions (1D axis, -1..+1)")]
    [SerializeField] private InputAction frequencyAxis = new InputAction("Frequency", InputActionType.Value, expectedControlType: "Axis");
    [SerializeField] private InputAction panAxis       = new InputAction("Pan",       InputActionType.Value, expectedControlType: "Axis");
    [SerializeField] private InputAction amplitudeAxis = new InputAction("Amplitude", InputActionType.Value, expectedControlType: "Axis");

    private void Awake()
    {
        EnsureDefaultBinding(frequencyAxis, "<Keyboard>/s", "<Keyboard>/w");
        EnsureDefaultBinding(panAxis,       "<Keyboard>/a", "<Keyboard>/d");
        EnsureDefaultBinding(amplitudeAxis, "<Keyboard>/e", "<Keyboard>/q");

        if (waveGenerator != null && config?.waveInput != null)
        {
            waveGenerator.Frequency = config.waveInput.freqInitial;
            waveGenerator.Amplitude = config.waveInput.ampInitial;
            waveGenerator.Pan       = config.waveInput.panInitial;
        }
    }

    private static void EnsureDefaultBinding(InputAction action, string negative, string positive)
    {
        if (action.bindings.Count > 0) return;
        action.AddCompositeBinding("1DAxis").With("Negative", negative).With("Positive", positive);
    }

    private void OnEnable()  { frequencyAxis.Enable();  panAxis.Enable();  amplitudeAxis.Enable(); }
    private void OnDisable() { frequencyAxis.Disable(); panAxis.Disable(); amplitudeAxis.Disable(); }

    private void Update()
    {
        if (waveGenerator == null || config?.waveInput == null) return;
        var wi = config.waveInput;
        float dt = Time.deltaTime;

        float f = frequencyAxis.ReadValue<float>();
        if (f != 0f)
            waveGenerator.Frequency = Mathf.Clamp(
                waveGenerator.Frequency + f * wi.frequencyRate * dt, wi.freqMin, wi.freqMax);

        float p = panAxis.ReadValue<float>();
        if (p != 0f)
            waveGenerator.Pan = waveGenerator.Pan + p * wi.panRate * dt;

        float a = amplitudeAxis.ReadValue<float>();
        if (a != 0f)
            waveGenerator.Amplitude = Mathf.Clamp(
                waveGenerator.Amplitude + a * wi.amplitudeRate * dt, wi.ampMin, wi.ampMax);
    }
}
```

- [ ] **Step 2: Verify Unity compiles with no errors.**

---

## Task 5: Refactor ProgressDriver + Update ProgressDriverTests

**Files:**
- Modify: `Assets/#GAME/Scripts/ProgressDriver.cs`
- Modify: `Assets/#GAME/Tests/ProgressDriverTests.cs`

- [ ] **Step 1: Replace `ProgressDriver.cs`**

```csharp
using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ProgressDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelScope    levelScope;
    [SerializeField] private WaveGenerator waveGenerator;
    [SerializeField] private Transform     world;
    [SerializeField] private GameConfig    config;

    private bool _finished;

    public event Action OnFinish;

    private float BaseScrollSpeed    => config != null ? config.progress.baseScrollSpeed    : 10f;
    private float SignalGain         => config != null ? config.progress.signalGain         : 0.05f;
    private float MinSpeedMultiplier => config != null ? config.progress.minSpeedMultiplier : 0.5f;
    private float MaxSpeedMultiplier => config != null ? config.progress.maxSpeedMultiplier : 2f;

    public void Setup(LevelScope scope, WaveGenerator gen) { levelScope = scope; waveGenerator = gen; }
    public void SetConfig(GameConfig c) { config = c; }

    private void Update()
    {
        if (!Application.isPlaying) return;
        Tick(Time.deltaTime);
    }

    public void Tick(float dt)
    {
        if (levelScope == null || waveGenerator == null) return;
        if (_finished) { levelScope.ScrollSpeed = 0f; return; }

        float derivative = waveGenerator.SampleDerivativeAtHero();
        float multiplier = Mathf.Clamp(1f + SignalGain * derivative, MinSpeedMultiplier, MaxSpeedMultiplier);
        float speed      = BaseScrollSpeed * multiplier;

        levelScope.ScrollSpeed    = speed;
        levelScope.VirtualDistance = levelScope.VirtualDistance + speed * dt;

        if (world != null)
        {
            Vector3 wp = world.position;
            wp.z = -levelScope.VirtualDistance;
            world.position = wp;
        }

        if (levelScope.IsFinished)
        {
            _finished = true;
            levelScope.ScrollSpeed = 0f;
            OnFinish?.Invoke();
        }
    }
}
```

- [ ] **Step 2: Replace `ProgressDriverTests.cs`**

```csharp
using NUnit.Framework;
using UnityEngine;

public class ProgressDriverTests
{
    private LevelScope     _scope;
    private WaveGenerator  _gen;
    private ProgressDriver _driver;
    private GameConfig     _config;

    [SetUp]
    public void SetUp()
    {
        var lc = ScriptableObject.CreateInstance<LevelConfig>();
        lc.levelLength = 1000f; lc.lookAhead = 30f; lc.decayLength = 5f; lc.playfieldRadius = 8f;

        var pc = ScriptableObject.CreateInstance<ProgressConfig>();
        pc.baseScrollSpeed = 10f; pc.signalGain = 0f;
        pc.minSpeedMultiplier = 0.5f; pc.maxSpeedMultiplier = 2f;

        var wc = ScriptableObject.CreateInstance<WaveGeneratorConfig>();
        wc.sampleDensity = 4f; wc.paramSmoothingDistance = 2f; wc.panLateralScale = 0.2f; wc.bpm = 0f;

        _config = ScriptableObject.CreateInstance<GameConfig>();
        _config.level = lc; _config.progress = pc; _config.waveGenerator = wc;

        var scopeGo = new GameObject("Scope");
        _scope = scopeGo.AddComponent<LevelScope>();
        _scope.SetConfig(_config);

        var genGo = new GameObject("Gen");
        _gen = genGo.AddComponent<WaveGenerator>();
        _gen.SetLevelScope(_scope);
        _gen.SetConfig(_config);
        _gen.Amplitude = 0f; _gen.Frequency = 1f;

        var drvGo = new GameObject("Drv");
        _driver = drvGo.AddComponent<ProgressDriver>();
        _driver.Setup(_scope, _gen);
        _driver.SetConfig(_config);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_driver.gameObject);
        Object.DestroyImmediate(_gen.gameObject);
        Object.DestroyImmediate(_scope.gameObject);
    }

    [Test]
    public void Tick_With_Zero_Gain_Advances_At_BaseSpeed()
    {
        for (int i = 0; i < 100; i++) _driver.Tick(0.1f);
        Assert.AreEqual(100f, _scope.VirtualDistance, 1e-3f);
        Assert.AreEqual(10f,  _scope.ScrollSpeed,     1e-3f);
    }

    [Test]
    public void Tick_Clamps_Speed_By_MaxMultiplier()
    {
        _config.progress.signalGain = 1000f;
        _config.progress.maxSpeedMultiplier = 2f;
        _gen.Amplitude = 1f;
        _scope.VirtualDistance = 0f;
        _driver.Tick(0.016f);
        Assert.LessOrEqual(_scope.ScrollSpeed, 10f * 2f + 1e-3f);
    }

    [Test]
    public void Tick_Clamps_VirtualDistance_To_LevelLength()
    {
        _scope.VirtualDistance = 999f;
        _driver.Tick(5f);
        Assert.AreEqual(1000f, _scope.VirtualDistance, 1e-3f);
        Assert.IsTrue(_scope.IsFinished);
    }

    [Test]
    public void OnFinish_Fires_Exactly_Once()
    {
        int calls = 0;
        _driver.OnFinish += () => calls++;
        _scope.VirtualDistance = 999f;
        _driver.Tick(5f); _driver.Tick(5f); _driver.Tick(5f);
        Assert.AreEqual(1, calls);
    }
}
```

- [ ] **Step 3: Run all tests** — Test Runner → EditMode → Run All. Expected: 14 tests pass (6 LevelScope + 4 WaveGenerator + 4 ProgressDriver).

---

## Task 6: Refactor SpaceRiderCamera

**Files:**
- Modify: `Assets/#GAME/Scripts/SpaceRiderCamera.cs`

- [ ] **Step 1: Replace `SpaceRiderCamera.cs`**

```csharp
using UnityEngine;

[DefaultExecutionOrder(100)]
public class SpaceRiderCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform     hero;
    [SerializeField] private Transform     finishLine;
    [SerializeField] private LevelScope    levelScope;
    [SerializeField] private WaveGenerator waveGenerator;
    [SerializeField] private GameConfig    config;

    private Camera  _cam;
    private Vector3 _velocity;
    private float   _fovVelocity;
    private float   _currentFov;

    private void Awake()
    {
        _cam        = GetComponentInChildren<Camera>();
        _currentFov = _cam != null ? _cam.fieldOfView : (config != null ? config.camera.baseFov : 60f);
    }

    private void LateUpdate()
    {
        if (hero == null) return;
        MoveCamera();
        RotateCamera();
        UpdateFov();
    }

    private void MoveCamera()
    {
        Vector3 offset     = config != null ? config.camera.offset             : new Vector3(0f, 2f, -8f);
        float   smoothTime = config != null ? config.camera.positionSmoothTime : 0.18f;
        transform.position = Vector3.SmoothDamp(transform.position, hero.position + offset, ref _velocity, smoothTime);
    }

    private void RotateCamera()
    {
        Vector3 lookTarget = finishLine != null
            ? finishLine.position
            : transform.position + transform.forward * 100f;

        Vector3 toTarget = lookTarget - transform.position;
        if (toTarget.sqrMagnitude < 1e-4f) return;

        float rollInfluence = config != null ? config.camera.rollInfluence : 0.35f;
        float lookSharpness = config != null ? config.camera.lookSharpness : 8f;

        Vector3 blendedUp = Vector3.Slerp(Vector3.up, hero.up, rollInfluence).normalized;
        Quaternion targetRot = Quaternion.LookRotation(toTarget, blendedUp);
        transform.rotation   = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lookSharpness);
    }

    private void UpdateFov()
    {
        if (_cam == null || config == null) return;
        float speed      = levelScope != null ? levelScope.ScrollSpeed : 0f;
        float targetFov  = config.camera.baseFov + speed * config.camera.fovSpeedScale;
        _currentFov      = Mathf.SmoothDamp(_currentFov, targetFov, ref _fovVelocity, config.camera.fovSmoothTime);
        _cam.fieldOfView = _currentFov;
    }
}
```

- [ ] **Step 2: Verify Unity compiles with no errors.**

---

## Task 7: Refactor Surfer

**Files:**
- Modify: `Assets/#GAME/Scripts/Surfer.cs`

- [ ] **Step 1: Replace `Surfer.cs`**

```csharp
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class Surfer : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private WaveGenerator   waveGenerator;
    [SerializeField] private GameConfig      config;

    private void Update()
    {
        if (splineContainer == null || config?.level == null) return;
        var spline = splineContainer.Spline;
        if (spline == null || spline.Count < 2) return;

        float decay = config.level.decayLength;
        float ahead = config.level.lookAhead;
        float span  = decay + ahead;
        if (span <= 0f) return;

        SplineUtility.Evaluate(spline, decay / span, out float3 pos, out float3 tan, out float3 _);
        Vector3 worldPos = splineContainer.transform.TransformPoint(pos);

        Vector3 parentLocal = transform.parent != null
            ? transform.parent.InverseTransformPoint(worldPos) : worldPos;
        parentLocal.z = 0f;
        transform.position = transform.parent != null
            ? transform.parent.TransformPoint(parentLocal) : parentLocal;

        if (!config.surfer.alignToTangent) return;

        Vector3 worldFwd = splineContainer.transform.TransformDirection(
            (Vector3)math.normalizesafe(tan, new float3(0, 0, 1)));
        if (worldFwd.sqrMagnitude <= 1e-6f) return;
        worldFwd.Normalize();

        Vector3 flatFwd = new Vector3(worldFwd.x, 0f, worldFwd.z);
        if (flatFwd.sqrMagnitude <= 1e-6f) return;
        flatFwd.Normalize();

        float maxTilt  = config.surfer.maxTiltDegrees;
        float maxSin   = Mathf.Sin(maxTilt * Mathf.Deg2Rad);
        float sinPitch = Mathf.Clamp(worldFwd.y, -maxSin, maxSin);
        float cosPitch = Mathf.Sqrt(Mathf.Max(0f, 1f - sinPitch * sinPitch));
        Quaternion yaw = Quaternion.LookRotation(flatFwd * cosPitch + Vector3.up * sinPitch, Vector3.up);

        float pan      = waveGenerator != null ? waveGenerator.GetEffectivePanAtHero() : 0f;
        transform.rotation = yaw * Quaternion.AngleAxis(-pan * maxTilt, Vector3.forward);
    }

    private void OnDrawGizmos()
    {
        if (!GameDebug.ShowGizmos || waveGenerator == null) return;
        Vector3 contact = transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(contact, 0.3f);
        Vector3 fwd = transform.forward;
        Gizmos.DrawLine(contact, contact + fwd * 1.5f);
        Gizmos.DrawLine(contact + fwd * 1.5f, contact + fwd * 1.2f + transform.up * 0.3f);
        Gizmos.DrawLine(contact + fwd * 1.5f, contact + fwd * 1.2f - transform.up * 0.3f);
    }
}
```

- [ ] **Step 2: Verify Unity compiles with no errors.**

---

## Task 8: Refactor FinishLine

**Files:**
- Modify: `Assets/#GAME/Scripts/FinishLine.cs`

- [ ] **Step 1: Replace `FinishLine.cs`**

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishLine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProgressDriver progressDriver;
    [SerializeField] private GameConfig     config;

    [Header("Scene")]
    [SerializeField] private string gameOverSceneName = "gameover";

    private bool _triggered;

    private void OnEnable()  { if (progressDriver != null) progressDriver.OnFinish += HandleFinish; }
    private void OnDisable() { if (progressDriver != null) progressDriver.OnFinish -= HandleFinish; }

    private void HandleFinish()
    {
        if (_triggered) return;
        _triggered = true;
        Time.timeScale = 0f;
        if (Application.isPlaying && !string.IsNullOrEmpty(gameOverSceneName))
            SceneManager.LoadSceneAsync(gameOverSceneName, LoadSceneMode.Additive);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!GameDebug.ShowGizmos) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(10f, 5f, 0.5f));
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 3f,
            $"Finish  z={transform.position.z:F1}m");
    }
#endif
}
```

- [ ] **Step 2: Verify Unity compiles with no errors.**

---

## Task 9: Create MAUVE EditorWindow

**Files:**
- Create: `Assets/#GAME/Editor/MauveGameTuner.cs`

- [ ] **Step 1: Create editor folder if missing** — create `Assets/#GAME/Editor/` directory via OS or Unity Project window.

- [ ] **Step 2: Create `MauveGameTuner.cs`**

```csharp
using UnityEngine;
using UnityEditor;

public class MauveGameTuner : EditorWindow
{
    private const string PrefGizmos    = "MAUVE_ShowGizmos";
    private const string PrefFoldWave  = "MAUVE_FoldWaveInput";
    private const string PrefFoldGen   = "MAUVE_FoldWaveGen";
    private const string PrefFoldLevel = "MAUVE_FoldLevel";
    private const string PrefFoldProg  = "MAUVE_FoldProgress";
    private const string PrefFoldCam   = "MAUVE_FoldCamera";
    private const string PrefFoldSurf  = "MAUVE_FoldSurfer";

    private GameConfig _config;
    private Editor     _waveInputEditor, _waveGenEditor, _levelEditor;
    private Editor     _progressEditor,  _cameraEditor,  _surferEditor;
    private Vector2    _scroll;

    [MenuItem("MAUVE/Game Tuner")]
    public static void Open() => GetWindow<MauveGameTuner>("MAUVE Game Tuner");

    private void OnEnable()
    {
        GameDebug.ShowGizmos = EditorPrefs.GetBool(PrefGizmos, false);
    }

    private void OnDestroy() => ClearEditors();

    private void OnGUI()
    {
        DrawHeader();
        if (_config == null) { DrawCreatePrompt(); return; }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawSection("Wave Input",     PrefFoldWave,  ref _waveInputEditor, _config.waveInput);
        DrawSection("Wave Generator", PrefFoldGen,   ref _waveGenEditor,   _config.waveGenerator);
        DrawSection("Level",          PrefFoldLevel, ref _levelEditor,     _config.level);
        DrawSection("Progress",       PrefFoldProg,  ref _progressEditor,  _config.progress);
        DrawSection("Camera",         PrefFoldCam,   ref _cameraEditor,    _config.camera);
        DrawSection("Surfer",         PrefFoldSurf,  ref _surferEditor,    _config.surfer);
        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        var newCfg = (GameConfig)EditorGUILayout.ObjectField("Game Config", _config, typeof(GameConfig), false);
        if (newCfg != _config) { _config = newCfg; ClearEditors(); }
        if (_config != null && GUILayout.Button("Ping", GUILayout.Width(40)))
            EditorGUIUtility.PingObject(_config);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        bool cur = EditorPrefs.GetBool(PrefGizmos, false);
        bool next = GUILayout.Toggle(cur, cur ? "\u2b24 Debug Gizmos ON" : "\u25cb Debug Gizmos OFF", "Button");
        if (next != cur)
        {
            EditorPrefs.SetBool(PrefGizmos, next);
            GameDebug.ShowGizmos = next;
            SceneView.RepaintAll();
        }
        EditorGUILayout.Space(6);
    }

    private void DrawCreatePrompt()
    {
        EditorGUILayout.HelpBox("No GameConfig assigned. Click to create a default asset set.", MessageType.Info);
        if (GUILayout.Button("Create Default Config"))
            CreateDefaultConfig();
    }

    private void DrawSection(string label, string prefKey, ref Editor editor, ScriptableObject target)
    {
        if (target == null)
        {
            EditorGUILayout.HelpBox($"{label} sub-config is null — assign it on GameConfig.", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        bool fold    = EditorPrefs.GetBool(prefKey, true);
        bool newFold = EditorGUILayout.Foldout(fold, label, true, EditorStyles.foldoutHeader);
        if (GUILayout.Button("Ping", GUILayout.Width(40))) EditorGUIUtility.PingObject(target);
        EditorGUILayout.EndHorizontal();
        if (newFold != fold) EditorPrefs.SetBool(prefKey, newFold);

        if (newFold)
        {
            EditorGUI.indentLevel++;
            Editor.CreateCachedEditor(target, null, ref editor);
            editor.OnInspectorGUI();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(4);
    }

    private void ClearEditors()
    {
        DestroyImmediate(_waveInputEditor);
        DestroyImmediate(_waveGenEditor);
        DestroyImmediate(_levelEditor);
        DestroyImmediate(_progressEditor);
        DestroyImmediate(_cameraEditor);
        DestroyImmediate(_surferEditor);
    }

    private void CreateDefaultConfig()
    {
        const string parent = "Assets/#GAME/Config";
        const string folder = "Assets/#GAME/Config/Default";

        if (!AssetDatabase.IsValidFolder(parent))
            AssetDatabase.CreateFolder("Assets/#GAME", "Config");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(parent, "Default");

        var waveInput     = CreateOrLoad<WaveInputConfig>(folder,     "WaveInput_Default");
        var waveGenerator = CreateOrLoad<WaveGeneratorConfig>(folder, "WaveGenerator_Default");
        var level         = CreateOrLoad<LevelConfig>(folder,         "Level_Default");
        var progress      = CreateOrLoad<ProgressConfig>(folder,      "Progress_Default");
        var camera        = CreateOrLoad<CameraConfig>(folder,        "Camera_Default");
        var surfer        = CreateOrLoad<SurferConfig>(folder,        "Surfer_Default");

        var cfg = CreateOrLoad<GameConfig>(folder, "GameConfig_Default");
        cfg.waveInput     = waveInput;
        cfg.waveGenerator = waveGenerator;
        cfg.level         = level;
        cfg.progress      = progress;
        cfg.camera        = camera;
        cfg.surfer        = surfer;

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _config = cfg;
        ClearEditors();
        EditorGUIUtility.PingObject(_config);
    }

    private static T CreateOrLoad<T>(string folder, string name) where T : ScriptableObject
    {
        string path     = $"{folder}/{name}.asset";
        var    existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        var asset = CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
```

- [ ] **Step 3: Verify Unity compiles. Open `MAUVE → Game Tuner`.** The window should appear.

---

## Task 10: Create Default Assets and Wire Scene

**Files:** Scene `main.unity` modified via Unity Editor.

- [ ] **Step 1: Create default config assets** — In the MAUVE Game Tuner window, click **"Create Default Config"**. Verify `Assets/#GAME/Config/Default/` is populated with 7 assets.

- [ ] **Step 2: Wire `config` field on every scene component** — Open `MAUVE → Game Tuner`. Then for each component in the `main` scene, drag `GameConfig_Default` into its `Config` field in the Inspector:

| GameObject | Component |
|---|---|
| `LevelSCope` | `LevelScope` |
| `ProgressiveDriver` | `ProgressDriver` |
| `HeroBundle/WaveGenerator` | `WaveGenerator` |
| `HeroBundle/WaveGenerator` | `WaveInputController` |
| `HeroBundle/Hero` | `Surfer` |
| `Main Camera` | `SpaceRiderCamera` |
| `World/FinishLine` | `FinishLine` |

- [ ] **Step 3: Drag `GameConfig_Default` into the MAUVE window's Config field.** The six sub-config foldouts should appear.

- [ ] **Step 4: Assign the config field on MAUVE window** — In the Game Tuner window, set the `Game Config` object field to `GameConfig_Default`.

- [ ] **Step 5: Press Play** — verify the game runs without NullReferenceExceptions. Hero should ride the wave, camera should follow.

- [ ] **Step 6: Toggle "Debug Gizmos ON" in the MAUVE window** — switch to Scene View. Verify:
  - Red wire cylinder appears at origin (playfield boundary)
  - Cyan line shows wave path
  - Yellow/red spheres mark LookAhead / DecayLength zones
  - Cyan sphere + arrow on hero
  - Green wire box + label at FinishLine

- [ ] **Step 7: Save scene** — File → Save (Ctrl+S).

- [ ] **Step 8: Run all tests one final time** — Test Runner → EditMode → Run All. Expected: 14 pass.
