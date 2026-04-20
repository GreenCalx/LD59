using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI pressAnyKeyLabel;
    [SerializeField] private GameObject     restartButton;

    [Header("Settings")]
    [SerializeField] private string titleSceneName = "title";
    [SerializeField] private float  blinkPeriod    = 1.2f;

    private bool _active;

    private void Start()
    {
        if (!GameResult.IsWin)
        {
            enabled = false;
            return;
        }

        _active = true;

        if (restartButton != null)
            restartButton.SetActive(false);

        if (pressAnyKeyLabel != null)
            pressAnyKeyLabel.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!_active) return;

        if (pressAnyKeyLabel != null)
        {
            float t     = Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / blinkPeriod)) * 0.5f + 0.5f;
            Color color = pressAnyKeyLabel.color;
            color.a     = t;
            pressAnyKeyLabel.color = color;
        }

        if (Input.anyKeyDown)
        {
            _active        = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(titleSceneName);
        }
    }
}
