using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HoopCurvePath))]
public class HoopCurvePathEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var path = (HoopCurvePath)target;
        EditorGUILayout.Space();
        if (GUILayout.Button("Regenerate", GUILayout.Height(30)))
        {
            Undo.RecordObject(path.gameObject, "Regenerate Hoops");
            path.Regenerate();
        }

        if (path.SplineContainer != null)
        {
            int count = path.SplineContainer.Splines.Count;
            EditorGUILayout.HelpBox($"SplineContainer has {count} spline(s). Each row in Per Spline maps to one.", MessageType.Info);
        }
    }
}
