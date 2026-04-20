# Hoop Ethereal Shader & Feedback Design

**Date:** 2026-04-20  
**Project:** SpaceRider (LD59)  
**Scope:** Hoop visual shader, next-hoop highlight, collection VFX/SFX callback, dissolve effect, perfect chain SFX

---

## Context

Hoops are thin torus meshes (low-poly NURBS circle from Blender) placed along splines via `HoopCurvePath`. They are grouped into chains via `HoopChain`. The game has a Jet Set Radio Future / Silver Surfer in space aesthetic. Dominant colors are deep space black (60%), orange (30%, sun + plasma shader below the level), and blue (10%, hero). Hoops should occupy the blue 10% — ethereal, readable, impactful, not detail-heavy.

---

## 1. Shader — `HoopEthereal.shader`

**Location:** `Assets/#GAME/GFX/Shaders/HoopEthereal.shader`  
**Style:** URP HLSL (matches existing BoundaryWall, BandpassRetro shaders). Additive blend, ZWrite Off.

### Visual States

All states are driven by two float properties animated by `HoopVisual.cs`:

| Property | Range | Meaning |
|----------|-------|---------|
| `_HighlightT` | 0–1 | 0 = normal, 1 = next-in-chain highlight |
| `_DissolveT` | 0–1 | 0 = intact, 1 = fully dissolved |

**Normal state** (`_HighlightT=0`, `_DissolveT=0`):  
Fresnel rim glow (`pow(1 - NdotV, _RimPower)`) in deep cyan-blue. The silhouette edge glows, the face stays near-transparent (additive so black face = invisible). A slow sine pulse (`sin(time * _PulseSpeed)`) breathes the rim intensity gently.

**Highlight state** (`_HighlightT=1`):  
Rim lerps from `_BaseRimColor` to `_HighlightRimColor` (brighter, more saturated blue-white). Pulse frequency increases. Achieved by lerping colors and pulse speed in the fragment shader — no branching.

**Dissolve state** (`_DissolveT` 0→1):  
Procedural noise (layered sin-based, no texture required) drives a clip threshold. Fragments dissolve inward as `_DissolveT` rises. A thin emissive fringe at the clip boundary (`abs(noise - threshold) < _FringeWidth`) glows in rim color — energy breaking apart visual.

### Inspector Properties

All exposed and tweakable:
- `_BaseRimColor` — normal state rim color (default: deep cyan-blue)
- `_HighlightRimColor` — next-hoop state rim color (default: bright blue-white)
- `_RimPower` — Fresnel falloff sharpness
- `_PulseSpeed` — breathing frequency (normal state)
- `_HighlightPulseSpeed` — breathing frequency (highlight state)
- `_DissolveT` — driven by HoopVisual at runtime
- `_HighlightT` — driven by HoopVisual at runtime
- `_FringeWidth` — dissolve edge glow thickness

---

## 2. `HoopVisual.cs`

**Location:** `Assets/#GAME/Scripts/HoopVisual.cs`  
**Placed on:** each hoop prefab root (same GameObject as `HoopDetector`)

### Responsibilities

- Owns a `MaterialPropertyBlock` applied to the hoop's `MeshRenderer` each frame — all hoops share one material asset, animate independently, zero extra allocations.
- `SetHighlight(bool)` — sets `_HighlightT` to 0 or 1 on the property block.
- `TriggerDissolve()` — starts a coroutine that lerps `_DissolveT` from 0→1 over `dissolveDuration` (default 0.35s, serialized), then calls `Destroy(gameObject)`.

### Serialized Fields

```csharp
[SerializeField] float dissolveDuration = 0.35f;
```

---

## 3. Chain Ordering & Highlight Logic — `HoopChain.cs` (modified)

`HoopChain` currently counts children without tracking order. Changes:

- At `Start()`, build `List<HoopDetector> _hoops` sorted by sibling index (spline placement order).
- Track `int _nextIndex = 0`.
- At `Start()`, call `_hoops[0].GetComponent<HoopVisual>()?.SetHighlight(true)` to mark first hoop.
- `RegisterPass()` and `RegisterMiss()` both call `AdvanceHighlight()` before their existing logic.
- `AdvanceHighlight()`: clears highlight on `_hoops[_nextIndex]`, increments `_nextIndex`, sets highlight on `_hoops[_nextIndex]` if it exists.

This means the highlight always tracks the next unconsumed hoop regardless of pass or miss outcome.

---

## 4. Collection Callback & SFX — `HoopDetector.cs` (modified)

Two new serialized fields:

```csharp
[SerializeField] UnityEvent            OnHoopCollected;
[SerializeField] FMODUnity.EventReference CollectSound;
```

On trigger pass (after `RegisterPass()` is called):
1. `OnHoopCollected.Invoke()` — designer-wirable VFX hook in inspector
2. `FMODUnity.RuntimeManager.PlayOneShot(CollectSound, transform.position)` — FMOD one-shot
3. `GetComponent<HoopVisual>()?.TriggerDissolve()` — start dissolve

The `UnityEvent` is the VFX entry point — no code change needed to add/swap particle effects.

---

## 5. Perfect Chain SFX — `HoopChain.cs` (modified)

One additional serialized field:

```csharp
[SerializeField] FMODUnity.EventReference PerfectChainSound;
```

In `Resolve()`, when `perfect == true`, plays `PerfectChainSound` as a one-shot at the chain's world position alongside the existing `NotifyChainComplete(true)` call.

---

## Files Changed

| File | Change |
|------|--------|
| `Assets/#GAME/GFX/Shaders/HoopEthereal.shader` | **New** — URP HLSL ethereal shader |
| `Assets/#GAME/Scripts/HoopVisual.cs` | **New** — MaterialPropertyBlock driver + dissolve coroutine |
| `Assets/#GAME/Scripts/HoopChain.cs` | **Modified** — ordered hoop list, highlight tracking, PerfectChainSound |
| `Assets/#GAME/Scripts/HoopDetector.cs` | **Modified** — OnHoopCollected UnityEvent, CollectSound FMOD event, dissolve trigger |

**Untouched:** `HoopTracker`, `HoopCurvePath`, `HoopCurvePathEditor`, `MissDetector`, `HoopCollector`, all other systems.
