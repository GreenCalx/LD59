using TMPro;
using UnityEngine;

public class HoopCounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text totalText;
    [SerializeField] private TMP_Text comboText;

    void OnEnable()
    {
        if (HoopTracker.Instance != null)
        {
            HoopTracker.Instance.OnScoreChanged += UpdateScore;
            HoopTracker.Instance.OnChainUpdated += UpdateChain;
        }
        UpdateScore(HoopTracker.Instance?.TotalScore ?? 0);
        UpdateChain(0, 0);
    }

    void OnDisable()
    {
        if (HoopTracker.Instance != null)
        {
            HoopTracker.Instance.OnScoreChanged -= UpdateScore;
            HoopTracker.Instance.OnChainUpdated -= UpdateChain;
        }
    }

    void UpdateScore(int score)
    {
        if (totalText != null) totalText.text = $"SCORE  {score}";
    }

    void UpdateChain(int passed, int total)
    {
        if (comboText == null) return;
        comboText.text = (total > 0 && passed > 0 && passed < total)
            ? $"CHAIN  {passed}/{total}"
            : "";
    }
}
