using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "MAUVE/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Min(0f)] public float levelLength     = 1000f;
    [Min(0f)] public float lookAhead       = 30f;
    [Min(0f)] public float decayLength     = 5f;
    [Min(0f)] public float playfieldRadius = 8f;
}
