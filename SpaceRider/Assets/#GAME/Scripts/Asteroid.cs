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

        _spinAxis  = Random.onUnitSphere;
        _spinSpeed = Random.Range(minSpinSpeed, maxSpinSpeed)
                   * (Random.value > 0.5f ? 1f : -1f);
    }

    private void Update()
    {
        transform.Rotate(_spinAxis, _spinSpeed * Time.deltaTime, Space.Self);
    }
}
