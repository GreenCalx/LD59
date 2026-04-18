using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Hero is pinned to local Z = 0. Each frame XY is snapped onto the wave
/// spline at the fixed parameter t = decayLength / (decayLength + lookAhead).
///
/// When alignToTangent is on, the hero's orientation comes from two sources:
///   - Pitch + yaw from the spline tangent at the hero (i.e. surfer follows the wave).
///   - Roll around local Z from <see cref="WaveGenerator.Pan"/> (banking into turns).
///
/// Both pitch and roll are capped at ±<see cref="maxTiltDegrees"/> so the hero
/// never lies flat on a steep wave nor rolls past a readable silhouette.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class Surfer : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private LevelScope      levelScope;
    [SerializeField] private WaveGenerator   waveGenerator;

    [Header("Orientation")]
    [SerializeField] private bool alignToTangent = true;
    [Tooltip("Cap applied to both pitch (from wave slope) and roll (from pan).")]
    [SerializeField, Range(0f, 90f)] private float maxTiltDegrees = 45f;

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

        Vector3 parentLocal = transform.parent != null
            ? transform.parent.InverseTransformPoint(worldPos)
            : worldPos;
        parentLocal.z = 0f;
        transform.position = transform.parent != null
            ? transform.parent.TransformPoint(parentLocal)
            : parentLocal;

        if (!alignToTangent) return;

        Vector3 worldFwd = splineContainer.transform.TransformDirection(
            (Vector3)math.normalizesafe(tan, new float3(0, 0, 1)));
        if (worldFwd.sqrMagnitude <= 1e-6f) return;
        worldFwd.Normalize();

        // --- Pitch cap ---
        // Decompose forward into horizontal (XZ) + vertical (Y). Clamp the sine of
        // the pitch angle so the hero never tilts forward/back past maxTiltDegrees.
        Vector3 flatFwd = new Vector3(worldFwd.x, 0f, worldFwd.z);
        if (flatFwd.sqrMagnitude <= 1e-6f) return;
        flatFwd.Normalize();

        float maxSin    = Mathf.Sin(maxTiltDegrees * Mathf.Deg2Rad);
        float sinPitch  = Mathf.Clamp(worldFwd.y, -maxSin, +maxSin);
        float cosPitch  = Mathf.Sqrt(Mathf.Max(0f, 1f - sinPitch * sinPitch));
        Vector3 cappedFwd = flatFwd * cosPitch + Vector3.up * sinPitch;

        Quaternion yaw = Quaternion.LookRotation(cappedFwd, Vector3.up);

        // --- Roll cap ---
        // Use the ribbon's actual lateral slope at the hero (already clamped to
        // [-1, 1] by the generator), so banking follows the wave under the
        // hero's feet rather than the current input. With the propagating
        // model, input changes reach the hero only as those samples scroll in.
        float pan = waveGenerator != null ? waveGenerator.GetEffectivePanAtHero() : 0f;
        Quaternion roll = Quaternion.AngleAxis(-pan * maxTiltDegrees, Vector3.forward);

        transform.rotation = yaw * roll;
    }
}
