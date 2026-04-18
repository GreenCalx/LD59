using UnityEngine;

[CreateAssetMenu(fileName = "SurferConfig", menuName = "MAUVE/Surfer Config")]
public class SurferConfig : ScriptableObject
{
    [Range(0f, 90f)] public float maxTiltDegrees = 45f;
    public bool alignToTangent = true;
}
