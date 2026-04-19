using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonoBehaviour
{
    [Header("Visuals")]
    public List<GameObject> prefabPool;
    [Min(0f)] public float minScale = 6f;
    [Min(0f)] public float maxScale = 20f;

    [Header("Spin")]
    [Min(0f)] public float minSpinSpeed = 8f;
    [Min(0f)] public float maxSpinSpeed = 40f;

    [Header("Collision")]
    [Tooltip("Trigger sphere radius in local space (world radius = scale * hitRadius)")]
    [Min(0.01f)] public float hitRadius = 0.5f;

    private FMODUnity.StudioEventEmitter soundFX;

    private Vector3 _spinAxis;
    private float   _spinSpeed;

    private void Start()
    {
        if (prefabPool == null || prefabPool.Count == 0) return;


        float scale          = Random.Range(minScale, maxScale);
        transform.localScale = Vector3.one * scale;
        transform.rotation   = Random.rotation;

        int idx    = Random.Range(0, prefabPool.Count);
        var visual = Instantiate(prefabPool[idx], transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale    = Vector3.one;

        var sc      = gameObject.AddComponent<SphereCollider>();
        sc.radius    = hitRadius;
        sc.isTrigger = true;
        GetComponent<HeroDamagerRoot>()?.Refresh();

        _spinAxis  = Random.onUnitSphere;
        _spinSpeed = Random.Range(minSpinSpeed, maxSpinSpeed)
                   * (Random.value > 0.5f ? 1f : -1f);

        soundFX = GetComponent<FMODUnity.StudioEventEmitter>();
        if (soundFX != null) soundFX.SetParameter("Size", (scale - minScale) / (maxScale - minScale));
    }

    private void Update()
    {
        transform.Rotate(_spinAxis, _spinSpeed * Time.deltaTime, Space.Self);
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        float meshR = GetPoolMeshRadius();
        if (meshR > 0f)
        {
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.25f);
            Gizmos.DrawWireSphere(Vector3.zero, meshR * minScale);
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
            Gizmos.DrawWireSphere(Vector3.zero, meshR * maxScale);
        }

        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(Vector3.zero, hitRadius * minScale);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Vector3.zero, hitRadius * maxScale);
    }

    private float GetPoolMeshRadius()
    {
        float max = 0f;
        if (prefabPool == null) return max;
        foreach (var prefab in prefabPool)
        {
            if (prefab == null) continue;
            foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                float r = mf.sharedMesh.bounds.extents.magnitude;
                if (r > max) max = r;
            }
        }
        return max;
    }
    #endif
}
