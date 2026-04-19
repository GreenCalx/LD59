using TMPro;
using UnityEngine;

public class HoopCounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text totalText;
    [SerializeField] private TMP_Text comboText;

    void OnEnable()
    {
        if (HoopTracker.Instance != null)
            HoopTracker.Instance.OnCountersChanged += UpdateUI;
        UpdateUI(HoopTracker.Instance?.TotalPassed ?? 0, HoopTracker.Instance?.Combo ?? 0);
    }

    void OnDisable()
    {
        if (HoopTracker.Instance != null)
            HoopTracker.Instance.OnCountersChanged -= UpdateUI;
    }

    void UpdateUI(int total, int combo)
    {
        if (totalText != null) totalText.text = $"HOOPS  {total}";
        if (comboText != null) comboText.text = $"COMBO  x{combo}";
    }
}
