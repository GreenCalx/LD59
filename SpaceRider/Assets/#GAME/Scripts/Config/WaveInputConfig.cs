using UnityEngine;

[CreateAssetMenu(fileName = "WaveInputConfig", menuName = "MAUVE/Wave Input Config")]
public class WaveInputConfig : ScriptableObject
{
    [Header("Frequency")]
    public int   freqInitial   = 500;
    public float frequencyRate = 200f;

    [Header("Amplitude")]
    public int   ampInitial    = 500;
    public float amplitudeRate = 200f;

    [Header("Pan")]
    public int   panInitial    = 500;
    public float panRate       = 200f;
}
