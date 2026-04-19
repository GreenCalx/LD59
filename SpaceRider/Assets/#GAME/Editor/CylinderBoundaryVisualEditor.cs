using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CylinderBoundaryVisual))]
public class CylinderBoundaryVisualEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space(4);
        if (GUILayout.Button("Bake Cylinder Mesh"))
            ((CylinderBoundaryVisual)target).BakeInEditor();
    }
}
