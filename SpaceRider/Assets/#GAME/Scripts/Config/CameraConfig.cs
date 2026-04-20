using UnityEngine;

[CreateAssetMenu(fileName = "CameraConfig", menuName = "MAUVE/Camera Config")]
public class CameraConfig : ScriptableObject
{
    [Header("Position")]
    public Vector3 offset             = new Vector3(0f, 2f, -8f);
    public float   positionSmoothTime = 0.18f;

    [Header("Rotation")]
    [Range(0f, 1f)] public float rollInfluence = 0.35f;
    public float lookSharpness  = 8f;
    public Vector3 rotationOffset = Vector3.zero;

    [Header("Field of View")]
    [Range(10f, 120f)] public float baseFov      = 60f;
    public float fovSpeedScale = 0.4f;
    public float fovSmoothTime = 0.25f;

    [Header("Wave Swell")]
    public float waveAmplitudeOffsetScale = 0.3f;
    public float swellSmoothTime         = 0.3f;

    [Header("Forward Lean")]
    public float waveSlopePitchScale = -4f;
    public float speedPitchScale     = -0.3f;
    public float pitchSmoothTime     = 0.2f;
    public float maxPitchDegrees     = 8f;

    [Header("Speed Pull-Back")]
    public float speedPullbackScale = 0.08f;
}
