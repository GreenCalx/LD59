using UnityEngine;

public class UIKnob : MonoBehaviour
{
    public enum WaveParam { Amplitude, Frequency, Pan }

    [SerializeField] private WaveParam     parameter;
    [SerializeField] private RectTransform knobImage;
    [SerializeField] private float         minAngle = -135f;
    [SerializeField] private float         maxAngle =  135f;

    private WaveGenerator _wave;

    private void Awake()
    {
        if (knobImage == null)
            knobImage = transform.Find("knobImage")?.GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        if (_wave == null)
        {
            _wave = GameServices.Instance?.waveGenerator;
            if (_wave == null) return;
        }
        if (knobImage == null) return;

        int value = parameter switch
        {
            WaveParam.Amplitude => _wave.Amplitude,
            WaveParam.Frequency => _wave.Frequency,
            WaveParam.Pan       => _wave.Pan,
            _                   => 0,
        };

        float angle = Mathf.Lerp(minAngle, maxAngle, value / (float)Constants.INTEGER_RANGE);
        knobImage.localEulerAngles = new Vector3(0f, 0f, angle);
    }
}
