using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class Surfer : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private WaveGenerator   waveGenerator;
    [SerializeField] private GameConfig      config;
    [SerializeField] private List<ParticleSystem> trails;

    private Animator        _animator;
    private ProgressDriver  _progressDriver;
    private float           _smSlope;
    private float           _smPan;
    private float           _prevSmPan;
    private float           _smPanVelocity;
    private Vector3         _smPos;
    private Vector3         _posVelocity;
    private Quaternion      _smRot    = Quaternion.identity;
    private float           _rotVelX, _rotVelY, _rotVelZ;
    private bool            _smInited;
    private float           _prevSpeed;
    private float           _smAccel;

    private void Awake()
    {
        _animator       = GetComponentInChildren<Animator>();
        _progressDriver = transform.root.GetComponentInChildren<ProgressDriver>();
    }
    private void OnEnable() { _smInited = false; _posVelocity = Vector3.zero; _rotVelX = _rotVelY = _rotVelZ = 0f; }

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
        Vector3 targetPos = transform.parent != null
            ? transform.parent.TransformPoint(parentLocal) : parentLocal;

        float posSmoothTime = config.surfer.positionSmoothTime;
        if (!_smInited) { _smPos = targetPos; _smRot = transform.rotation; _posVelocity = Vector3.zero; _smInited = true; }
        _smPos = posSmoothTime > 0f
            ? Vector3.SmoothDamp(_smPos, targetPos, ref _posVelocity, posSmoothTime)
            : targetPos;
        transform.position = _smPos;

        UpdateTrails();

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

        float pan         = waveGenerator != null ? waveGenerator.GetEffectivePanAtHero() : 0f;
        Quaternion targetRot = yaw * Quaternion.AngleAxis(-pan * maxTilt, Vector3.forward);

        float rotSmoothTime = config.surfer.rotationSmoothTime;
        if (rotSmoothTime > 0f)
        {
            Vector3 ce = _smRot.eulerAngles;
            Vector3 te = targetRot.eulerAngles;
            float x = Mathf.SmoothDampAngle(ce.x, te.x, ref _rotVelX, rotSmoothTime);
            float y = Mathf.SmoothDampAngle(ce.y, te.y, ref _rotVelY, rotSmoothTime);
            float z = Mathf.SmoothDampAngle(ce.z, te.z, ref _rotVelZ, rotSmoothTime);
            _smRot = Quaternion.Euler(x, y, z);
        }
        else
        {
            _smRot = targetRot;
        }
        transform.rotation = _smRot;

        DriveAnimator(pan);
    }

    private void UpdateTrails()
    {
        if (!Application.isPlaying || trails == null || trails.Count == 0 || _progressDriver == null) return;

        float speed    = _progressDriver.CurrentSpeed;
        float rawAccel = Time.deltaTime > 0f ? (speed - _prevSpeed) / Time.deltaTime : 0f;
        _prevSpeed     = speed;
        _smAccel       = Mathf.Lerp(_smAccel, rawAccel, Time.deltaTime * 5f);

        bool gaining = _smAccel > 0f;
        foreach (var trail in trails)
        {
            if (trail == null) continue;
            if (gaining  && !trail.isPlaying) trail.Play();
            if (!gaining &&  trail.isPlaying) trail.Stop();
        }
    }

    private void DriveAnimator(float effectivePan)
    {
        if (_animator == null || waveGenerator == null || config?.surfer == null) return;
        if (!Application.isPlaying) return;

        float scale          = config.surfer.slopeAnimScale;
        float smoothTime     = config.surfer.animSmoothTime;
        float lerpT          = smoothTime > 0f ? Time.deltaTime / smoothTime : 1f;

        float targetSlope = Mathf.Clamp(waveGenerator.SampleDerivativeAtHero() / scale, -1f, 1f);
        _smSlope = Mathf.Lerp(_smSlope, targetSlope, lerpT);

        // Smooth the raw pan signal first, then derive velocity from that
        _smPan = Mathf.Lerp(_smPan, effectivePan, lerpT);
        float rawVelocity = Time.deltaTime > 0f ? (_smPan - _prevSmPan) / Time.deltaTime : 0f;
        _prevSmPan = _smPan;

        float targetPanVelocity = Mathf.Clamp(rawVelocity * config.surfer.panCurvatureScale, -1f, 1f);
        _smPanVelocity = Mathf.Lerp(_smPanVelocity, targetPanVelocity, lerpT);

        _animator.SetFloat("Slope", _smSlope);
        _animator.SetFloat("Pan",   _smPanVelocity);
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
