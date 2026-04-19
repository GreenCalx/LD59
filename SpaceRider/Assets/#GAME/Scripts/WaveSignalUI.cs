using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class WaveSignalUI : MaskableGraphic
{
    [Header("References")]
    [SerializeField] public WaveGenerator waveGenerator;
    [SerializeField] public GameConfig    config;

    [Header("Display")]
    [SerializeField] public int   resolution    = 64;
    [SerializeField] public float lineHalfWidth = 3f;
    [SerializeField] public Color lineColor     = new Color(0f, 1f, 0.75f, 1f);

    public override Texture mainTexture => Texture2D.whiteTexture;

    protected override void Start()
    {
        base.Start();
        if (waveGenerator == null)
            waveGenerator = GameServices.Instance?.waveGenerator;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (waveGenerator == null) return;

        Rect  r      = rectTransform.rect;
        float w      = r.width;
        float h      = r.height;
        float ox     = r.xMin;
        float oy     = r.yMin;
        float zMin   = -(config?.level?.decayLength ?? 5f);
        float zMax   =   config?.level?.lookAhead   ?? 30f;
        float ampMax =   config?.waveGenerator?.amplitudeMax ?? 10f;
        if (ampMax < 0.001f) ampMax = 0.001f;

        for (int i = 0; i < resolution - 1; i++)
        {
            float t0 = (float)i       / (resolution - 1);
            float t1 = (float)(i + 1) / (resolution - 1);

            Vector3 s0 = waveGenerator.SampleAtLocalZ(Mathf.Lerp(zMin, zMax, t0));
            Vector3 s1 = waveGenerator.SampleAtLocalZ(Mathf.Lerp(zMin, zMax, t1));

            float x0 = ox + t0 * w;
            float y0 = oy + Mathf.Clamp01(s0.y / ampMax * 0.5f + 0.5f) * h;
            float x1 = ox + t1 * w;
            float y1 = oy + Mathf.Clamp01(s1.y / ampMax * 0.5f + 0.5f) * h;

            Vector2 dir  = new Vector2(x1 - x0, y1 - y0);
            if (dir.sqrMagnitude < 1e-8f) dir = Vector2.right;
            dir.Normalize();
            Vector2 perp = new Vector2(-dir.y, dir.x) * lineHalfWidth;

            int b = i * 4;
            vh.AddVert(new Vector3(x0 + perp.x, y0 + perp.y), lineColor, new Vector2(0, t0));
            vh.AddVert(new Vector3(x0 - perp.x, y0 - perp.y), lineColor, new Vector2(1, t0));
            vh.AddVert(new Vector3(x1 - perp.x, y1 - perp.y), lineColor, new Vector2(1, t1));
            vh.AddVert(new Vector3(x1 + perp.x, y1 + perp.y), lineColor, new Vector2(0, t1));

            vh.AddTriangle(b,     b + 1, b + 2);
            vh.AddTriangle(b,     b + 2, b + 3);
        }
    }

    private void Update()
    {
        if (Application.isPlaying) SetVerticesDirty();
    }
}
