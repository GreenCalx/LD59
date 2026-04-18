using UnityEngine;

/// <summary>
/// Action camera that chases the hero in XY with smooth damping while
/// always aiming at the FinishLine as a vanishing point.
///
/// Position: hero.position + worldOffset, smoothed with SmoothDamp so the
/// camera lags behind XY jolts, giving a floaty "surfing" feel.
///
/// Rotation: looks at the FinishLine. The up vector is blended between
/// world-up and the hero's local up by <see cref="rollInfluence"/>, so the
/// camera gently banks into turns without going full roll.
///
/// Field of view: expands with scroll speed to sell acceleration.
/// </summary>
[DefaultExecutionOrder(100)]
public class SpaceRiderCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform      hero;
    [SerializeField] private Transform      finishLine;
    [SerializeField] private LevelScope     levelScope;
    [SerializeField] private WaveGenerator  waveGenerator;

    [Header("Position")]
    [Tooltip("Camera offset in world space relative to the hero.")]
    [SerializeField] private Vector3 offset           = new Vector3(0f, 2f, -8f);
    [Tooltip("Lower = more lag, higher = snappier follow.")]
    [SerializeField] private float   positionSmoothTime = 0.18f;

    [Header("Rotation")]
    [Tooltip("How much the hero's banking tilts the camera's up vector. 0 = no roll, 1 = full roll.")]
    [SerializeField, Range(0f, 1f)] private float rollInfluence  = 0.35f;
    [Tooltip("Slerp speed toward the target look rotation (higher = snappier).")]
    [SerializeField] private float                lookSharpness  = 8f;

    [Header("Field of View")]
    [SerializeField] private float baseFov        = 60f;
    [Tooltip("Extra degrees of FOV per unit of scroll speed.")]
    [SerializeField] private float fovSpeedScale  = 0.4f;
    [SerializeField] private float fovSmoothTime  = 0.25f;

    private Camera    _cam;
    private Vector3   _velocity;
    private float     _fovVelocity;
    private float     _currentFov;

    private void Awake()
    {
        _cam        = GetComponentInChildren<Camera>();
        _currentFov = _cam != null ? _cam.fieldOfView : baseFov;
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
        Vector3 target = hero.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position, target, ref _velocity, positionSmoothTime);
    }

    private void RotateCamera()
    {
        // Look direction: camera → FinishLine (falls back to camera forward if null)
        Vector3 lookTarget = finishLine != null
            ? finishLine.position
            : transform.position + transform.forward * 100f;

        Vector3 toTarget = lookTarget - transform.position;
        if (toTarget.sqrMagnitude < 1e-4f) return;

        // Up vector: blend world-up toward hero-up for gentle banking
        Vector3 worldUp = Vector3.up;
        Vector3 heroUp  = hero.up;
        Vector3 blendedUp = Vector3.Slerp(worldUp, heroUp, rollInfluence).normalized;

        Quaternion targetRot = Quaternion.LookRotation(toTarget, blendedUp);
        transform.rotation   = Quaternion.Slerp(
            transform.rotation, targetRot, Time.deltaTime * lookSharpness);
    }

    private void UpdateFov()
    {
        if (_cam == null) return;

        float speed    = levelScope != null ? levelScope.ScrollSpeed : 0f;
        float targetFov = baseFov + speed * fovSpeedScale;
        _currentFov     = Mathf.SmoothDamp(
            _currentFov, targetFov, ref _fovVelocity, fovSmoothTime);
        _cam.fieldOfView = _currentFov;
    }
}
