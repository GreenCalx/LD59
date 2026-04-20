using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Asteroid : MonoBehaviour
{
    [Header("Visuals")]
    public List<GameObject> prefabPool;
    [Min(0f)] public float minScale = 6f;
    [Min(0f)] public float maxScale = 20f;

    [Header("Spin")]
    [Tooltip("Spin speed in deg/sec. Negative = reverse direction. Defaults to a random value (10–15) when first added.")]
    public float spinSpeed = 12f;
    [Tooltip("Axes that contribute to the spin direction (local space).")]
    public bool spinX = true;
    public bool spinY = true;
    public bool spinZ = true;

    private FMODUnity.StudioEventEmitter soundFX;
    private Vector3 _spinAxis;

    // Called when the component is first added or Reset from the Inspector context menu.
    private void Reset()
    {
        spinSpeed = Random.Range(10f, 15f);
    }

    private void Start()
    {
        if (!Application.isPlaying) return;
        if (prefabPool == null || prefabPool.Count == 0) return;

        float scale          = Random.Range(minScale, maxScale);
        transform.localScale = Vector3.one * scale;
        transform.rotation   = Random.rotation;

        int idx    = Random.Range(0, prefabPool.Count);
        var visual = Instantiate(prefabPool[idx], transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale    = Vector3.one;

        foreach (var mf in visual.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            var mc        = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex     = false;
        }
        GetComponent<HeroDamagerRoot>()?.Refresh();

        // Build spin axis from bools; fall back to Y if all unchecked.
        Vector3 axis = new Vector3(spinX ? 1f : 0f, spinY ? 1f : 0f, spinZ ? 1f : 0f);
        _spinAxis    = axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.up;

        soundFX = GetComponent<FMODUnity.StudioEventEmitter>();
        if (soundFX != null) soundFX.SetParameter("Size", transform.localScale.x / 50f);
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        transform.Rotate(_spinAxis, spinSpeed * Time.deltaTime, Space.Self);
    }

#if UNITY_EDITOR
    private GameObject _preview;
    private const string PreviewName = "__AsteroidPreview__";

    private void OnEnable()
    {
        if (!Application.isPlaying) RefreshPreview();
    }

    private void OnDisable()
    {
        DestroyPreview();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            UnityEditor.EditorApplication.delayCall += () => { if (this) RefreshPreview(); };
    }

    private void RefreshPreview()
    {
        DestroyPreview();
        if (prefabPool == null || prefabPool.Count == 0 || prefabPool[0] == null) return;

        // InstantiatePrefab with a parent fails when this GO is inside a prefab
        // asset (Prefab Mode). Fall back to regular Instantiate in that case.
        bool inPrefabAsset = UnityEditor.EditorUtility.IsPersistent(gameObject);
        _preview = inPrefabAsset
            ? Instantiate(prefabPool[0], transform)
            : (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefabPool[0], transform);
        _preview.name      = PreviewName;
        _preview.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

        float mean = (minScale + maxScale) * 0.5f;
        _preview.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        _preview.transform.localScale = Vector3.one * mean;
    }

    private void DestroyPreview()
    {
        if (_preview != null) DestroyImmediate(_preview);
        var orphan = transform.Find(PreviewName);
        if (orphan != null) DestroyImmediate(orphan.gameObject);
        _preview = null;
    }
#endif
}
