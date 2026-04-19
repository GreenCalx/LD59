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
    [Min(0f)] public float minSpinSpeed = 8f;
    [Min(0f)] public float maxSpinSpeed = 40f;


    private FMODUnity.StudioEventEmitter soundFX;
    private Vector3 _spinAxis;
    private float   _spinSpeed;

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

        _spinAxis  = Random.onUnitSphere;
        _spinSpeed = Random.Range(minSpinSpeed, maxSpinSpeed)
                   * (Random.value > 0.5f ? 1f : -1f);

        soundFX = GetComponent<FMODUnity.StudioEventEmitter>();
        if (soundFX != null) soundFX.SetParameter("Size", (scale - minScale) / (maxScale - minScale));
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        transform.Rotate(_spinAxis, _spinSpeed * Time.deltaTime, Space.Self);
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

        _preview = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefabPool[0], transform);
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
