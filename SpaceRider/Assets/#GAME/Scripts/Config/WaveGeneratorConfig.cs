using UnityEngine;

[CreateAssetMenu(fileName = "WaveGeneratorConfig", menuName = "MAUVE/Wave Generator Config")]
public class WaveGeneratorConfig : ScriptableObject
{
    [Min(1f)] public float sampleDensity          = 4f;
    [Min(0f)] public float paramSmoothingDistance = 2f;
    [Min(0f)]  public float panLateralScale = 0.2f;
    [Min(1f)]  public float bpm             = 120f;
}
