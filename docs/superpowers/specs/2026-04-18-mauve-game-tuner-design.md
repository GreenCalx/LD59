# MAUVE Game Tuner — Design Spec

**Date:** 2026-04-18
**Project:** SpaceRider (Ludum Dare 59)

---

## Overview

A centralized Unity Editor tool (`MAUVE/Game Tuner`) that consolidates all tunable game variables in one place. Values live in a hierarchy of ScriptableObject assets so any sub-config can be independently swapped (e.g. a debug camera config, an easy-mode wave config). A debug gizmos toggle in the window surfaces relevant scene-view visuals without touching the game's Play-mode code.

---

## Architecture

### Config Hierarchy

```
Assets/#GAME/Scripts/Config/
  GameConfig.cs           ← root SO — holds references to all sub-configs
  WaveInputConfig.cs
  WaveGeneratorConfig.cs
  LevelConfig.cs
  ProgressConfig.cs
  CameraConfig.cs
  SurferConfig.cs

Assets/#GAME/Config/Default/
  GameConfig_Default.asset
  WaveInput_Default.asset
  WaveGenerator_Default.asset
  Level_Default.asset
  Progress_Default.asset
  Camera_Default.asset
  Surfer_Default.asset
```

`GameConfig` is the root asset. It holds one reference per sub-config SO. Each sub-config is independently swappable — to try a different camera feel, swap only `CameraConfig` without touching anything else.

Adding a new tunable group in the future = new SO class + one reference field in `GameConfig`. No other changes required.

### Runtime Access

Every script that owns tunable values gains a single `[SerializeField] GameConfig config` field. Fields currently serialized directly on the component are replaced with reads from the appropriate sub-config:

```csharp
// Before
[SerializeField] float freqMin = 0.1f;

// After
float freqMin => config.waveInput.freqMin;
```

No local copies — scripts read from config directly so the asset is the single source of truth.

### LevelScope Simplification

`LevelScope` becomes a **pure runtime-state container**. Its design-time fields (`LevelLength`, `LookAhead`, `DecayLength`) move to `LevelConfig`. Any script needing these reads from `config.level.*`.

`LevelScope` retains only:
- `VirtualDistance` (written by ProgressDriver each frame)
- `ScrollSpeed` (written by ProgressDriver)
- `Progress01` (computed)
- `IsFinished` (computed)

---

## Sub-Config Field Inventory

### WaveInputConfig
| Field | Default | Notes |
|---|---|---|
| `freqMin` | 0.1 | Clamped lower bound |
| `freqMax` | 5.0 | Clamped upper bound |
| `ampMin` | 0.0 | |
| `ampMax` | 5.0 | |
| `freqInitial` | 1.0 | Value at game start |
| `ampInitial` | 1.0 | |
| `panInitial` | 0.0 | |
| `frequencyRate` | 0.5 | Units/sec input rate |
| `panRate` | 2.0 | |
| `amplitudeRate` | 1.0 | |

### WaveGeneratorConfig
| Field | Default | Notes |
|---|---|---|
| `sampleDensity` | 4 | Samples per unit Z |
| `paramSmoothingDistance` | 2 | Low-pass distance |
| `panLateralScale` | 0.2 | X offset per sample from pan |
| `bpm` | 120 | Phase clock |

### LevelConfig
| Field | Default | Notes |
|---|---|---|
| `levelLength` | 1000 | Total level Z length |
| `lookAhead` | 20 | Forward buffer past hero |
| `decayLength` | 10 | Back buffer behind hero |
| `playfieldRadius` | 8 | XY cylinder death boundary |

### ProgressConfig
| Field | Default | Notes |
|---|---|---|
| `baseScrollSpeed` | 10 | Units/sec base speed |
| `signalGain` | 0.05 | Derivative→speed multiplier |
| `minSpeedMultiplier` | 0.5 | |
| `maxSpeedMultiplier` | 2.0 | |

### CameraConfig
| Field | Default | Notes |
|---|---|---|
| `offset` | (0, 2, -8) | World-space offset from hero |
| `positionSmoothTime` | 0.18 | SmoothDamp time |
| `rollInfluence` | 0.35 | 0=no bank, 1=full roll |
| `lookSharpness` | 8 | Slerp speed toward look target |
| `baseFov` | 60 | Degrees |
| `fovSpeedScale` | 0.4 | Extra FOV per unit speed |
| `fovSmoothTime` | 0.25 | |

### SurferConfig
| Field | Default | Notes |
|---|---|---|
| `maxTiltDegrees` | 45 | Cap for pitch and roll |
| `alignToTangent` | true | Pitch/yaw from spline tangent |

---

## GameDebug Static Class

`Assets/#GAME/Scripts/GameDebug.cs` — plain runtime class, no editor dependency.

```csharp
public static class GameDebug
{
    public static bool ShowGizmos;
}
```

Scripts check `GameDebug.ShowGizmos` inside `OnDrawGizmos`. The MAUVE window button sets this flag. Resets on domain reload (edit-time only by design).

---

## MAUVE EditorWindow

**Menu path:** `MAUVE/Game Tuner`  
**File:** `Assets/#GAME/Editor/MauveGameTuner.cs`

### Layout

```
[ MAUVE Game Tuner                              ]
[ GameConfig asset field: [Default ▾]  [Ping]  ]
[ [⬤ Debug Gizmos ON]                          ]
─────────────────────────────────────────────────
▼ Wave Input      [Ping]
  (inline editor for WaveInputConfig)
▼ Wave Generator  [Ping]
  ...
▼ Level           [Ping]
  ...
▼ Progress        [Ping]
  ...
▼ Camera          [Ping]
  ...
▼ Surfer          [Ping]
  ...
─────────────────────────────────────────────────
[ Create Default Config ]   (shown if none assigned)
```

Each section draws the sub-config's default inspector via `Editor.CreateEditor(subConfig)`. Foldout state persists via `EditorPrefs`.

---

## Debug Gizmos

All gizmos are guarded by `if (!GameDebug.ShowGizmos) return;` and drawn in `OnDrawGizmos` (always visible, not just on selection).

| Gizmo | Owner Script | Visual |
|---|---|---|
| Playfield cylinder | `LevelScope` | `Gizmos.DrawWireMesh` cylinder at world origin, radius = `config.level.playfieldRadius` |
| Wave path | `WaveGenerator` | `Gizmos.DrawLineStrip` through all sample (x, y, worldZ) points |
| LookAhead zone | `WaveGenerator` | Yellow wire sphere at the lookahead boundary sample |
| DecayLength zone | `WaveGenerator` | Red wire sphere at the decay boundary sample |
| Hero contact + tangent | `Surfer` | Cyan sphere at wave contact point; arrow along tangent direction |
| Finish line label | `FinishLine` | `Handles.Label` at FinishLine world position showing distance to hero |

---

## Files Changed

### New
- `Assets/#GAME/Scripts/Config/GameConfig.cs`
- `Assets/#GAME/Scripts/Config/WaveInputConfig.cs`
- `Assets/#GAME/Scripts/Config/WaveGeneratorConfig.cs`
- `Assets/#GAME/Scripts/Config/LevelConfig.cs`
- `Assets/#GAME/Scripts/Config/ProgressConfig.cs`
- `Assets/#GAME/Scripts/Config/CameraConfig.cs`
- `Assets/#GAME/Scripts/Config/SurferConfig.cs`
- `Assets/#GAME/Scripts/GameDebug.cs`
- `Assets/#GAME/Editor/MauveGameTuner.cs`

### Modified
- `WaveInputController.cs` — swap individual fields for `config.waveInput.*`
- `WaveGenerator.cs` — swap fields + add gizmos
- `LevelScope.cs` — remove design-time fields
- `ProgressDriver.cs` — swap fields, read level data from `config.level.*`
- `SpaceRiderCamera.cs` — swap fields + add gizmos
- `Surfer.cs` — swap fields + add gizmos
- `FinishLine.cs` — read `config.level.levelLength`, add gizmo

---

## Out of Scope

- Runtime hot-reload of config (edit-time only for now)
- Per-level config overrides (future: GameConfig ref on a LevelManager)
- Undo/redo for the Debug Gizmos toggle (static bool, resets on reload anyway)
