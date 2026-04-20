using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishLine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProgressDriver progressDriver;
    [SerializeField] private GameConfig     config;

    [Header("Scene")]
    [SerializeField] private string gameOverSceneName = "gameover";

    private bool _triggered;

    private void OnEnable()  { if (progressDriver != null) progressDriver.OnFinish += HandleFinish; }
    private void OnDisable() { if (progressDriver != null) progressDriver.OnFinish -= HandleFinish; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerDeath>() == null) return;
        HandleFinish();
    }

    private void HandleFinish()
    {
        if (_triggered) return;
        _triggered = true;
        GameResult.IsWin = true;
        Time.timeScale = 0f;
        if (Application.isPlaying && !string.IsNullOrEmpty(gameOverSceneName))
            SceneManager.LoadSceneAsync(gameOverSceneName, LoadSceneMode.Additive);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!GameDebug.ShowGizmos) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(10f, 5f, 0.5f));
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 3f,
            $"Finish  z={transform.position.z:F1}m");
    }
#endif
}
