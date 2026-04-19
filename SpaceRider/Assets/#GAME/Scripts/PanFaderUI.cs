using UnityEngine;

public class PanFaderUI : MonoBehaviour
{
    [SerializeField] private RectTransform head;
    [SerializeField] private RectTransform leftPoint;
    [SerializeField] private RectTransform rightPoint;

    private WaveGenerator _wave;

    private void Awake()
    {
        if (head       == null) head       = transform.Find("PanFaderHeroHead")?.GetComponent<RectTransform>();
        if (leftPoint  == null) leftPoint  = transform.Find("LeftPoint")?.GetComponent<RectTransform>();
        if (rightPoint == null) rightPoint = transform.Find("RightPoint")?.GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        if (_wave == null)
        {
            _wave = GameServices.Instance?.waveGenerator;
            if (_wave == null) return;
        }
        if (head == null || leftPoint == null || rightPoint == null) return;

        float t = _wave.Pan / (float)Constants.INTEGER_RANGE;
        head.anchoredPosition = Vector2.Lerp(leftPoint.anchoredPosition, rightPoint.anchoredPosition, t);
    }
}
