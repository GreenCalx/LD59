using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hold-to-adjust keyboard controls for the wave signal parameters,
/// wired through Unity's Input System as serialized InputActions.
///
/// Default bindings (populated on Awake if the inspector is empty):
///   W/S  → frequency up/down
///   A/D  → pan (tilt) left/right
///   Q/E  → amplitude up/down
///
/// Rebind per-project by editing the three actions in the inspector.
/// Runs before WaveGenerator.Update so changes apply on the same frame.
/// </summary>
[DefaultExecutionOrder(-95)]
public class WaveInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveGenerator waveGenerator;

    [Header("Input Actions (1D axis, -1..+1)")]
    [SerializeField] private InputAction frequencyAxis = new InputAction("Frequency", InputActionType.Value, expectedControlType: "Axis");
    [SerializeField] private InputAction panAxis       = new InputAction("Pan",       InputActionType.Value, expectedControlType: "Axis");
    [SerializeField] private InputAction amplitudeAxis = new InputAction("Amplitude", InputActionType.Value, expectedControlType: "Axis");

    [Header("Rates (units per second at full deflection)")]
    [SerializeField] private float frequencyRate = 0.5f;
    [SerializeField] private float panRate       = 2f;
    [SerializeField] private float amplitudeRate = 1f;

    [Header("Clamps")]
    [SerializeField] private float minFrequency = 0.1f;
    [SerializeField] private float maxFrequency = 5f;
    [SerializeField] private float minAmplitude = 0f;
    [SerializeField] private float maxAmplitude = 5f;

    private void Awake()
    {
        EnsureDefaultBinding(frequencyAxis, negative: "<Keyboard>/s", positive: "<Keyboard>/w");
        EnsureDefaultBinding(panAxis,       negative: "<Keyboard>/a", positive: "<Keyboard>/d");
        EnsureDefaultBinding(amplitudeAxis, negative: "<Keyboard>/e", positive: "<Keyboard>/q");
    }

    private static void EnsureDefaultBinding(InputAction action, string negative, string positive)
    {
        if (action.bindings.Count > 0) return; // inspector/user bindings win
        action.AddCompositeBinding("1DAxis")
            .With("Negative", negative)
            .With("Positive", positive);
    }

    private void OnEnable()
    {
        frequencyAxis.Enable();
        panAxis.Enable();
        amplitudeAxis.Enable();
    }

    private void OnDisable()
    {
        frequencyAxis.Disable();
        panAxis.Disable();
        amplitudeAxis.Disable();
    }

    private void Update()
    {
        if (waveGenerator == null) return;
        float dt = Time.deltaTime;

        float f = frequencyAxis.ReadValue<float>();
        if (f != 0f)
            waveGenerator.Frequency = Mathf.Clamp(
                waveGenerator.Frequency + f * frequencyRate * dt,
                minFrequency, maxFrequency);

        float p = panAxis.ReadValue<float>();
        if (p != 0f)
            waveGenerator.Pan = waveGenerator.Pan + p * panRate * dt;

        float a = amplitudeAxis.ReadValue<float>();
        if (a != 0f)
            waveGenerator.Amplitude = Mathf.Clamp(
                waveGenerator.Amplitude + a * amplitudeRate * dt,
                minAmplitude, maxAmplitude);
    }
}
