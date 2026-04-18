using UnityEngine;

[CreateAssetMenu(fileName = "ProgressConfig", menuName = "MAUVE/Progress Config")]
public class ProgressConfig : ScriptableObject
{
    [Min(0f)] public float baseScrollSpeed    = 10f;
    [Min(0f)] public float signalGain         = 5f;
    [Min(0f)] public float minSpeedMultiplier = 0.3f;
    [Min(0f)] public float maxSpeedMultiplier = 2.5f;
    [Min(0f)] public float dragRate           = 1.5f;
}
