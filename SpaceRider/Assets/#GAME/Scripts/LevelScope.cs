using UnityEngine;

/// <summary>
/// Single source of truth for level-wide parameters.
/// Referenced by <see cref="WaveGenerator"/>, <see cref="Surfer"/>, etc.
/// </summary>
public class LevelScope : MonoBehaviour
{
    [SerializeField] private float levelLength = 100f;

    /// <summary>Total length of the level along the Z axis.</summary>
    public float LevelLength { get => levelLength; set => levelLength = Mathf.Max(0f, value); }
}
