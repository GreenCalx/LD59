using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Static goal marker placed inside the World GameObject at local Z = LevelLength.
/// As ProgressDriver scrolls World toward the hero, this object naturally closes in
/// on Z = 0. The camera can target it directly as a vanishing point.
///
/// When ProgressDriver fires OnFinish, time stops and the gameover scene is loaded
/// additively on top of the current scene.
/// </summary>
public class FinishLine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProgressDriver progressDriver;

    [Header("Scene")]
    [SerializeField] private string gameOverSceneName = "gameover";

    private bool _triggered;

    private void OnEnable()
    {
        if (progressDriver != null)
            progressDriver.OnFinish += HandleFinish;
    }

    private void OnDisable()
    {
        if (progressDriver != null)
            progressDriver.OnFinish -= HandleFinish;
    }

    private void HandleFinish()
    {
        if (_triggered) return;
        _triggered = true;

        Time.timeScale = 0f;

        if (Application.isPlaying && !string.IsNullOrEmpty(gameOverSceneName))
            SceneManager.LoadSceneAsync(gameOverSceneName, LoadSceneMode.Additive);
    }
}
