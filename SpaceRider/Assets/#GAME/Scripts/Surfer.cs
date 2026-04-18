using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class Surfer : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private WaveGenerator   waveGenerator;
    [SerializeField] private GameConfig      config;

    private Animator _animator;
    private float    _smSlope;
    private float    _smPan;

    private void Awake() => _animator = GetComponentInChildren<Animator>();

    private void Update()
    {
        if (splineContainer == null || config?.level == null || config?.surfer == null) return;
        var spline = splineContainer.Spline;
        if (spline == null || spline.Count < 2) return;

        float decay = config.level.decayLength;
        float ahead = config.level.lookAhead;
        float span  = decay + ahead;
        if (span <= 0f) return;

        SplineUtility.Evaluate(spline, decay / span, out float3 pos, out float3 tan, out float3 _);
        Vector3 worldPos = splineContainer.transform.TransformPoint(pos);

        Vector3 parentLocal = transform.parent != null
            ? transform.parent.InverseTransformPoint(worldPos) : worldPos;
        parentLocal.z = 0f;
        transform.position = transform.parent != null
            ? transform.parent.TransformPoint(parentLocal) : parentLocal;

        if (!config.surfer.alignToTangent) return;

        Vector3 worldFwd = splineContainer.transform.TransformDirection(
            (Vector3)math.normalizesafe(tan, new float3(0, 0, 1)));
        if (worldFwd.sqrMagnitude <= 1e-6f) return;
        worldFwd.Normalize();

        Vector3 flatFwd = new Vector3(worldFwd.x, 0f, worldFwd.z);
        if (flatFwd.sqrMagnitude <= 1e-6f) return;
        flatFwd.Normalize();

        float maxTilt  = config.surfer.maxTiltDegrees;
        float maxSin   = Mathf.Sin(maxTilt * Mathf.Deg2Rad);
        float sinPitch = Mathf.Clamp(worldFwd.y, -maxSin, maxSin);
        float cosPitch = Mathf.Sqrt(Mathf.Max(0f, 1f - sinPitch * sinPitch));
        Quaternion yaw = Quaternion.LookRotation(flatFwd * cosPitch + Vector3.up * sinPitch, Vector3.up);

        float pan      = waveGenerator != null ? waveGenerator.GetEffectivePanAtHero() : 0f;
        transform.rotation = yaw * Quaternion.AngleAxis(-pan * maxTilt, Vector3.forward);

        DriveAnimator(pan);
    }

    private void DriveAnimator(float effectivePan)
    {
        if (_animator == null || waveGenerator == null || config?.surfer == null) return;
        if (!Application.isPlaying) return;

        float scale      = config.surfer.slopeAnimScale;
        float smoothTime = config.surfer.animSmoothTime;
        float lerpT      = smoothTime > 0f ? Time.deltaTime / smoothTime : 1f;

        float targetSlope = Mathf.Clamp(waveGenerator.SampleDerivativeAtHero() / scale, -1f, 1f);
        float targetPan   = effectivePan;

        _smSlope = Mathf.Lerp(_smSlope, targetSlope, lerpT);
        _smPan   = Mathf.Lerp(_smPan,   targetPan,   lerpT);

        _animator.SetFloat("Slope", _smSlope);
        _animator.SetFloat("Pan",   _smPan);
    }

    private void OnDrawGizmos()
    {
        if (!GameDebug.ShowGizmos || waveGenerator == null) return;
        Vector3 contact = transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(contact, 0.3f);
        Vector3 fwd = transform.forward;
        Gizmos.DrawLine(contact, contact + fwd * 1.5f);
        Gizmos.DrawLine(contact + fwd * 1.5f, contact + fwd * 1.2f + transform.up * 0.3f);
        Gizmos.DrawLine(contact + fwd * 1.5f, contact + fwd * 1.2f - transform.up * 0.3f);
    }
}
