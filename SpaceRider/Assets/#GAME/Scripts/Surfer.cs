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
