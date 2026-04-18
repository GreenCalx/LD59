using UnityEngine;

[CreateAssetMenu(fileName = "WaveGeneratorConfig", menuName = "MAUVE/Wave Generator Config")]
public class WaveGeneratorConfig : ScriptableObject
{
    [Min(1f)] public float sampleDensity          = 4f;
    [Min(0f)] public float paramSmoothingDistance = 2f;
    [Min(0f)] public float panLateralScale        = 0.2f;
    [Min(1f)] public float bpm                    = 120f;

    [Header("Amplitude Mapping")]
    [Min(0f)] public float amplitudeMin = 0.1f;
    [Min(0f)] public float amplitudeMax = 10f;

    [Header("Frequency Mapping")]
    [Min(0f)] public float frequencyMin = 0.1f;
    [Min(0f)] public float frequencyMax = 1f;

    [Header("Tilt")]
    [Min(0f)] public float maxTiltDegrees = 30f;
}
