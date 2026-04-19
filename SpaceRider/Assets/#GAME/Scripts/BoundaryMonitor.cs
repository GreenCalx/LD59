using UnityEngine;

[DefaultExecutionOrder(-60)]
public class BoundaryMonitor : MonoBehaviour
{
    [SerializeField] private Transform              hero;
    [SerializeField] private GameConfig             config;
    [SerializeField] private WaveGenerator          waveGenerator;
    [SerializeField] private RibbonVisualizer       ribbonVisualizer;
    [SerializeField] private CylinderBoundaryVisual boundaryVisual;
    [SerializeField] private PlayerDeath            playerDeath;
    [SerializeField] private LevelScope             levelScope;

    public float BoundaryT { get; private set; }

    public static float ComputeBoundaryT(Vector3 heroPos, float radius)
    {
        float r = Mathf.Max(radius, 1e-4f);
        return new Vector2(heroPos.x, heroPos.y).magnitude / r;
    }

    public static float ComputeEffectT(float boundaryT, float feedbackStartT)
    {
        return Mathf.InverseLerp(feedbackStartT, 1f, boundaryT);
    }

    private void Update()
    {
        if (hero == null || config?.level == null || config?.boundary == null) return;

        Vector3 center = levelScope != null ? levelScope.transform.position : Vector3.zero;
        Vector3 relPos = hero.position - center;
        BoundaryT = ComputeBoundaryT(relPos, config.level.playfieldRadius);

        float effectT = Mathf.Clamp01(ComputeEffectT(BoundaryT, config.boundary.feedbackStartT));

        waveGenerator?.SetBoundaryProximity(effectT);
        ribbonVisualizer?.SetBoundaryScale(1f - effectT);
        boundaryVisual?.SetProximityT(effectT);

        if (BoundaryT >= 1f && playerDeath != null)
        {
            playerDeath.Die();
            enabled = false;
        }
    }
}
