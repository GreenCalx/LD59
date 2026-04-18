using UnityEngine;

public class BoomerBeat : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer skinnedMesh;
    [SerializeField] private GameConfig          config;
    [SerializeField] private string              blendShapeName = "boomin";
    [SerializeField, Range(0f, 100f)] private float amplitude = 100f;

    private float _phase;
    private int   _shapeIndex = -1;

    private float Bpm => config?.waveGenerator?.bpm ?? 120f;

    private void Start()
    {
        if (skinnedMesh != null)
            _shapeIndex = skinnedMesh.sharedMesh.GetBlendShapeIndex(blendShapeName);
    }

    private void Update()
    {
        if (!Application.isPlaying || skinnedMesh == null || _shapeIndex < 0) return;
        _phase = (_phase + Bpm / 60f * Time.deltaTime) % 1f;
        float weight = Mathf.Sin(_phase * Mathf.PI) * amplitude;
        skinnedMesh.SetBlendShapeWeight(_shapeIndex, weight);
    }
}
