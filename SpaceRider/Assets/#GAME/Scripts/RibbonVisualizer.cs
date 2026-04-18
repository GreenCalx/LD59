using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Generates a double-sided ribbon mesh along a <see cref="SplineContainer"/>.
/// Only emits geometry between <c>waveSource</c> and <c>surfer + decayLength</c>.
/// Vertex color alpha fades in the decay region past the surfer.
/// UVs: U = [0,1] across width, V = [0,1] along visible length.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RibbonVisualizer : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Material material;
    [SerializeField] private float width = 1f;
    [SerializeField, Min(2)] private int segments = 256;
    [Tooltip("World-space offset applied to the ribbon mesh (e.g. negative Y to place it under the surfer's feet)")]
    [SerializeField] private Vector3 ribbonOffset = Vector3.zero;

    [Header("Visibility")]
    [SerializeField] private Transform waveSource;
    [SerializeField] private Transform surfer;
    [SerializeField] private float decayLength = 5f;

    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    private void OnEnable()
    {
        _meshFilter   = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _mesh = new Mesh { name = "Ribbon" };
        _meshFilter.sharedMesh = _mesh;
    }

    private void OnDisable()
    {
        if (_mesh != null)
        {
            if (Application.isPlaying) Destroy(_mesh);
            else DestroyImmediate(_mesh);
            _mesh = null;
        }
    }

    private void LateUpdate()
    {
        if (material != null)
            _meshRenderer.sharedMaterial = material;

        if (splineContainer == null) return;
        var spline = splineContainer.Spline;
        if (spline == null || spline.Count < 2) return;

        RebuildMesh(spline);
    }

    private void RebuildMesh(Spline spline)
    {
        int N       = segments;
        float halfW = width * 0.5f;

        // --- Determine visible t-range [tMin .. tMax] ---
        // Ribbon runs: waveSource ---(opaque)--- surfer ---(fade)--- surfer+decay
        float tMin    = 0f;
        float tMax    = 1f;
        float tHero   = 0f;
        bool hasSurfer = surfer != null;

        float4x4 splineMtx = splineContainer.transform.localToWorldMatrix;
        float splineLen     = SplineUtility.CalculateLength(spline, splineMtx);

        if (waveSource != null)
        {
            float3 srcLocal = splineContainer.transform.InverseTransformPoint(waveSource.position);
            SplineUtility.GetNearestPoint(spline, srcLocal, out _, out tMax);
        }

        if (hasSurfer)
        {
            float3 surferLocal = splineContainer.transform.InverseTransformPoint(surfer.position);
            SplineUtility.GetNearestPoint(spline, surferLocal, out _, out tHero);

            float decayT = splineLen > 0f ? decayLength / splineLen : 0f;
            tMin = math.max(0f, tHero - decayT);
        }

        // Ensure valid range
        if (tMin >= tMax) return;

        // --- Allocate ---
        int vertCount = N * 4;
        int triCount  = (N - 1) * 12;

        var verts  = new Vector3[vertCount];
        var norms  = new Vector3[vertCount];
        var uvs    = new Vector2[vertCount];
        var colors = new Color[vertCount];
        var tris   = new int[triCount];

        // --- Vertices ---
        for (int i = 0; i < N; i++)
        {
            float s = i / (N - 1f);                    // 0 → 1 over visible range
            float t = math.lerp(tMin, tMax, s);         // actual spline parameter
            SplineUtility.Evaluate(spline, t, out float3 pos, out float3 tan, out float3 _);

            // Flat ribbon: right from world-up × forward (no banking)
            float3 fwd = math.normalizesafe(tan, new float3(0, 0, 1));
            float3 right = math.cross(new float3(0, 1, 0), fwd);
            if (math.lengthsq(right) < 0.0001f)
                right = math.cross(new float3(0, 0, 1), fwd);
            right = math.normalizesafe(right, new float3(1, 0, 0));
            float3 up = math.cross(fwd, right);

            Vector3 l = (Vector3)(pos - right * halfW) + ribbonOffset;
            Vector3 r = (Vector3)(pos + right * halfW) + ribbonOffset;
            Vector3 n = (Vector3)up;

            // Alpha: decay zone [tMin .. tHero] fades 0→1, opaque after tHero
            float alpha = 1f;
            if (hasSurfer && t < tHero && tHero > tMin)
                alpha = (t - tMin) / (tHero - tMin);

            Color col = new Color(1f, 1f, 1f, alpha);

            // Front face
            int fi = i * 2;
            verts[fi]  = l;            verts[fi + 1]  = r;
            norms[fi]  = n;            norms[fi + 1]  = n;
            uvs[fi]    = new Vector2(0f, s);
            uvs[fi + 1] = new Vector2(1f, s);
            colors[fi] = col;          colors[fi + 1] = col;

            // Back face (offset by 2N, mirrored U)
            int bi = N * 2 + i * 2;
            verts[bi]  = l;            verts[bi + 1]  = r;
            norms[bi]  = -n;           norms[bi + 1]  = -n;
            uvs[bi]    = new Vector2(1f, s);
            uvs[bi + 1] = new Vector2(0f, s);
            colors[bi] = col;          colors[bi + 1] = col;
        }

        // --- Triangles ---
        int idx = 0;
        for (int i = 0; i < N - 1; i++)
        {
            int f0 = i * 2, f1 = f0 + 1, f2 = f0 + 2, f3 = f0 + 3;

            // Front face
            tris[idx++] = f0; tris[idx++] = f3; tris[idx++] = f1;
            tris[idx++] = f0; tris[idx++] = f2; tris[idx++] = f3;

            // Back face (reversed winding)
            int b0 = N * 2 + i * 2, b1 = b0 + 1, b2 = b0 + 2, b3 = b0 + 3;
            tris[idx++] = b0; tris[idx++] = b1; tris[idx++] = b3;
            tris[idx++] = b0; tris[idx++] = b3; tris[idx++] = b2;
        }

        _mesh.Clear();
        _mesh.vertices  = verts;
        _mesh.normals   = norms;
        _mesh.uv        = uvs;
        _mesh.colors    = colors;
        _mesh.triangles = tris;
        _mesh.RecalculateBounds();
    }
}
