using UnityEngine;

[CreateAssetMenu(fileName = "ProgressConfig", menuName = "MAUVE/Progress Config")]
public class ProgressConfig : ScriptableObject
{
    [Min(0f)] public float baseScrollSpeed    = 10f;
    [Min(0f)] public float signalGain         = 0.05f;
    [Min(0f)] public float minSpeedMultiplier = 0.5f;
    [Min(0f)] public float maxSpeedMultiplier = 2f;
}
