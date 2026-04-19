using UnityEngine;

[CreateAssetMenu(fileName = "BoundaryConfig", menuName = "MAUVE/Boundary Config")]
public class BoundaryConfig : ScriptableObject
{
    [Range(0f, 1f)] public float feedbackStartT        = 0.5f;
    public string                fmodParameterName      = "BoundaryProximity";
    [Min(3)]        public int   cylinderSegmentsAround = 64;
    [Min(2)]        public int   cylinderSegmentsAlong  = 32;
    [Min(0f)]       public float drawRadius             = 15f;
}
