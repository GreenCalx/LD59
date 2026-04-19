using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CylinderBoundaryVisual : MonoBehaviour
{
    [SerializeField] private GameConfig config;
    [SerializeField] private Transform  hero;

    private MeshFilter            _filter;
    private MeshRenderer          _renderer;
    private MaterialPropertyBlock _mpb;
    private Mesh                  _mesh;

    private static readonly int ProximityTId   = Shader.PropertyToID("_ProximityT");
    private static readonly int HeroWorldPosId = Shader.PropertyToID("_HeroWorldPos");
    private static readonly int DrawRadiusId   = Shader.PropertyToID("_DrawRadius");

    private void Awake()
    {
        _filter   = GetComponent<MeshFilter>();
        _renderer = GetComponent<MeshRenderer>();
        _mpb      = new MaterialPropertyBlock();
    }

    private void Start() => RebuildMesh();

    public void SetProximityT(float t)
    {
        if (_renderer == null) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(ProximityTId, Mathf.Clamp01(t));
        if (hero != null)
            _mpb.SetVector(HeroWorldPosId, hero.position);
        if (config?.boundary != null)
            _mpb.SetFloat(DrawRadiusId, config.boundary.drawRadius);
        _renderer.SetPropertyBlock(_mpb);
    }

    private void RebuildMesh()
    {
        if (_filter == null)   _filter   = GetComponent<MeshFilter>();
        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
        if (config?.level == null || config?.boundary == null) return;

        float radius = config.level.playfieldRadius;
        float length = config.level.levelLength;
        int   around = Mathf.Max(3, config.boundary.cylinderSegmentsAround);
        int   along  = Mathf.Max(2, config.boundary.cylinderSegmentsAlong);

        // +1 on around so first/last column share world position but get uv 0 and 1
        int vAround = around + 1;
        int vAlong  = along  + 1;

        var vertices  = new Vector3[vAround * vAlong];
        var uvs       = new Vector2[vAround * vAlong];
        var triangles = new int[around * along * 6];

        for (int j = 0; j <= along; j++)
        {
            float v = j / (float)along;
            float z = v * length;
            for (int i = 0; i <= around; i++)
            {
                float u     = i / (float)around;
                float angle = u * Mathf.PI * 2f;
                int   idx   = j * vAround + i;
                vertices[idx] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, z);
                uvs[idx]      = new Vector2(u, v);
            }
        }

        int t = 0;
        for (int j = 0; j < along; j++)
        {
            for (int i = 0; i < around; i++)
            {
                int v00 = j * vAround + i;
                int v10 = v00 + 1;
                int v01 = v00 + vAround;
                int v11 = v01 + 1;
                triangles[t++] = v00; triangles[t++] = v10; triangles[t++] = v01;
                triangles[t++] = v10; triangles[t++] = v11; triangles[t++] = v01;
            }
        }

        if (_mesh != null)
        {
            if (Application.isPlaying) Destroy(_mesh);
            else DestroyImmediate(_mesh);
        }

        _mesh = new Mesh { name = "CylinderBoundary" };
        _mesh.vertices  = vertices;
        _mesh.uv        = uvs;
        _mesh.triangles = triangles;
        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();
        _filter.sharedMesh = _mesh;
    }

    public void BakeInEditor()
    {
        if (_filter == null)   _filter   = GetComponent<MeshFilter>();
        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
        RebuildMesh();
    }

#if UNITY_EDITOR
    private void OnValidate() => UnityEditor.EditorApplication.delayCall += () =>
    {
        if (this != null) RebuildMesh();
    };
#endif
}
