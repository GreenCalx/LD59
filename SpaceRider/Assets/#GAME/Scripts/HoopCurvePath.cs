using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[RequireComponent(typeof(SplineContainer))]
public class HoopCurvePath : MonoBehaviour
{
    [System.Serializable]
    public struct SplineSettings
    {
        public int   hoopCount;
        public float spacing;
    }

    [SerializeField] private GameObject     hoopPrefab;
    [SerializeField] private int            defaultHoopCount = 5;
    [SerializeField] private float          defaultSpacing   = 10f;
    [SerializeField] private List<SplineSettings> perSpline = new();

    private SplineContainer _container;
    private SplineContainer Container => _container != null ? _container : (_container = GetComponent<SplineContainer>());

    public SplineContainer SplineContainer => Container;

    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        ClearChildren();
        if (hoopPrefab == null || Container == null) return;

        SyncPerSplineList();

        for (int si = 0; si < Container.Splines.Count; si++)
            PlaceHoops(Container.Splines[si], perSpline[si]);
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (Application.isPlaying) Destroy(c.gameObject);
            else                       DestroyImmediate(c.gameObject);
        }
    }

    public void SyncPerSplineList()
    {
        int count = Container != null ? Container.Splines.Count : 0;
        while (perSpline.Count < count)
            perSpline.Add(new SplineSettings { hoopCount = defaultHoopCount, spacing = defaultSpacing });
        while (perSpline.Count > count)
            perSpline.RemoveAt(perSpline.Count - 1);
    }

    void PlaceHoops(Spline spline, SplineSettings cfg)
    {
        float length = SplineUtility.CalculateLength(spline, transform.localToWorldMatrix);
        if (length <= 0f) return;

        int placed = 0;
        for (int i = 0; i < cfg.hoopCount; i++)
        {
            float dist = i * cfg.spacing;
            if (dist > length) break;

            float t = dist / length;
            SplineUtility.Evaluate(spline, t, out float3 localPos, out float3 localTangent, out float3 localUp);

            Vector3 worldPos     = transform.TransformPoint((Vector3)localPos);
            Vector3 worldTangent = transform.TransformDirection((Vector3)localTangent);
            Vector3 worldUp      = transform.TransformDirection((Vector3)localUp);

            Quaternion rot = worldTangent.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(worldTangent.normalized, worldUp.normalized)
                : Quaternion.identity;

            Instantiate(hoopPrefab, worldPos, rot, transform);
            placed++;
        }
    }

    void OnDrawGizmos()
    {
        if (Container == null) return;
        SyncPerSplineList();

        for (int si = 0; si < Container.Splines.Count; si++)
        {
            var spline = Container.Splines[si];
            var cfg    = perSpline[si];
            float length = SplineUtility.CalculateLength(spline, transform.localToWorldMatrix);
            if (length <= 0f) continue;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < cfg.hoopCount; i++)
            {
                float dist = i * cfg.spacing;
                if (dist > length) break;

                float t = dist / length;
                SplineUtility.Evaluate(spline, t, out float3 lp, out float3 lt, out float3 lu);
                Vector3 wp = transform.TransformPoint((Vector3)lp);
                Gizmos.DrawWireSphere(wp, 0.4f);
            }
        }
    }
}
