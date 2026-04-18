using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Keeps the surfer riding on the wave spline.
/// The surfer's Z position is authoritative (set by gameplay or editor).
/// Each frame, XY are snapped to the spline so the surfer follows the wave surface.
/// Changing <see cref="LevelScope.LevelLength"/> does not move the surfer.
/// </summary>
[ExecuteAlways]
public class Surfer : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private LevelScope levelScope;

    private void Update()
    {
        if (splineContainer == null || levelScope == null) return;
        var spline = splineContainer.Spline;
        if (spline == null || spline.Count < 2) return;

        float levelLen = levelScope.LevelLength;
        if (levelLen <= 0f) return;

        // Convert world Z to spline parameter
        float t = Mathf.Clamp01(transform.position.z / levelLen);

        SplineUtility.Evaluate(spline, t, out float3 pos, out float3 tan, out float3 _);

        // Snap XY to spline, keep Z
        Vector3 worldPos = splineContainer.transform.TransformPoint(pos);
        transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);

        // Align forward along the tangent
        float3 fwd = math.normalizesafe(tan, new float3(0, 0, 1));
        if (math.lengthsq(fwd) > 0.0001f)
            transform.rotation = Quaternion.LookRotation(
                splineContainer.transform.TransformDirection(fwd),
                Vector3.up
            );
    }
}
