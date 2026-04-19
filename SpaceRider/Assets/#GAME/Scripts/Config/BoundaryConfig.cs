using UnityEngine;

[CreateAssetMenu(fileName = "BoundaryConfig", menuName = "MAUVE/Boundary Config")]
public class BoundaryConfig : ScriptableObject
{
    [Range(0f, 1f)] public float  feedbackStartT       = 0.5f;
    public string                 fmodParameterName     = "BoundaryProximity";
    [Min(3)]        public int    cylinderSegmentsAround = 64;
    [Min(2)]        public int    cylinderSegmentsAlong  = 32;
    public Vector2                gridTiling            = new Vector2(8f, 20f);
    [Range(0f, 0.5f)] public float gridLineWidth        = 0.05f;
    [Range(0f, 1f)] public float  baseAlpha             = 0.05f;
    [Range(0f, 1f)] public float  proximityMaxAlpha     = 0.6f;
    public Color                  glowColor             = new Color(0f, 0.8f, 1f, 1f);
}
