using UnityEngine;

[CreateAssetMenu(fileName = "RibbonConfig", menuName = "MAUVE/Ribbon Config")]
public class RibbonConfig : ScriptableObject
{
    [Min(0.01f)] public float  width       = 1f;
    [Min(2)]     public int    segments    = 256;
    public Vector3             ribbonOffset = Vector3.zero;
    [Min(0f)]    public float  decayLength = 5f;
}
