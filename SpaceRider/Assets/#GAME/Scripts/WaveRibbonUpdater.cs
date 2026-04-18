using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Drives a <see cref="SplineContainer"/> from a <see cref="WaveGenerator"/> every frame.
/// Add this component alongside <see cref="SplineContainer"/> and (optionally)
/// <see cref="SplineExtrude"/> on the ribbon GameObject. The SplineExtrude will
/// auto-rebuild its mesh whenever the spline changes.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SplineContainer))]
public class WaveRibbonUpdater : MonoBehaviour
{
    [SerializeField] private WaveGenerator waveGenerator;
    [SerializeField, Min(2)] private int resolution = 64;

    private SplineContainer _splineContainer;
    private SplineExtrude   _splineExtrude;

    private void OnEnable()
    {
        _splineContainer = GetComponent<SplineContainer>();
        _splineExtrude   = GetComponent<SplineExtrude>();
    }

    private void Update()
    {
        if (waveGenerator == null) return;
        RebuildSpline();
    }

    private void RebuildSpline()
    {
        var points = waveGenerator.GetWavePoints(resolution, Allocator.Temp);

        var spline = _splineContainer.Spline;
        spline.Clear();
        for (int i = 0; i < resolution; i++)
            spline.Add(
                new BezierKnot(new float3(points[i].x, points[i].y, points[i].z)),
                TangentMode.AutoSmooth
            );

        points.Dispose();

        // SplineExtrude listens to Spline.Changed, but an explicit Rebuild() ensures
        // the mesh is up to date within the same frame.
        _splineExtrude?.Rebuild();
    }
}
