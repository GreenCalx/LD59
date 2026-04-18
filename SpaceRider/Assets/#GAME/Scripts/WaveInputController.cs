using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-95)]
public class WaveInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveGenerator waveGenerator;
    [SerializeField] private GameConfig    config;

    [Header("Input Actions (1D axis, -1..+1)")]
    [SerializeField] private InputAction frequencyAxis = new InputAction("Frequency", InputActionType.Value, expectedControlType: "Axis");
    [SerializeField] private InputAction panAxis       = new InputAction("Pan",       InputActionType.Value, expectedControlType: "Axis");
    [SerializeField] private InputAction amplitudeAxis = new InputAction("Amplitude", InputActionType.Value, expectedControlType: "Axis");

    private void Awake()
    {
        EnsureDefaultBinding(frequencyAxis, "<Keyboard>/s", "<Keyboard>/w");
        EnsureDefaultBinding(panAxis,       "<Keyboard>/a", "<Keyboard>/d");
        EnsureDefaultBinding(amplitudeAxis, "<Keyboard>/e", "<Keyboard>/q");

        if (waveGenerator != null && config?.waveInput != null)
        {
            waveGenerator.Frequency = config.waveInput.freqInitial;
            waveGenerator.Amplitude = config.waveInput.ampInitial;
            waveGenerator.Pan       = config.waveInput.panInitial;
        }
    }

    private static void EnsureDefaultBinding(InputAction action, string negative, string positive)
    {
        if (action.bindings.Count > 0) return;
        action.AddCompositeBinding("1DAxis").With("Negative", negative).With("Positive", positive);
    }

    private void OnEnable()  { frequencyAxis.Enable();  panAxis.Enable();  amplitudeAxis.Enable(); }
    private void OnDisable() { frequencyAxis.Disable(); panAxis.Disable(); amplitudeAxis.Disable(); }

    private void Update()
    {
        if (waveGenerator == null || config?.waveInput == null) return;
        var wi = config.waveInput;
        float dt = Time.deltaTime;

        float f = frequencyAxis.ReadValue<float>();
        if (f != 0f)
            waveGenerator.Frequency = Mathf.Clamp(
                Mathf.RoundToInt(waveGenerator.Frequency + f * wi.frequencyRate * dt),
                0, Constants.INTEGER_RANGE);

        float p = panAxis.ReadValue<float>();
        if (p != 0f)
            waveGenerator.Pan = Mathf.Clamp(
                Mathf.RoundToInt(waveGenerator.Pan + p * wi.panRate * dt),
                0, Constants.INTEGER_RANGE);

        float a = amplitudeAxis.ReadValue<float>();
        if (a != 0f)
            waveGenerator.Amplitude = Mathf.Clamp(
                Mathf.RoundToInt(waveGenerator.Amplitude + a * wi.amplitudeRate * dt),
                0, Constants.INTEGER_RANGE);
    }
}
