# Treadmill LevelScope Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Switch `SpaceRider`'s level geometry from world-space forward motion to a floating-origin treadmill where the hero is pinned at local (0,0,0) and `LevelScope` owns a scalar `virtualDistance` that scrolls the wave pattern past.

**Architecture:** `LevelScope` becomes the progress/speed authority (no geometry). `WaveGenerator` samples local Z ∈ [−decayLength, +lookAhead], using `virtualZ = localZ + virtualDistance` as its spatial input so the pattern scrolls as progress advances. A new `ProgressDriver` reads the signal derivative at the hero and advances `virtualDistance` each frame. `Surfer` is pinned — no Z movement.

**Tech Stack:** Unity 6 (URP 17.3), C#, Unity Splines, `com.unity.test-framework` (NUnit) for edit-mode tests, `Unity.Collections.NativeArray`.

**Spec:** `docs/superpowers/specs/2026-04-18-treadmill-levelscope-design.md`

---

## File Structure

**Create:**
- `SpaceRider/Assets/#GAME/Scripts/SpaceRider.Game.asmdef` — assembly def for game scripts so tests can reference them
- `SpaceRider/Assets/#GAME/Scripts/ProgressDriver.cs` — treadmill motor
- `SpaceRider/Assets/#GAME/Tests/SpaceRider.Game.Tests.asmdef` — edit-mode test assembly
- `SpaceRider/Assets/#GAME/Tests/LevelScopeTests.cs` — progress math tests
- `SpaceRider/Assets/#GAME/Tests/WaveGeneratorTests.cs` — derivative sanity test
- `SpaceRider/Assets/#GAME/Tests/ProgressDriverTests.cs` — integration test

**Modify:**
- `SpaceRider/Assets/#GAME/Scripts/LevelScope.cs` — add progress/speed/window fields and properties
- `SpaceRider/Assets/#GAME/Scripts/WaveGenerator.cs` — switch to windowed sampling + derivative probe + execution order attr
- `SpaceRider/Assets/#GAME/Scripts/Surfer.cs` — pin to local (0,0,0), use fixed spline-t + execution order attr
- `SpaceRider/Assets/#GAME/Scripts/WaveRibbonUpdater.cs` — add execution order attr (no logic change)

**Unchanged:** `RibbonVisualizer.cs`, `WaveController.cs`.

---

## Task 1: Assembly definitions for game + tests

Unity needs an asmdef on game code before a test asmdef can reference it. Without this, tests cannot see types like `LevelScope`.

**Files:**
- Create: `SpaceRider/Assets/#GAME/Scripts/SpaceRider.Game.asmdef`
- Create: `SpaceRider/Assets/#GAME/Tests/SpaceRider.Game.Tests.asmdef`

- [ ] **Step 1: Create the game asmdef**

Write `SpaceRider/Assets/#GAME/Scripts/SpaceRider.Game.asmdef`:

```json
{
    "name": "SpaceRider.Game",
    "rootNamespace": "",
    "references": [
        "GUID:4a033b9f8ad45e141afb1bfd2d8911ff"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

Note: `GUID:4a033b9f8ad45e141afb1bfd2d8911ff` is `Unity.Splines`. If that GUID is wrong on this machine, open `SpaceRider/Packages/packages-lock.json` and confirm the splines package is installed; Unity will auto-resolve the reference by name on first import, so you can also use `"Unity.Splines"` instead of the GUID form.

- [ ] **Step 2: Create the tests folder and test asmdef**

Write `SpaceRider/Assets/#GAME/Tests/SpaceRider.Game.Tests.asmdef`:

```json
{
    "name": "SpaceRider.Game.Tests",
    "rootNamespace": "",
    "references": [
        "SpaceRider.Game",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Refresh Unity and verify compilation**

If Unity Editor is open, it auto-refreshes. Otherwise: next time Unity opens, the asmdefs will be picked up. To verify from the console, either:
- Use the `assets-refresh` MCP tool if Unity is running, then check for compile errors via `console-get-logs`.
- Or open the project in Unity and confirm no red errors in the Console.

Expected: No compile errors. The existing scripts in `#GAME/Scripts/` now live in `SpaceRider.Game` assembly.

- [ ] **Step 4: Commit**

```bash
git add SpaceRider/Assets/#GAME/Scripts/SpaceRider.Game.asmdef SpaceRider/Assets/#GAME/Tests/SpaceRider.Game.Tests.asmdef
git commit -m "chore: add asmdefs for game and tests

Prepares the #GAME scripts for test targeting by promoting them into
SpaceRider.Game, and adds an Editor-only SpaceRider.Game.Tests assembly.
"
```

---

## Task 2: LevelScope progress fields + tests (TDD)

Promote `LevelScope` from geometry-holder to progress/speed authority. TDD — test first.

**Files:**
- Modify: `SpaceRider/Assets/#GAME/Scripts/LevelScope.cs`
- Create: `SpaceRider/Assets/#GAME/Tests/LevelScopeTests.cs`

- [ ] **Step 1: Write the failing tests**

Write `SpaceRider/Assets/#GAME/Tests/LevelScopeTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class LevelScopeTests
{
    private LevelScope MakeScope(float length)
    {
        var go = new GameObject("LevelScope");
        var scope = go.AddComponent<LevelScope>();
        scope.LevelLength = length;
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

- [ ] **Step 2: Run tests to verify they fail**

Run via Unity's Test Runner (Window → General → Test Runner → EditMode → Run All) or via MCP `tests-run` with test mode `EditMode`.

Expected: all tests in `LevelScopeTests` FAIL with missing member errors on `VirtualDistance`, `Progress01`, `IsFinished`.

- [ ] **Step 3: Rewrite `LevelScope.cs`**

Replace the entire contents of `SpaceRider/Assets/#GAME/Scripts/LevelScope.cs` with:

```csharp
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run the EditMode tests again.

Expected: All six `LevelScopeTests` PASS.

- [ ] **Step 5: Commit**

```bash
git add SpaceRider/Assets/#GAME/Scripts/LevelScope.cs SpaceRider/Assets/#GAME/Tests/LevelScopeTests.cs
git commit -m "feat(LevelScope): become progress + window authority

Adds VirtualDistance, ScrollSpeed, LookAhead, DecayLength, Progress01,
IsFinished. LevelLength is no longer a world-space extent.
"
```

---

## Task 3: WaveGenerator windowed sampling + derivative probe (TDD)

Switch the sampling domain from `[0, LevelLength]` to `[-DecayLength, +LookAhead]`, shift the spatial phase by `VirtualDistance`, and expose a derivative probe at the hero.

**Files:**
- Modify: `SpaceRider/Assets/#GAME/Scripts/WaveGenerator.cs`
- Create: `SpaceRider/Assets/#GAME/Tests/WaveGeneratorTests.cs`

- [ ] **Step 1: Write the failing tests**

Write `SpaceRider/Assets/#GAME/Tests/WaveGeneratorTests.cs`:

```csharp
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

public class WaveGeneratorTests
{
    private LevelScope _scope;
    private WaveGenerator _gen;

    [SetUp]
    public void SetUp()
    {
        var scopeGo = new GameObject("Scope");
        _scope = scopeGo.AddComponent<LevelScope>();
        _scope.LevelLength = 1000f;
        _scope.LookAhead = 30f;
        _scope.DecayLength = 5f;

        var genGo = new GameObject("Gen");
        _gen = genGo.AddComponent<WaveGenerator>();
        _gen.SetLevelScope(_scope);
        _gen.Amplitude = 1f;
        _gen.Frequency = 1f;
        _gen.Pan = 0f;
        _gen.Bpm = 0f; // freeze phase
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
        Assert.AreEqual(-_scope.DecayLength, pts[0].z, 1e-4f);
        Assert.AreEqual(+_scope.LookAhead,   pts[pts.Length - 1].z, 1e-4f);
        pts.Dispose();
    }

    [Test]
    public void Wave_Scrolls_When_VirtualDistance_Changes()
    {
        _scope.VirtualDistance = 0f;
        var a = _gen.GetWavePoints(64, Allocator.Temp);
        float yBefore = a[32].y;
        a.Dispose();

        _scope.VirtualDistance = 0.25f; // quarter of a cycle at frequency=1
        var b = _gen.GetWavePoints(64, Allocator.Temp);
        float yAfter = b[32].y;
        b.Dispose();

        Assert.AreNotEqual(yBefore, yAfter, "Wave Y at the same local Z must change when VirtualDistance advances.");
    }

    [Test]
    public void SampleDerivativeAtHero_Matches_Finite_Difference()
    {
        _scope.VirtualDistance = 0.1f;
        float analytical = _gen.SampleDerivativeAtHero();

        // Finite-difference approximation using two samples straddling local Z = 0
        float h = 1e-3f;
        float yMinus = _gen.SampleAtLocalZ(-h).y;
        float yPlus  = _gen.SampleAtLocalZ(+h).y;
        float fd = (yPlus - yMinus) / (2f * h);

        Assert.AreEqual(fd, analytical, 1e-2f);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run EditMode tests.

Expected: `WaveGeneratorTests` FAIL — `SetLevelScope`, `SampleDerivativeAtHero`, `SampleAtLocalZ` don't exist, and sampling range is wrong.

- [ ] **Step 3: Rewrite `WaveGenerator.cs`**

Replace the entire contents of `SpaceRider/Assets/#GAME/Scripts/WaveGenerator.cs` with:

```csharp
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

    private float _phase; // radians, advances each frame

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
            float t  = resolution > 1 ? i / (resolution - 1f) : 0f;
            float z  = Mathf.Lerp(zMin, zMax, t);
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
        // pan rotates the oscillation plane: pan=0 is pure Y, pan=±1 is pure X.
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run EditMode tests.

Expected: All three `WaveGeneratorTests` PASS, and `LevelScopeTests` still PASS.

- [ ] **Step 5: Commit**

```bash
git add SpaceRider/Assets/#GAME/Scripts/WaveGenerator.cs SpaceRider/Assets/#GAME/Tests/WaveGeneratorTests.cs
git commit -m "feat(WaveGenerator): windowed sampling + derivative probe

Samples [-DecayLength, +LookAhead] in local Z and uses
virtualZ = localZ + VirtualDistance so the pattern scrolls past.
Adds SampleAtLocalZ and SampleDerivativeAtHero for the progress driver.
"
```

---

## Task 4: Pin the Surfer to local (0, 0, 0)

Hero no longer owns its Z. It snaps XY to the spline at the fixed t corresponding to local Z = 0.

**Files:**
- Modify: `SpaceRider/Assets/#GAME/Scripts/Surfer.cs`

- [ ] **Step 1: Rewrite `Surfer.cs`**

Replace the entire contents of `SpaceRider/Assets/#GAME/Scripts/Surfer.cs` with:

```csharp
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Hero is pinned to local Z = 0. Each frame XY is snapped onto the wave
/// spline at the fixed parameter t = decayLength / (decayLength + lookAhead).
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class Surfer : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private LevelScope      levelScope;
    [SerializeField] private bool            alignToTangent = true;

    private void Update()
    {
        if (splineContainer == null || levelScope == null) return;
        var spline = splineContainer.Spline;
        if (spline == null || spline.Count < 2) return;

        float span = levelScope.DecayLength + levelScope.LookAhead;
        if (span <= 0f) return;

        float t = levelScope.DecayLength / span;

        SplineUtility.Evaluate(spline, t, out float3 pos, out float3 tan, out float3 _);
        Vector3 worldPos = splineContainer.transform.TransformPoint(pos);

        // Pin local Z to 0 (relative to the Surfer's parent, i.e. PlayerBundle).
        // We express the snapped XY in world, then re-home to parent-local with Z=0.
        Vector3 parentLocal = transform.parent != null
            ? transform.parent.InverseTransformPoint(worldPos)
            : worldPos;
        parentLocal.z = 0f;
        transform.position = transform.parent != null
            ? transform.parent.TransformPoint(parentLocal)
            : parentLocal;

        if (alignToTangent)
        {
            float3 fwd = math.normalizesafe(tan, new float3(0, 0, 1));
            if (math.lengthsq(fwd) > 0.0001f)
                transform.rotation = Quaternion.LookRotation(
                    splineContainer.transform.TransformDirection(fwd),
                    Vector3.up
                );
        }
    }
}
```

- [ ] **Step 2: Run existing tests to verify no regressions**

Run EditMode tests.

Expected: All previously-passing tests still PASS. No tests fail.

- [ ] **Step 3: Commit**

```bash
git add SpaceRider/Assets/#GAME/Scripts/Surfer.cs
git commit -m "feat(Surfer): pin hero to local Z=0

Hero no longer owns its Z. Each frame XY is snapped to the spline at the
fixed parameter corresponding to local Z = 0, and Z is forced to 0 in
parent-local space (the PlayerBundle).
"
```

---

## Task 5: ProgressDriver — the treadmill motor (TDD)

Reads the signal derivative at the hero, computes a clamped scroll speed, and advances `LevelScope.VirtualDistance`.

**Files:**
- Create: `SpaceRider/Assets/#GAME/Scripts/ProgressDriver.cs`
- Create: `SpaceRider/Assets/#GAME/Tests/ProgressDriverTests.cs`

- [ ] **Step 1: Write the failing tests**

Write `SpaceRider/Assets/#GAME/Tests/ProgressDriverTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class ProgressDriverTests
{
    private LevelScope    _scope;
    private WaveGenerator _gen;
    private ProgressDriver _driver;

    [SetUp]
    public void SetUp()
    {
        var scopeGo = new GameObject("Scope");
        _scope = scopeGo.AddComponent<LevelScope>();
        _scope.LevelLength = 1000f;
        _scope.LookAhead = 30f;
        _scope.DecayLength = 5f;

        var genGo = new GameObject("Gen");
        _gen = genGo.AddComponent<WaveGenerator>();
        _gen.SetLevelScope(_scope);
        _gen.Amplitude = 0f; // derivative is zero → multiplier is 1
        _gen.Frequency = 1f;
        _gen.Bpm = 0f;

        var drvGo = new GameObject("Drv");
        _driver = drvGo.AddComponent<ProgressDriver>();
        _driver.Setup(_scope, _gen);
        _driver.BaseScrollSpeed = 10f;
        _driver.SignalGain = 0f;
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
        for (int i = 0; i < 100; i++) _driver.Tick(0.1f); // 10 simulated seconds

        Assert.AreEqual(100f, _scope.VirtualDistance, 1e-3f);
        Assert.AreEqual(10f, _scope.ScrollSpeed, 1e-3f);
    }

    [Test]
    public void Tick_Clamps_Speed_By_MaxMultiplier()
    {
        _driver.BaseScrollSpeed = 10f;
        _driver.SignalGain = 1000f;       // huge gain
        _driver.MaxSpeedMultiplier = 2f;
        _gen.Amplitude = 1f;              // non-zero derivative
        _scope.VirtualDistance = 0f;      // at t=0, derivative = 2π (positive)

        _driver.Tick(0.016f);

        Assert.LessOrEqual(_scope.ScrollSpeed, 10f * 2f + 1e-3f);
    }

    [Test]
    public void Tick_Clamps_VirtualDistance_To_LevelLength()
    {
        _scope.VirtualDistance = 999f;
        _driver.BaseScrollSpeed = 10f;

        _driver.Tick(5f); // would add 50, but we clamp at LevelLength=1000

        Assert.AreEqual(1000f, _scope.VirtualDistance, 1e-3f);
        Assert.IsTrue(_scope.IsFinished);
    }

    [Test]
    public void OnFinish_Fires_Exactly_Once()
    {
        int calls = 0;
        _driver.OnFinish += () => calls++;
        _scope.VirtualDistance = 999f;

        _driver.Tick(5f); // crosses the line
        _driver.Tick(5f); // already finished — should not re-fire
        _driver.Tick(5f);

        Assert.AreEqual(1, calls);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run EditMode tests.

Expected: `ProgressDriverTests` FAIL — `ProgressDriver` class does not exist.

- [ ] **Step 3: Implement `ProgressDriver.cs`**

Write `SpaceRider/Assets/#GAME/Scripts/ProgressDriver.cs`:

```csharp
using System;
using UnityEngine;

/// <summary>
/// Drives virtualDistance on LevelScope using a constant base speed modulated
/// by the wave's derivative at the hero. Clamped to [minMult, maxMult] of the
/// base speed and to LevelLength.
/// </summary>
[DefaultExecutionOrder(-100)]
public class ProgressDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelScope    levelScope;
    [SerializeField] private WaveGenerator waveGenerator;

    [Header("Speed")]
    [SerializeField] private float baseScrollSpeed    = 10f;
    [SerializeField] private float signalGain         = 0.05f;
    [SerializeField] private float minSpeedMultiplier = 0.5f;
    [SerializeField] private float maxSpeedMultiplier = 2.0f;

    private bool _finished;

    public event Action OnFinish;

    public float BaseScrollSpeed    { get => baseScrollSpeed;    set => baseScrollSpeed = value; }
    public float SignalGain         { get => signalGain;         set => signalGain = value; }
    public float MinSpeedMultiplier { get => minSpeedMultiplier; set => minSpeedMultiplier = value; }
    public float MaxSpeedMultiplier { get => maxSpeedMultiplier; set => maxSpeedMultiplier = value; }

    public void Setup(LevelScope scope, WaveGenerator gen)
    {
        levelScope = scope;
        waveGenerator = gen;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        Tick(Time.deltaTime);
    }

    /// <summary>Advances the treadmill by dt seconds. Exposed for tests.</summary>
    public void Tick(float dt)
    {
        if (levelScope == null || waveGenerator == null) return;

        if (_finished)
        {
            levelScope.ScrollSpeed = 0f;
            return;
        }

        float derivative = waveGenerator.SampleDerivativeAtHero();
        float multiplier = Mathf.Clamp(
            1f + signalGain * derivative,
            minSpeedMultiplier,
            maxSpeedMultiplier);

        float speed = baseScrollSpeed * multiplier;
        levelScope.ScrollSpeed = speed;
        levelScope.VirtualDistance = levelScope.VirtualDistance + speed * dt;

        if (levelScope.IsFinished)
        {
            _finished = true;
            levelScope.ScrollSpeed = 0f;
            OnFinish?.Invoke();
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run EditMode tests.

Expected: All `ProgressDriverTests` PASS, plus all previous tests.

- [ ] **Step 5: Commit**

```bash
git add SpaceRider/Assets/#GAME/Scripts/ProgressDriver.cs SpaceRider/Assets/#GAME/Tests/ProgressDriverTests.cs
git commit -m "feat(ProgressDriver): treadmill motor with signal-modulated speed

Reads the wave derivative at the hero, computes a clamped scroll speed,
advances LevelScope.VirtualDistance, and fires OnFinish once.
"
```

---

## Task 6: Add execution-order attribute to WaveRibbonUpdater

Lock the per-frame order without touching logic. The spec requires `WaveGenerator` (advance phase) runs before `WaveRibbonUpdater` (sample + rebuild spline), which runs before `Surfer` (snap to spline).

**Files:**
- Modify: `SpaceRider/Assets/#GAME/Scripts/WaveRibbonUpdater.cs`

- [ ] **Step 1: Add the attribute**

Open `SpaceRider/Assets/#GAME/Scripts/WaveRibbonUpdater.cs`. Replace:

```csharp
[ExecuteAlways]
[RequireComponent(typeof(SplineContainer))]
public class WaveRibbonUpdater : MonoBehaviour
```

with:

```csharp
[ExecuteAlways]
[DefaultExecutionOrder(-70)]
[RequireComponent(typeof(SplineContainer))]
public class WaveRibbonUpdater : MonoBehaviour
```

Order summary after this task:
- `ProgressDriver` -100 → advances VirtualDistance first
- `WaveGenerator` -90 → advances phase
- `WaveRibbonUpdater` -70 → samples points, rebuilds spline
- `Surfer` -50 → snaps XY to spline
- `RibbonVisualizer` LateUpdate (unchanged) → rebuilds mesh last

- [ ] **Step 2: Run all tests to confirm no regressions**

Run EditMode tests.

Expected: all tests PASS.

- [ ] **Step 3: Commit**

```bash
git add SpaceRider/Assets/#GAME/Scripts/WaveRibbonUpdater.cs
git commit -m "chore: lock wave/ribbon update order with DefaultExecutionOrder"
```

---

## Task 7: Manual smoke test in Unity

Verify visually that the treadmill works. No test code — this is an editor check.

**Files:** none (scene inspection only)

- [ ] **Step 1: Open the scene**

Open `SpaceRider/Assets/Scenes/SampleScene.unity` in Unity. Expected to already contain:
- A `LevelScope` GameObject (or one on the PlayerBundle)
- A `WaveGenerator` referencing the scope
- A `WaveRibbonUpdater` on the ribbon
- A `Surfer` referencing the spline and scope
- A `RibbonVisualizer` on a ribbon mesh

If `ProgressDriver` is not yet in the scene: add it as a component on the same GameObject as `WaveGenerator` (or anywhere sensible) and wire its `LevelScope` and `WaveGenerator` fields.

- [ ] **Step 2: Configure the hero as pinned**

Ensure the `Surfer` GameObject's transform has `localPosition = (0, 0, 0)`. If its parent is a `PlayerBundle`, the parent may be anywhere in the world — the treadmill does not care.

- [ ] **Step 3: Enter Play mode**

Watch for:
- Hero stays at local (0, 0, 0) relative to its parent for the entire run.
- Ribbon covers roughly the span (−DecayLength, +LookAhead) in the hero's local Z and does not grow/shrink.
- The wave pattern visually scrolls toward the camera as time passes.
- `LevelScope.VirtualDistance` ticks upward in the inspector; `ScrollSpeed` is non-zero.
- Around `VirtualDistance == LevelLength`, motion freezes and `IsFinished` is true.

If any of these fail, diagnose — do NOT silently "fix" by tweaking values without understanding the cause.

- [ ] **Step 4: Optional — tune values**

Default values (`baseScrollSpeed = 10`, `signalGain = 0.05`, min/max multiplier = 0.5/2.0, `lookAhead = 30`, `decayLength = 5`, `levelLength = 100`) are starting points. Iterate in the inspector until the surf feel is right. Save the scene when done.

- [ ] **Step 5: Commit scene changes (if any)**

```bash
git add SpaceRider/Assets/Scenes/SampleScene.unity
git commit -m "chore: wire ProgressDriver + treadmill defaults in sample scene"
```

If the scene did not change, skip this step.

---

## Done

After Task 7, the treadmill model is live:
- Hero is pinned at local origin
- `LevelScope.VirtualDistance` is the single source of truth for progress
- `WaveGenerator` samples a local window and the pattern scrolls past
- `ProgressDriver` drives the treadmill using the signal derivative
- Future consumers (obstacles, parallax) plug in via `LevelScope.ScrollSpeed`
