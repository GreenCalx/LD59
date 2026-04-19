using UnityEngine;
using UnityEngine.UI;

public class UIKnob : MonoBehaviour
{
    public enum WaveParam { Amplitude, Frequency, Pan }

    [SerializeField] private WaveParam     parameter;
    [SerializeField] private RectTransform knobImage;
    [SerializeField] private Image         knobGauge;
    [SerializeField] private float         minAngle = -135f;
    [SerializeField] private float         maxAngle =  135f;

    [Header("Gauge Shader")]
    [SerializeField] private float   gaugeArcStart = -135f;
    [SerializeField] private float   gaugeArcSpan  = 270f;
    [SerializeField] private Vector2 gaugeCenter   = new Vector2(0.5f, 0.5f);
    [SerializeField] private Color   gaugeColorMin = Color.green;
    [SerializeField] private Color   gaugeColorMax = Color.red;

    private WaveGenerator _wave;
    private Material      _gaugeMat;

    private void Awake()
    {
        if (knobImage == null)
            knobImage = transform.Find("knobImage")?.GetComponent<RectTransform>();
        if (knobGauge == null)
            knobGauge = transform.Find("knobGaugeMask")?.GetComponent<Image>();

        if (knobGauge != null)
            InitGaugeMaterial();
    }

    private void OnDestroy()
    {
        if (_gaugeMat != null) Destroy(_gaugeMat);
    }

    private void InitGaugeMaterial()
    {
        var shader = Shader.Find("Custom/UIGaugeFill");
        if (shader == null) return;

        _gaugeMat = new Material(shader);
        _gaugeMat.SetFloat("_ArcStart", gaugeArcStart);
        _gaugeMat.SetFloat("_ArcSpan",  gaugeArcSpan);
        _gaugeMat.SetColor("_ColorA",   gaugeColorMin);
        _gaugeMat.SetColor("_ColorB",   gaugeColorMax);
        _gaugeMat.SetVector("_Center",  new Vector4(gaugeCenter.x, gaugeCenter.y, 0f, 0f));
        knobGauge.material = _gaugeMat;
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

        float t = value / (float)Constants.INTEGER_RANGE;
        float angle = Mathf.Lerp(minAngle, maxAngle, t);
        knobImage.localEulerAngles = new Vector3(0f, 0f, angle);

        if (_gaugeMat != null)
        {
            _gaugeMat.SetFloat("_Fill",     t);
            _gaugeMat.SetFloat("_ArcStart", gaugeArcStart);
            _gaugeMat.SetFloat("_ArcSpan",  gaugeArcSpan);
            _gaugeMat.SetVector("_Center",  new Vector4(gaugeCenter.x, gaugeCenter.y, 0f, 0f));
        }
    }
}
