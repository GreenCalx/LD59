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
        Vector3 offset     = config?.camera?.offset             ?? new Vector3(0f, 2f, -8f);
        float   smoothTime = config?.camera?.positionSmoothTime ?? 0.18f;
        transform.position = Vector3.SmoothDamp(transform.position, hero.position + offset, ref _velocity, smoothTime);
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

        Vector3 blendedUp = Vector3.Slerp(Vector3.up, hero.up, rollInfluence).normalized;
        Vector3    rotOffset = config?.camera?.rotationOffset ?? Vector3.zero;
        Quaternion targetRot = Quaternion.LookRotation(toTarget, blendedUp) * Quaternion.Euler(rotOffset);
        transform.rotation   = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lookSharpness);
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
