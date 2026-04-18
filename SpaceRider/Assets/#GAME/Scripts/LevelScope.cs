using UnityEngine;

public class LevelScope : MonoBehaviour
{
    [SerializeField] private GameConfig config;

    [Header("Runtime (written by ProgressDriver)")]
    [SerializeField] private float virtualDistance;
    [SerializeField] private float scrollSpeed;

    public float LevelLength => config != null ? config.level.levelLength : 0f;
    public float LookAhead   => config != null ? config.level.lookAhead   : 30f;
    public float DecayLength => config != null ? config.level.decayLength : 5f;

    public float VirtualDistance
    {
        get => virtualDistance;
        set => virtualDistance = Mathf.Clamp(value, 0f, LevelLength > 0f ? LevelLength : float.MaxValue);
    }

    public float ScrollSpeed { get => scrollSpeed; set => scrollSpeed = value; }

    public float Progress01 =>
        LevelLength <= 0f ? 1f : Mathf.Clamp01(virtualDistance / LevelLength);

    public bool IsFinished => LevelLength > 0f && virtualDistance >= LevelLength;

    public void SetConfig(GameConfig c) { config = c; }

    private void OnDrawGizmos()
    {
        if (!GameDebug.ShowGizmos || config == null) return;
        float r      = config.level.playfieldRadius;
        float length = config.level.levelLength;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        DrawWireCylinder(transform.position, r, length);
    }

    // Draws a cylinder oriented along the Z-axis (the level's forward direction).
    // Rings are in the XY plane; struts connect them every 8 steps.
    private static void DrawWireCylinder(Vector3 origin, float radius, float length)
    {
        const int steps = 32;
        Vector3 back  = origin;
        Vector3 front = origin + Vector3.forward * length;
        Vector3 prev  = Vector3.right * radius;
        for (int i = 1; i <= steps; i++)
        {
            float angle = i / (float)steps * Mathf.PI * 2f;
            Vector3 curr = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            Gizmos.DrawLine(back  + prev, back  + curr);
            Gizmos.DrawLine(front + prev, front + curr);
            if (i % 8 == 0) Gizmos.DrawLine(back + curr, front + curr);
            prev = curr;
        }
    }
}
