using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lives on the root of the gameover scene. Wire the Restart button's
/// onClick event to the <see cref="Restart"/> method in the inspector.
/// </summary>
public class GameOverController : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "main";

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainSceneName);
    }
}
