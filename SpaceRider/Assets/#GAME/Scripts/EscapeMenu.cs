using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Escape / pause menu with FMOD global volume sliders.
///
/// Setup in Unity:
///  1. Create a Canvas > Panel (name it e.g. "EscapeMenuPanel").
///  2. Inside the Panel add three Slider components and a "Resume" Button.
///  3. Attach this script to any persistent GameObject in the scene.
///  4. Wire the serialised fields below in the Inspector.
///  5. The Panel will be hidden at runtime start; pressing Escape toggles it.
/// </summary>
public class EscapeMenu : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject menuPanel;

    [Header("Volume Sliders (0 – 1)")]
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider fxSlider;

    const string PREF_MASTER = "vol_master";
    const string PREF_MUSIC  = "vol_music";
    const string PREF_FX     = "vol_fx";

    bool _isPaused;

    void Start()
    {
        // Restore saved values (default 1).
        float master = PlayerPrefs.GetFloat(PREF_MASTER, 1f);
        float music  = PlayerPrefs.GetFloat(PREF_MUSIC,  1f);
        float fx     = PlayerPrefs.GetFloat(PREF_FX,     1f);

        InitSlider(masterSlider, master, OnMasterChanged);
        InitSlider(musicSlider,  music,  OnMusicChanged);
        InitSlider(fxSlider,     fx,     OnFxChanged);

        // Push saved values into FMOD immediately.
        SetFmodParam("master_volume", master);
        SetFmodParam("music_volume",  music);
        SetFmodParam("fx_volume",     fx);

        menuPanel.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Toggle();
    }

    public void Toggle()
    {
        _isPaused = !_isPaused;
        menuPanel.SetActive(_isPaused);
        Time.timeScale = _isPaused ? 0f : 1f;
    }

    public void Resume()
    {
        if (!_isPaused) return;
        Toggle();
    }

    // --- slider callbacks ---

    void OnMasterChanged(float v)
    {
        SetFmodParam("master_volume", v);
        PlayerPrefs.SetFloat(PREF_MASTER, v);
    }

    void OnMusicChanged(float v)
    {
        SetFmodParam("music_volume", v);
        PlayerPrefs.SetFloat(PREF_MUSIC, v);
    }

    void OnFxChanged(float v)
    {
        SetFmodParam("fx_volume", v);
        PlayerPrefs.SetFloat(PREF_FX, v);
    }

    // --- helpers ---

    static void InitSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = value;
        slider.onValueChanged.AddListener(callback);
    }

    static void SetFmodParam(string name, float value)
    {
        var result = FMODUnity.RuntimeManager.StudioSystem.setParameterByName(name, value);
        if (result != FMOD.RESULT.OK)
            Debug.LogWarning($"[EscapeMenu] FMOD setParameterByName('{name}') failed: {result}");
    }
}
