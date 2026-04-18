using UnityEngine;

[CreateAssetMenu(fileName = "WaveInputConfig", menuName = "MAUVE/Wave Input Config")]
public class WaveInputConfig : ScriptableObject
{
    [Header("Frequency")]
    [Min(0.01f)] public float freqMin = 0.1f;
    public float freqMax       = 5f;
    public float freqInitial   = 1f;
    public float frequencyRate = 0.5f;

    [Header("Amplitude")]
    public float ampMin        = 0f;
    public float ampMax        = 5f;
    public float ampInitial    = 1f;
    public float amplitudeRate = 1f;

    [Header("Pan")]
    public float panInitial    = 0f;
    public float panRate       = 2f;
}
