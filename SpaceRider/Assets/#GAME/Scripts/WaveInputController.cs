using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hold-to-adjust keyboard controls for the wave signal parameters.
/// W/S  → frequency up/down
/// A/D  → pan (tilt) left/right
/// Q/E  → amplitude up/down
/// Runs before WaveGenerator.Update so parameter changes are visible
/// on the same frame the wave is rebuilt.
/// </summary>
[DefaultExecutionOrder(-95)]
public class WaveInputController : MonoBehaviour
{
    [SerializeField] private WaveGenerator waveGenerator;

    [Header("Rates (units per second held)")]
    [SerializeField] private float frequencyRate = 0.5f;
    [SerializeField] private float panRate       = 2f;
    [SerializeField] private float amplitudeRate = 1f;

    [Header("Clamps")]
    [SerializeField] private float minFrequency = 0.1f;
    [SerializeField] private float maxFrequency = 5f;
    [SerializeField] private float minAmplitude = 0f;
    [SerializeField] private float maxAmplitude = 5f;

    private void Update()
    {
        if (waveGenerator == null) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        float dt = Time.deltaTime;

        float fDelta = 0f;
        if (kb.wKey.isPressed) fDelta += 1f;
        if (kb.sKey.isPressed) fDelta -= 1f;
        if (fDelta != 0f)
            waveGenerator.Frequency = Mathf.Clamp(
                waveGenerator.Frequency + fDelta * frequencyRate * dt,
                minFrequency, maxFrequency);

        float pDelta = 0f;
        if (kb.dKey.isPressed) pDelta += 1f;
        if (kb.aKey.isPressed) pDelta -= 1f;
        if (pDelta != 0f)
            waveGenerator.Pan = waveGenerator.Pan + pDelta * panRate * dt;

        float aDelta = 0f;
        if (kb.qKey.isPressed) aDelta += 1f;
        if (kb.eKey.isPressed) aDelta -= 1f;
        if (aDelta != 0f)
            waveGenerator.Amplitude = Mathf.Clamp(
                waveGenerator.Amplitude + aDelta * amplitudeRate * dt,
                minAmplitude, maxAmplitude);
    }
}
