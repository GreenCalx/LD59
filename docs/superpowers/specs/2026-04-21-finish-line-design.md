# Finish Line — Design Spec
**Date:** 2026-04-21  
**Status:** Approved

## Overview

Add a physical finish-line trigger box to the `main` scene, a checkerboard flag visual plane, and a win flow in the gameover scene that waits for press-any-key then returns to the title screen. All changes are purely additive — no existing functionality is removed.

---

## 1. Trigger & Win State

### `GameResult` (new static class)
A single static class, no MonoBehaviour:

```csharp
public static class GameResult
{
    public static bool IsWin;
}
```

Resets to `false` automatically each time the `main` scene reloads (static fields reset on domain reload; in IL2CPP builds the scene reload re-initialises statics via the normal flow).

### `FinishLine.cs` (extended)
- Add `OnTriggerEnter(Collider other)`: checks that the collider belongs to the player (via `PlayerDeath` component lookup), then calls `HandleFinish()`.
- `HandleFinish()` sets `GameResult.IsWin = true` before `SceneManager.LoadSceneAsync`.
- The existing `_triggered` bool prevents double-firing from both the physical trigger and `ProgressDriver.OnFinish`.
- The BoxCollider on the FinishLine GameObject must be set to **Is Trigger = true**. Size: wide enough to span the playfield (e.g. `(20, 20, 2)`).

---

## 2. Win Flow in Gameover Scene

### `WinSequence` (new MonoBehaviour)
Lives on the gameover canvas root (or a dedicated child GameObject).

Behaviour:
1. `Start()` — check `GameResult.IsWin`. If false, disable self and return (death path unchanged).
2. If win: hide the restart button, show a "PRESS ANY KEY" TextMeshPro label (reuse `TextBlink` component for blink effect).
3. `Update()` — poll `Input.anyKeyDown` (unscaled; `timeScale = 0` at this point).
4. On key press: `Time.timeScale = 1f` → `SceneManager.LoadScene("title")`.

`UIGameOver.cs` is unchanged — it already reads `HoopTracker.Instance.TotalScore` which remains valid.

`GameOverController.Restart()` is unchanged — still fires only when the restart button is clicked (death path).

---

## 3. Checkerboard Flag Shader & Plane

### `FinishFlag.shader` (new URP shader, `MAUVE/FinishFlag`)

**Properties:**
| Property | Default | Purpose |
|---|---|---|
| `_Alpha` | 0.4 | Overall transparency |
| `_Tiling` | 8 | Checker cell density (cells per UV unit) |
| `_WaveSpeed` | 1.5 | UV wave scroll speed |
| `_WaveAmplitude` | 0.04 | Wave displacement magnitude |
| `_WaveFrequency` | 6.0 | Wave spatial frequency along V axis |

**Fragment logic:**
```hlsl
float2 animUV  = uv;
animUV.x      += sin(uv.y * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveAmplitude;
float2 tiledUV = animUV * _Tiling;
float  checker = fmod(floor(tiledUV.x) + floor(tiledUV.y), 2.0);
half3  col     = checker > 0.5 ? half3(1,1,1) : half3(0,0,0);
return half4(col, _Alpha);
```

**Render state:** `Blend SrcAlpha OneMinusSrcAlpha`, `ZWrite Off`, `Cull Off` (double-sided), Queue `Transparent`.

### Scene setup
- A Unity **Plane** GameObject (`FinishFlagPlane`) parented to or placed just in front of the FinishLine trigger box.
- Rotated 90° on X so it faces the player (plane's normal points down the Z axis toward the camera).
- Scale adjusted to cover the visible playfield width (e.g. `(0.5, 1, 0.3)` depending on desired size).
- MeshRenderer uses a new Material `FinishFlagMat` with `MAUVE/FinishFlag` shader.
- No script — animation is driven entirely by `_Time` in the shader.

---

## Out of Scope
- No audio cue on finish (FMOD not wired here).
- No score tally animation or fanfare — score display reuses existing `UIGameOver`.
- No changes to the death gameover flow.
