using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "main";

    private bool _loading;

    private void Update()
    {
        if (_loading) return;
        if (Input.anyKeyDown)
            Load();
    }

    private void Load()
    {
        _loading = true;
        SceneManager.LoadScene(mainSceneName);
    }
}
