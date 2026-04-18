# Treadmill LevelScope — Design

**Date:** 2026-04-18
**Status:** Approved, awaiting implementation plan
**Author:** Claude + Romain

## Problem

The current `LevelScope` treats `levelLength` as a world-space Z extent. `WaveGenerator` samples positions from Z=0 to Z=`levelLength`, and `Surfer` reads `transform.position.z / levelLength` to find its spline parameter. This means the hero genuinely moves forward through world space across the full length of the level.

For SpaceRider we want long levels (potentially tens of thousands of units). Keeping all level data in world space would eventually hit floating-point precision issues, and it bakes a world-space scale into scripts that don't need it.

## Goal

Switch to a **floating-origin / treadmill** model:

- The player bundle (hero + camera + wave generator + ribbon) lives near world origin and never translates forward.
- "Forward progress" becomes a scalar on `LevelScope`.
- The hero is pinned to local Z = 0; the ribbon and (future) obstacles scroll past.
- The wave pattern scrolls past the hero as progress advances, so visually the player still surfs forward.

Non-goals: obstacle system, cylinder bounds / death, FMOD audio, parallax scenery. Hooks for those are considered but not built here.

## Architecture

### Coordinate system

| Thing | World position | Notes |
|---|---|---|
| PlayerBundle root | Near origin, static | Never translates forward |
| Hero (`Surfer`) | Local (0, 0, 0) | Pinned. XY snapped to spline. |
| `WaveGenerator` | Local Z = `+lookAhead` | The "signal source" from the design doc |
| Ribbon span | Local Z ∈ `[-decayLength, +lookAhead]` | Fixed window |
| Obstacles (future) | Spawn at +lookAhead, scroll to −decay | Read `LevelScope.scrollSpeed` |
| Scenery (future) | Parallax, scroll at fractional rate | Read `LevelScope.scrollSpeed` |

### Progress authority — `LevelScope`

Owns the scalars that describe "where am I along the virtual level, and how fast am I going?"

**Serialized:**
- `levelLength` (float, existing) — total virtual distance. No longer a world-space extent.
- `lookAhead` (float, new) — how far in front of the hero the ribbon extends, in local Z.
- `decayLength` (float, new) — how far behind the hero the ribbon extends, in local Z.

**Runtime:**
- `virtualDistance` (float) — current progress, 0 → `levelLength`. Advanced by `ProgressDriver`.
- `scrollSpeed` (float) — current derivative of `virtualDistance` (m/s). Written by `ProgressDriver`.

**Read-only:**
- `Progress01` — `virtualDistance / levelLength`, guarded against divide-by-zero.
- `IsFinished` — `virtualDistance >= levelLength`.

### Wave signal — `WaveGenerator`

Same responsibility as today (produce wave spine points; own signal parameters), but the sampling domain changes.

- `GetWavePoints(resolution, allocator)` samples local Z ∈ `[-decayLength, +lookAhead]` read from `LevelScope`.
- Wave function becomes `f(virtualZ, phase)` where `virtualZ = localZ + levelScope.virtualDistance`. This is what makes the pattern scroll past the hero as `virtualDistance` advances.
- New: `float SampleDerivativeAtHero()` — returns `∂f/∂virtualZ` at local Z = 0. Used by `ProgressDriver` to compute the signal modifier.
- `_phase` still advances with BPM (time-based drift, layered on top of spatial scrolling).

### Hero — `Surfer`

- Hero's transform stays at local (0, 0, 0). Script no longer reads or writes Z.
- Snaps XY to the spline at the constant t-value corresponding to the hero's local Z = 0. Given the spline covers `[-decayLength, +lookAhead]`, that's `t = decayLength / (decayLength + lookAhead)`.
- Still aligns forward rotation to the spline tangent for surfing feel (optional).

### Treadmill motor — `ProgressDriver` (new)

Drives forward motion using the signal derivative as a modifier on a constant base speed.

**Serialized:**
- `baseScrollSpeed` (float, m/s)
- `signalGain` (float, unitless)
- `minSpeedMultiplier`, `maxSpeedMultiplier` (floats, clamp range — e.g. 0.5 and 2.0)

**Per frame:**
```
derivative = waveGenerator.SampleDerivativeAtHero()
multiplier = clamp(1 + signalGain * derivative, minMult, maxMult)
levelScope.scrollSpeed    = baseScrollSpeed * multiplier
levelScope.virtualDistance = min(levelScope.virtualDistance + scrollSpeed * dt,
                                 levelScope.levelLength)
if (IsFinished && !_fired) { _fired = true; OnFinish?.Invoke(); }
```

### Unchanged

- `RibbonVisualizer` — uses `waveSource` and `surfer` transforms; since they're now pinned, the script doesn't need to change. The ribbon simply always covers the same local span.
- `WaveRibbonUpdater` — still rebuilds the spline from `WaveGenerator.GetWavePoints` each frame; only the input range changed.

## Frame Order

Use `[DefaultExecutionOrder]` attributes to fix the order:

1. `ProgressDriver` — reads the previous frame's derivative, advances `virtualDistance`, writes `scrollSpeed`.
2. `WaveGenerator` — advances `_phase` (BPM drift).
3. `WaveRibbonUpdater` — calls `GetWavePoints`, rebuilds spline.
4. `Surfer` — snaps XY to spline at the fixed hero-t.
5. `RibbonVisualizer.LateUpdate` — rebuilds mesh.

(Future consumers — obstacle spawner, parallax — read `LevelScope.scrollSpeed` in `Update` and can run after `ProgressDriver`.)

## Edge Cases

- **Flat signal / zero derivative** — multiplier = 1.0; player always moves at base speed. No softlock.
- **Derivative spike** (e.g. user slams frequency to max) — clamped by `[minSpeedMultiplier, maxSpeedMultiplier]`.
- **`levelLength == 0`** — `Progress01` returns 1, `IsFinished` returns true, no NaN.
- **Finish overshoot** — `virtualDistance` clamped to `levelLength`; `OnFinish` fires exactly once.
- **Editor mode** — `ProgressDriver` should only advance `virtualDistance` in Play mode so scene editing doesn't scroll the level. `[ExecuteAlways]` is fine on `WaveGenerator`, `WaveRibbonUpdater`, `Surfer`, `RibbonVisualizer` (they already have it) so the ribbon still previews.

## Testing

- **Unit:** `LevelScope` progress math (`Progress01` clamping, `IsFinished`, finish latch).
- **Unit / edit-mode:** `WaveGenerator.SampleDerivativeAtHero` consistency — compare against a finite-difference of two `GetWavePoints` samples straddling local Z = 0.
- **Edit-mode integration:** run `ProgressDriver` for 10 simulated seconds at `baseScrollSpeed = 10`, `signalGain = 0`; assert `virtualDistance ≈ 100`.
- **Visual** (manual): open the scene, enter Play mode, confirm the ribbon pattern scrolls past the hero as `virtualDistance` grows, and the hero stays at local (0, 0, 0).

## Out of Scope (Deliberately)

- Cylinder XY bounds + death (`LevelScope` will grow to own these, but not now).
- Obstacle system (but `LevelScope.scrollSpeed` is the intended contract).
- FMOD audio integration.
- Background scenery / parallax.
- Camera behavior (camera is already part of PlayerBundle; no changes needed for the treadmill switch).

## Open Questions

None remaining at design time. Tuning values (`lookAhead`, `decayLength`, `baseScrollSpeed`, `signalGain`, clamp range) will be iterated in-editor during implementation.
