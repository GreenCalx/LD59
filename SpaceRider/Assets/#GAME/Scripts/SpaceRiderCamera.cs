using UnityEngine;

[DefaultExecutionOrder(100)]
public class SpaceRiderCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform     hero;
    [SerializeField] private Transform     vanishingPoint;
    [SerializeField] private LevelScope    levelScope;
    [SerializeField] private WaveGenerator waveGenerator;
    [SerializeField] private GameConfig    config;

    private Camera  _cam;
    private Vector3 _velocity;
    private float   _fovVelocity;
    private float   _currentFov;
    private float   _swellY;
    private float   _swellVelocity;
    private float   _currentPitch;
    private float   _pitchVelocity;

    private void Awake()
    {
        _cam        = GetComponentInChildren<Camera>();
        _currentFov = _cam != null ? _cam.fieldOfView : (config?.camera?.baseFov ?? 60f);
    }

    private void LateUpdate()
    {
        if (hero == null) return;
        MoveCamera();
        RotateCamera();
        UpdateFov();
    }

    private void MoveCamera()
    {
        Vector3 baseOffset = config?.camera?.offset             ?? new Vector3(0f, 2f, -8f);
        float   smoothTime = config?.camera?.positionSmoothTime ?? 0.18f;

        // Wave swell — lift camera proportional to current wave amplitude
        float targetSwellY = waveGenerator != null && config?.camera != null
            ? waveGenerator.Mapped_amplitude() * config.camera.waveAmplitudeOffsetScale
            : 0f;
        _swellY = Mathf.SmoothDamp(_swellY, targetSwellY, ref _swellVelocity,
                                    config?.camera?.swellSmoothTime ?? 0.3f);

        // Speed pull-back — extend Z offset at high speed
        float speed    = levelScope != null ? levelScope.ScrollSpeed : 0f;
        float pullback = config?.camera != null ? speed * config.camera.speedPullbackScale : 0f;

        Vector3 offset = baseOffset + new Vector3(0f, _swellY, -pullback);
        transform.position = Vector3.SmoothDamp(transform.position, hero.position + offset,
                                                ref _velocity, smoothTime);
    }

    private void RotateCamera()
    {
        Vector3 lookTarget = vanishingPoint != null
            ? vanishingPoint.position
            : transform.position + new Vector3(0f, 0f, 100f);

        Vector3 toTarget = lookTarget - transform.position;
        if (toTarget.sqrMagnitude < 1e-4f) return;

        float rollInfluence = config?.camera?.rollInfluence ?? 0.35f;
        float lookSharpness = config?.camera?.lookSharpness ?? 8f;

        Vector3    blendedUp = Vector3.Slerp(Vector3.up, hero.up, rollInfluence).normalized;
        Vector3    rotOffset = config?.camera?.rotationOffset ?? Vector3.zero;
        Quaternion targetRot = Quaternion.LookRotation(toTarget, blendedUp) * Quaternion.Euler(rotOffset);

        // Forward lean — pitch driven by wave slope and speed
        float targetPitch = 0f;
        if (waveGenerator != null && config?.camera != null)
        {
            float slope = waveGenerator.SampleDerivativeAtHero();
            float spd   = levelScope != null ? levelScope.ScrollSpeed : 0f;
            float raw   = slope * config.camera.waveSlopePitchScale
                        + spd   * config.camera.speedPitchScale;
            targetPitch = Mathf.Clamp(raw, -config.camera.maxPitchDegrees, config.camera.maxPitchDegrees);
        }
        _currentPitch = Mathf.SmoothDamp(_currentPitch, targetPitch, ref _pitchVelocity,
                                          config?.camera?.pitchSmoothTime ?? 0.2f);

        targetRot *= Quaternion.Euler(_currentPitch, 0f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lookSharpness);
    }

    private void UpdateFov()
    {
        if (_cam == null || config?.camera == null) return;
        float speed      = levelScope != null ? levelScope.ScrollSpeed : 0f;
        float targetFov  = config.camera.baseFov + speed * config.camera.fovSpeedScale;
        _currentFov      = Mathf.SmoothDamp(_currentFov, targetFov, ref _fovVelocity, config.camera.fovSmoothTime);
        _cam.fieldOfView = _currentFov;
    }
}
