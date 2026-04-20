using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Asteroid : MonoBehaviour
{
    [Header("Visuals")]
    public List<GameObject> prefabPool;

    [Header("Spin")]
    [Tooltip("Spin speed in deg/sec. Negative = reverse direction. Defaults to a random value (10–15) when first added.")]
    public float spinSpeed = 12f;
    [Tooltip("Axes that contribute to the spin direction (local space).")]
    public bool spinX = true;
    public bool spinY = true;
    public bool spinZ = true;

    // Name used to track the designer-placed visual child.
    internal const string VisualChildName = "__AsteroidVisual__";

    private FMODUnity.StudioEventEmitter soundFX;
    private Vector3 _spinAxis;

    private void Reset()
    {
        spinSpeed = Random.Range(10f, 15f);
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        // If a visual was pre-placed via the editor button, use it.
        // Otherwise pick a random one from the pool.
        var existingVisual = transform.Find(VisualChildName);
        GameObject visual;

        if (existingVisual != null)
        {
            visual = existingVisual.gameObject;
            // Restore normal hide flags stripped during edit-time placement
            visual.hideFlags = HideFlags.None;
        }
        else
        {
            if (prefabPool == null || prefabPool.Count == 0) return;
            int idx = Random.Range(0, prefabPool.Count);
            visual  = Instantiate(prefabPool[idx], transform);
        }

        transform.rotation = Random.rotation;

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

        Vector3 axis = new Vector3(spinX ? 1f : 0f, spinY ? 1f : 0f, spinZ ? 1f : 0f);
        _spinAxis    = axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.up;

        soundFX = GetComponent<FMODUnity.StudioEventEmitter>();
        if (soundFX != null) soundFX.SetParameter("Size", transform.localScale.x);
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        transform.Rotate(_spinAxis, spinSpeed * Time.deltaTime, Space.Self);
    }

#if UNITY_EDITOR
    internal void SpawnVisual(int index)
    {
        // Destroy any previously spawned visual child
        var existing = transform.Find(VisualChildName);
        if (existing != null) DestroyImmediate(existing.gameObject);

        if (prefabPool == null || index >= prefabPool.Count || prefabPool[index] == null) return;

        var visual  = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefabPool[index], transform);
        visual.name = VisualChildName;
        visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        visual.transform.localScale = Vector3.one;

        UnityEditor.EditorUtility.SetDirty(gameObject);
    }

    internal void ClearVisual()
    {
        var existing = transform.Find(VisualChildName);
        if (existing != null) DestroyImmediate(existing.gameObject);
    }
#endif
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(Asteroid))]
public class AsteroidEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var asteroid = (Asteroid)target;
        if (asteroid.prefabPool == null || asteroid.prefabPool.Count == 0) return;

        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("Spawn Visual", UnityEditor.EditorStyles.boldLabel);

        for (int i = 0; i < asteroid.prefabPool.Count; i++)
        {
            var prefab = asteroid.prefabPool[i];
            if (prefab == null) continue;
            if (GUILayout.Button(prefab.name))
            {
                UnityEditor.Undo.RegisterFullObjectHierarchyUndo(asteroid.gameObject, "Spawn Asteroid Visual");
                asteroid.SpawnVisual(i);
            }
        }

        if (GUILayout.Button("Clear Visual"))
        {
            UnityEditor.Undo.RegisterFullObjectHierarchyUndo(asteroid.gameObject, "Clear Asteroid Visual");
            asteroid.ClearVisual();
        }
    }
}
#endif
