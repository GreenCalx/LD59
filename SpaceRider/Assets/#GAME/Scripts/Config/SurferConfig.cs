using UnityEngine;

[CreateAssetMenu(fileName = "SurferConfig", menuName = "MAUVE/Surfer Config")]
public class SurferConfig : ScriptableObject
{
    [Range(0f, 90f)] public float maxTiltDegrees  = 45f;
    public bool alignToTangent = true;

    [Header("Animation")]
    [Min(0.01f)] public float slopeAnimScale    = 1f;
    [Min(0.01f)] public float animSmoothTime    = 0.15f;
    [Min(0.01f)] public float panCurvatureScale = 3f;
}
