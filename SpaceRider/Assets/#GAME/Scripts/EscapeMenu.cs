using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EscapeMenu : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject menuPanel;

    [Header("Volume Sliders (0 – 1)")]
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider fxSlider;

    [Header("Resume Button")]
    [SerializeField] Button resumeButton;

    [Header("Navigation")]
    [SerializeField] float sliderStep     = 0.05f;  // per key press
    [SerializeField] float navCooldown    = 0.2f;   // seconds between nav steps (unscaled)

    const string PREF_MASTER = "vol_master";
    const string PREF_MUSIC  = "vol_music";
    const string PREF_FX     = "vol_fx";

    Selectable[] _items;   // ordered: master, music, fx, resume
    int   _selectedIndex;
    float _navNextTime;

    void Start()
    {
        float master = PlayerPrefs.GetFloat(PREF_MASTER, 1f);
        float music  = PlayerPrefs.GetFloat(PREF_MUSIC,  1f);
        float fx     = PlayerPrefs.GetFloat(PREF_FX,     1f);

        InitSlider(masterSlider, master, OnMasterChanged);
        InitSlider(musicSlider,  music,  OnMusicChanged);
        InitSlider(fxSlider,     fx,     OnFxChanged);

        SetFmodParam("master_volume", master);
        SetFmodParam("music_volume",  music);
        SetFmodParam("fx_volume",     fx);

        _items = new Selectable[] { masterSlider, musicSlider, fxSlider, resumeButton };

        menuPanel.SetActive(false);
    }

    void Update()
    {
        if ((Keyboard.current?.escapeKey.wasPressedThisFrame ?? false)
         || (Gamepad.current?.startButton.wasPressedThisFrame ?? false))
            Toggle();

        if (!_isPaused) return;

        HandleNavigation();
        HandleSliderAdjust();
        HandleSubmit();
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    void HandleNavigation()
    {
        if (Time.unscaledTime < _navNextTime) return;

        bool up   = (Keyboard.current?.wKey.isPressed ?? false)
                 || (Gamepad.current?.dpad.up.isPressed ?? false)
                 || (Gamepad.current != null && Gamepad.current.leftStick.y.ReadValue() >  0.5f);

        bool down = (Keyboard.current?.sKey.isPressed ?? false)
                 || (Gamepad.current?.dpad.down.isPressed ?? false)
                 || (Gamepad.current != null && Gamepad.current.leftStick.y.ReadValue() < -0.5f);

        if (up)   SetSelection(_selectedIndex - 1);
        if (down) SetSelection(_selectedIndex + 1);
    }

    void HandleSliderAdjust()
    {
        Selectable sel = _items[_selectedIndex];
        if (!(sel is Slider slider)) return;

        bool left  = (Keyboard.current?.aKey.isPressed ?? false)
                  || (Gamepad.current?.dpad.left.isPressed ?? false)
                  || (Gamepad.current != null && Gamepad.current.leftStick.x.ReadValue() < -0.5f);

        bool right = (Keyboard.current?.dKey.isPressed ?? false)
                  || (Gamepad.current?.dpad.right.isPressed ?? false)
                  || (Gamepad.current != null && Gamepad.current.leftStick.x.ReadValue() >  0.5f);

        if (left || right)
        {
            if (Time.unscaledTime >= _navNextTime)
            {
                slider.value = Mathf.Clamp01(slider.value + (right ? sliderStep : -sliderStep));
                _navNextTime = Time.unscaledTime + navCooldown;
            }
        }
    }

    void HandleSubmit()
    {
        bool submit = (Keyboard.current?.enterKey.wasPressedThisFrame  ?? false)
                   || (Keyboard.current?.spaceKey.wasPressedThisFrame  ?? false)
                   || (Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false);

        if (!submit) return;

        Selectable sel = _items[_selectedIndex];
        if (sel is Button btn)
            btn.onClick.Invoke();
    }

    void SetSelection(int index)
    {
        _selectedIndex = Mathf.Clamp(index, 0, _items.Length - 1);
        EventSystem.current?.SetSelectedGameObject(_items[_selectedIndex].gameObject);
        _navNextTime = Time.unscaledTime + navCooldown;
    }

    // ── Toggle ────────────────────────────────────────────────────────────────

    bool _isPaused;

    public void Toggle()
    {
        _isPaused = !_isPaused;
        menuPanel.SetActive(_isPaused);
        Time.timeScale = _isPaused ? 0f : 1f;

        if (_isPaused)
            SetSelection(0);  // always start on Master slider
        else
            EventSystem.current?.SetSelectedGameObject(null);
    }

    public void Resume()
    {
        if (!_isPaused) return;
        Toggle();
    }

    // ── Slider callbacks ──────────────────────────────────────────────────────

    void OnMasterChanged(float v) { SetFmodParam("master_volume", v); PlayerPrefs.SetFloat(PREF_MASTER, v); }
    void OnMusicChanged (float v) { SetFmodParam("music_volume",  v); PlayerPrefs.SetFloat(PREF_MUSIC,  v); }
    void OnFxChanged    (float v) { SetFmodParam("fx_volume",     v); PlayerPrefs.SetFloat(PREF_FX,     v); }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
