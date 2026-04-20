using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "main";

    [Header("Input")]
    [SerializeField] private InputAction startAction = new InputAction("Start", InputActionType.Button);

    private bool _loading;

    private void Awake()
    {
        EnsureBinding(startAction, "<Keyboard>/space");
        EnsureBinding(startAction, "<Keyboard>/enter");
        EnsureBinding(startAction, "<Gamepad>/start");
        EnsureBinding(startAction, "<Gamepad>/buttonSouth");
    }

    private void OnEnable()  => startAction.Enable();
    private void OnDisable() => startAction.Disable();

    private void Update()
    {
        if (_loading) return;
        if (startAction.WasPressedThisFrame())
            Load();
    }

    private void Load()
    {
        _loading = true;
        SceneManager.LoadScene(mainSceneName);
    }

    private static void EnsureBinding(InputAction action, string path)
    {
        foreach (var b in action.bindings)
            if (b.path == path) return;
        action.AddBinding(path);
    }
}
