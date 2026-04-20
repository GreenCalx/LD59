using UnityEngine;
using TMPro;

public class UIGameOver : MonoBehaviour
{
    public TextMeshProUGUI scoreTxt;

    void Start()
    {
        if (scoreTxt == null) return;
        int score = HoopTracker.Instance != null ? HoopTracker.Instance.TotalScore : 0;
        scoreTxt.text = score.ToString();
    }
}
