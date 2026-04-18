using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    public void Start()
    {
        SceneManager.LoadScene("main");
    }
}
