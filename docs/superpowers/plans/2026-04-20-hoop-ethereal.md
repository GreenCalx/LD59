# Hoop Ethereal Shader & Feedback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an ethereal Fresnel rim shader to hoops with a next-hoop highlight, VFX/SFX callback on collection, a procedural dissolve on collection, and a perfect-chain SFX.

**Architecture:** A single URP HLSL shader (`HoopEthereal`) drives all visual states via two floats (`_HighlightT`, `_DissolveT`). A `HoopVisual` component on each hoop owns a `MaterialPropertyBlock` to animate those floats independently per hoop without allocations. `HoopChain` gains ordered-index tracking to highlight the next unconsumed hoop; `HoopDetector` gains a `UnityEvent` and FMOD `EventReference` for designer-wirable collection feedback.

**Tech Stack:** Unity 6 URP, URP HLSL (Core.hlsl), FMOD for Unity (`FMODUnity` namespace), Unity New Input System (unchanged), C# coroutines for dissolve animation.

---

## File Map

| File | Action |
|------|--------|
| `SpaceRider/Assets/#GAME/GFX/Shaders/HoopEthereal.shader` | Create |
| `SpaceRider/Assets/#GAME/Scripts/HoopVisual.cs` | Create |
| `SpaceRider/Assets/#GAME/Scripts/HoopChain.cs` | Modify |
| `SpaceRider/Assets/#GAME/Scripts/HoopDetector.cs` | Modify |
| Material + prefab wiring | Unity Editor (manual step) |

---

## Task 1: Write `HoopEthereal.shader`

**Files:**
- Create: `SpaceRider/Assets/#GAME/GFX/Shaders/HoopEthereal.shader`

- [ ] **Step 1: Create the shader file**

Write the following content to `SpaceRider/Assets/#GAME/GFX/Shaders/HoopEthereal.shader`:

```hlsl
Shader "MAUVE/HoopEthereal"
{
    Properties
    {
        _BaseRimColor        ("Base Rim Color",        Color)            = (0.1, 0.4, 1.0, 1)
        _HighlightRimColor   ("Highlight Rim Color",   Color)            = (0.6, 0.9, 1.0, 1)
        _RimPower            ("Rim Power",             Range(1, 8))      = 3.0
        _RimIntensity        ("Rim Intensity",         Range(0, 4))      = 1.5
        _PulseSpeed          ("Pulse Speed",           Range(0, 5))      = 1.0
        _HighlightPulseSpeed ("Highlight Pulse Speed", Range(0, 5))      = 3.0
        _HighlightT          ("Highlight T",           Range(0, 1))      = 0
        _DissolveT           ("Dissolve T",            Range(0, 1))      = 0
        _FringeWidth         ("Fringe Width",          Range(0.01, 0.5)) = 0.1
        _NoiseScale          ("Noise Scale",           Range(0.1, 10))   = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "HoopEthereal"
            Blend  One One
            ZWrite Off
            Cull   Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseRimColor;
                half4  _HighlightRimColor;
                float  _RimPower;
                float  _RimIntensity;
                float  _PulseSpeed;
                float  _HighlightPulseSpeed;
                float  _HighlightT;
                float  _DissolveT;
                float  _FringeWidth;
                float  _NoiseScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 positionOS  : TEXCOORD2;
            };

            // Deterministic hash noise from object-space position.
            // Returns 0-1 pseudo-random value stable per vertex/fragment position.
            float Hash(float3 p)
            {
                p = frac(p * float3(443.897, 441.423, 437.195));
                p += dot(p, p.yzx + 19.19);
                return frac((p.x + p.y) * p.z);
            }

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionOS  = IN.positionOS.xyz;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // --- Dissolve clip ---
                float noiseVal  = Hash(IN.positionOS * _NoiseScale);
                float threshold = _DissolveT;
                clip(noiseVal - threshold);

                // --- Fresnel rim ---
                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float  NdotV     = saturate(dot(normalWS, viewDirWS));
                float  fresnel   = pow(1.0 - NdotV, _RimPower);

                // --- Breathing pulse (lerps speed between normal/highlight) ---
                float pulseSpeed = lerp(_PulseSpeed, _HighlightPulseSpeed, _HighlightT);
                float pulse      = sin(_Time.y * pulseSpeed) * 0.3 + 0.7; // range 0.4–1.0

                // --- Color lerp between normal and highlight ---
                half3 rimColor = lerp(_BaseRimColor.rgb, _HighlightRimColor.rgb, _HighlightT);

                // --- Dissolve fringe: bright edge at clip boundary ---
                float fringe = saturate(1.0 - (noiseVal - threshold) / max(_FringeWidth, 0.001));
                fringe *= step(0.001, _DissolveT); // suppress fringe when not dissolving

                // --- Composite ---
                half3 col = rimColor * fresnel * _RimIntensity * pulse;
                col      += rimColor * fringe * 2.0;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
```

- [ ] **Step 2: Verify shader compiles in Unity**

Save the file, switch to the Unity Editor, and wait for recompilation. Open **Window > Analysis > Shader Variant Collection** (or just create a material using this shader — see Task 5). The Console should show zero errors. If there are HLSL errors, check the console message for line numbers and fix accordingly.

- [ ] **Step 3: Commit**

```bash
git add SpaceRider/Assets/#GAME/GFX/Shaders/HoopEthereal.shader
git commit -m "feat: add HoopEthereal URP shader with Fresnel rim, highlight, and dissolve"
```

---

## Task 2: Write `HoopVisual.cs`

**Files:**
- Create: `SpaceRider/Assets/#GAME/Scripts/HoopVisual.cs`

`HoopVisual` sits on the hoop prefab root. It drives `_HighlightT` and `_DissolveT` on a `MaterialPropertyBlock`, keeping all hoop instances independent while sharing one material asset.

- [ ] **Step 1: Create the script**

Write the following to `SpaceRider/Assets/#GAME/Scripts/HoopVisual.cs`:

```csharp
using System.Collections;
using UnityEngine;

public class HoopVisual : MonoBehaviour
{
    [SerializeField] float dissolveDuration = 0.35f;

    static readonly int HighlightTID = Shader.PropertyToID("_HighlightT");
    static readonly int DissolveTID  = Shader.PropertyToID("_DissolveT");

    MeshRenderer          _renderer;
    MaterialPropertyBlock _block;
    bool                  _isDissolving;

    void Awake()
    {
        // Use GetComponentInChildren in case the mesh sits on a child object.
        _renderer = GetComponentInChildren<MeshRenderer>();
        _block    = new MaterialPropertyBlock();
        if (_renderer != null)
            ApplyBlock();
    }

    /// <summary>
    /// Toggle the next-in-chain highlight. Called by HoopChain.
    /// </summary>
    public void SetHighlight(bool on)
    {
        if (_renderer == null) return;
        _block.SetFloat(HighlightTID, on ? 1f : 0f);
        _renderer.SetPropertyBlock(_block);
    }

    /// <summary>
    /// Begin the dissolve animation. Safe to call multiple times (no-ops after first).
    /// After dissolveDuration seconds, the hoop GameObject is destroyed.
    /// </summary>
    public void TriggerDissolve()
    {
        if (_isDissolving) return;
        _isDissolving = true;
        StartCoroutine(DissolveRoutine());
    }

    IEnumerator DissolveRoutine()
    {
        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            _block.SetFloat(DissolveTID, Mathf.Clamp01(elapsed / dissolveDuration));
            _renderer.SetPropertyBlock(_block);
            yield return null;
        }
        Destroy(gameObject);
    }

    // Initialises the property block so the hoop starts in the normal state.
    void ApplyBlock()
    {
        _block.SetFloat(HighlightTID, 0f);
        _block.SetFloat(DissolveTID,  0f);
        _renderer.SetPropertyBlock(_block);
    }
}
```

- [ ] **Step 2: Verify compilation**

Switch to Unity Editor and confirm zero errors in the Console.

- [ ] **Step 3: Commit**

```bash
git add SpaceRider/Assets/#GAME/Scripts/HoopVisual.cs
git commit -m "feat: add HoopVisual MaterialPropertyBlock driver with dissolve coroutine"
```

---

## Task 3: Modify `HoopChain.cs`

**Files:**
- Modify: `SpaceRider/Assets/#GAME/Scripts/HoopChain.cs`

Adds ordered hoop tracking, next-hoop highlight management, and perfect chain SFX. `RegisterMiss` now also advances the highlight index so the beacon always moves forward regardless of pass/miss.

- [ ] **Step 1: Replace HoopChain.cs**

Write the following to `SpaceRider/Assets/#GAME/Scripts/HoopChain.cs`:

```csharp
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class HoopChain : MonoBehaviour
{
    [SerializeField] EventReference PerfectChainSound;

    int                 _total;
    int                 _passed;
    int                 _resolved;
    int                 _nextIndex;
    List<HoopDetector>  _hoops = new();

    void Start()
    {
        // Build list sorted by sibling index so order matches spline placement.
        var detectors = GetComponentsInChildren<HoopDetector>();
        _hoops = new List<HoopDetector>(detectors);
        _hoops.Sort((a, b) =>
            a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        _total = _hoops.Count;
        Debug.Log($"[HoopChain] {gameObject.name} — {_total} hoops");

        if (_total > 0)
            _hoops[0].GetComponent<HoopVisual>()?.SetHighlight(true);
    }

    // Clears highlight on the current hoop, advances index, highlights the next one.
    void AdvanceHighlight()
    {
        if (_nextIndex < _hoops.Count)
            _hoops[_nextIndex].GetComponent<HoopVisual>()?.SetHighlight(false);

        _nextIndex++;

        if (_nextIndex < _hoops.Count)
            _hoops[_nextIndex].GetComponent<HoopVisual>()?.SetHighlight(true);
    }

    public void RegisterPass()
    {
        AdvanceHighlight();
        _passed++;
        HoopTracker.Instance?.NotifyPass();
        HoopTracker.Instance?.UpdateChain(_passed, _total);
        Resolve();
    }

    public void RegisterMiss()
    {
        AdvanceHighlight();
        Resolve();
    }

    void Resolve()
    {
        _resolved++;
        Debug.Log($"[HoopChain] {gameObject.name} — resolved {_resolved}/{_total}, passed {_passed}");
        if (_resolved < _total) return;

        bool perfect = _passed == _total && _total > 0;
        int  score   = perfect ? _total * 2 : _passed;
        Debug.Log($"[HoopChain] {gameObject.name} — chain complete! score={score} perfect={perfect}");

        if (perfect && !PerfectChainSound.IsNull)
            RuntimeManager.PlayOneShot(PerfectChainSound, transform.position);

        HoopTracker.Instance?.AddScore(score);
        HoopTracker.Instance?.NotifyChainComplete(perfect);
        HoopTracker.Instance?.ClearChain();
    }
}
```

- [ ] **Step 2: Verify compilation**

Switch to Unity Editor. Console must show zero errors. If `EventReference` is not found, confirm the FMOD for Unity package is installed via **Window > Package Manager**.

- [ ] **Step 3: Commit**

```bash
git add SpaceRider/Assets/#GAME/Scripts/HoopChain.cs
git commit -m "feat: add ordered highlight tracking and perfect chain SFX to HoopChain"
```

---

## Task 4: Modify `HoopDetector.cs`

**Files:**
- Modify: `SpaceRider/Assets/#GAME/Scripts/HoopDetector.cs`

Adds a `UnityEvent` for VFX wiring, a FMOD `EventReference` for per-hoop collection SFX, and triggers the `HoopVisual` dissolve on pass. Note: `AdvanceHighlight` is now called inside `HoopChain.RegisterPass()`, so by the time `TriggerDissolve` fires the highlight has already been cleared on this hoop.

- [ ] **Step 1: Replace HoopDetector.cs**

Write the following to `SpaceRider/Assets/#GAME/Scripts/HoopDetector.cs`:

```csharp
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;

public class HoopDetector : MonoBehaviour
{
    [SerializeField] UnityEvent    OnHoopCollected;
    [SerializeField] EventReference CollectSound;

    public bool IsConsumed { get; private set; }

    void OnTriggerStay(Collider iCollider)
    {
        if (IsConsumed) return;
        if (iCollider.GetComponentInParent<Surfer>() == null) return;

        IsConsumed = true;
        enabled    = false;

        // 1. Advance chain highlight + score accounting.
        GetComponentInParent<HoopChain>()?.RegisterPass();

        // 2. Fire designer-wirable VFX callback.
        OnHoopCollected.Invoke();

        // 3. Play per-hoop collection SFX via FMOD.
        if (!CollectSound.IsNull)
            RuntimeManager.PlayOneShot(CollectSound, transform.position);

        // 4. Begin shader dissolve; destroys gameObject after dissolveDuration.
        GetComponent<HoopVisual>()?.TriggerDissolve();

        Debug.Log($"[HoopDetector] pass on {transform.parent.name}");
    }

    public void ForceConsume()
    {
        IsConsumed = true;
        enabled    = false;
    }
}
```

- [ ] **Step 2: Verify compilation**

Switch to Unity Editor. Console must show zero errors.

- [ ] **Step 3: Commit**

```bash
git add SpaceRider/Assets/#GAME/Scripts/HoopDetector.cs
git commit -m "feat: add UnityEvent VFX callback, FMOD SFX, and dissolve trigger to HoopDetector"
```

---

## Task 5: Create Material & Wire Up Hoop Prefab

All code compiles; now connect everything in the Unity Editor.

- [ ] **Step 1: Create the HoopEthereal material**

In the Unity Editor **Project** panel:
- Navigate to `Assets/#GAME/GFX/Materials/`
- Right-click → **Create > Material**
- Name it `HoopEthereal`
- In the Inspector, click the **Shader** dropdown and select **MAUVE > HoopEthereal**
- Tweak defaults to taste (start with: Base Rim Color = `#1A66FF`, Highlight Rim Color = `#99DDFF`, Rim Power = `3`, Rim Intensity = `1.5`, Pulse Speed = `1`, Highlight Pulse Speed = `3`, Fringe Width = `0.1`, Noise Scale = `2`)

- [ ] **Step 2: Open the hoop prefab**

In the Project panel, double-click `Assets/#GAME/Prefabs/Hoops.prefab` to enter prefab edit mode.

- [ ] **Step 3: Assign the material**

Select the GameObject that holds the `MeshRenderer` (root or first child). In the Inspector under **Materials**, replace the existing material slot with `HoopEthereal`.

- [ ] **Step 4: Add HoopVisual component**

On the hoop prefab root (the same object that has `HoopDetector`):
- Click **Add Component** → search for **HoopVisual** → add it
- Leave `Dissolve Duration` at `0.35` (or adjust to taste)

- [ ] **Step 5: Save the prefab**

Click **Save** in the top-left of the prefab editor (or Ctrl+S). Exit prefab mode.

- [ ] **Step 6: Enter Play Mode and verify visual states**

Press Play. Observe a chain of hoops in the scene:
- All hoops should glow with a subtle breathing cyan-blue Fresnel rim
- The first hoop in the chain should be visibly brighter/whiter (highlight state)
- When the player passes through a hoop: the next hoop highlights, and the collected hoop dissolves away over ~0.35s with a bright fringe edge
- After all hoops in a chain: if perfect, `PerfectChainSound` fires (assign an FMOD event in the `HoopsSpline` prefab's `HoopChain` component to test)
- `CollectSound` and `OnHoopCollected` are empty by default — assign an FMOD event and/or a particle system `Play()` call to the `HoopDetector` on the prefab to test them

- [ ] **Step 7: Commit**

```bash
git add SpaceRider/Assets/#GAME/GFX/Materials/HoopEthereal.mat
git add SpaceRider/Assets/#GAME/GFX/Materials/HoopEthereal.mat.meta
git add SpaceRider/Assets/#GAME/Prefabs/Hoops.prefab
git commit -m "feat: wire HoopEthereal material and HoopVisual onto hoop prefab"
```
