using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingToast : MonoBehaviour
{
    TMP_Text _label;

    void Awake() => _label = GetComponent<TMP_Text>();

    public void Play(string text, Color color)
    {
        _label.text  = text;
        _label.color = color;
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        const float duration   = 1.4f;
        const float risePixels = 90f;

        var     rt         = (RectTransform)transform;
        Vector2 startPos   = rt.anchoredPosition;
        Color   startColor = _label.color;
        float   elapsed    = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            rt.anchoredPosition = startPos + Vector2.up * (risePixels * t);

            float alpha = t < 0.35f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.35f) / 0.65f);
            _label.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }

        Destroy(gameObject);
    }
}
