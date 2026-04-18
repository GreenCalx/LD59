using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ProgressDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelScope    levelScope;
    [SerializeField] private WaveGenerator waveGenerator;
    [SerializeField] private Transform     world;
    [SerializeField] private GameConfig    config;

    private bool  _finished;
    private float _currentSpeed;
    private float _lastDerivative;
    private float _lastSlopeAccel;
    private float _lastDragAccel;

    public event Action OnFinish;

    public float CurrentSpeed   => _currentSpeed;
    public float LastDerivative => _lastDerivative;
    public float LastSlopeAccel => _lastSlopeAccel;
    public float LastDragAccel  => _lastDragAccel;

    private float BaseScrollSpeed    => config?.progress?.baseScrollSpeed    ?? 10f;
    private float SignalGain         => config?.progress?.signalGain         ?? 5f;
    private float MinSpeedMultiplier => config?.progress?.minSpeedMultiplier ?? 0.3f;
    private float MaxSpeedMultiplier => config?.progress?.maxSpeedMultiplier ?? 2.5f;
    private float DragRate           => config?.progress?.dragRate           ?? 1.5f;

    public void Setup(LevelScope scope, WaveGenerator gen) { levelScope = scope; waveGenerator = gen; }
    public void SetConfig(GameConfig c) { config = c; }

    private void Update()
    {
        if (!Application.isPlaying) return;
        Tick(Time.deltaTime);
    }

    public void Tick(float dt)
    {
        if (levelScope == null || waveGenerator == null) return;
        if (_finished) { levelScope.ScrollSpeed = 0f; return; }

        if (_currentSpeed == 0f) _currentSpeed = BaseScrollSpeed;

        float derivative  = waveGenerator.SampleDerivativeAtHero();
        float slopeAccel  = -SignalGain * derivative;
        float dragAccel   = (BaseScrollSpeed - _currentSpeed) * DragRate;
        _lastDerivative   = derivative;
        _lastSlopeAccel   = slopeAccel;
        _lastDragAccel    = dragAccel;
        _currentSpeed    += (slopeAccel + dragAccel) * dt;
        _currentSpeed     = Mathf.Clamp(_currentSpeed,
                                BaseScrollSpeed * MinSpeedMultiplier,
                                BaseScrollSpeed * MaxSpeedMultiplier);
        float speed = _currentSpeed;

        levelScope.ScrollSpeed    = speed;
        levelScope.VirtualDistance = levelScope.VirtualDistance + speed * dt;

        if (world != null)
        {
            Vector3 wp = world.position;
            wp.z = -levelScope.VirtualDistance;
            world.position = wp;
        }

        if (levelScope.IsFinished)
        {
            _finished = true;
            levelScope.ScrollSpeed = 0f;
            OnFinish?.Invoke();
        }
    }

    private void OnGUI()
    {
        if (!GameDebug.ShowGizmos) return;

        float base_   = BaseScrollSpeed;
        float minSpd  = base_ * MinSpeedMultiplier;
        float maxSpd  = base_ * MaxSpeedMultiplier;
        float range   = maxSpd - minSpd;

        GUILayout.BeginArea(new Rect(10f, 10f, 300f, 130f), GUI.skin.box);

        GUILayout.Label($"Speed:  {_currentSpeed:F2} m/s  (base {base_:F1})");

        Rect bar = GUILayoutUtility.GetRect(280f, 18f);
        GUI.Box(bar, GUIContent.none);
        if (range > 0f)
        {
            float tBase = (base_ - minSpd) / range;
            float tCur  = Mathf.Clamp01((_currentSpeed - minSpd) / range);
            Rect fill   = new Rect(bar.x, bar.y, bar.width * tCur, bar.height);
            GUI.Box(fill, GUIContent.none);
            float baseX = bar.x + bar.width * tBase;
            GUI.Box(new Rect(baseX - 1f, bar.y, 3f, bar.height), GUIContent.none);
        }

        GUILayout.Label($"Slope:      {_lastDerivative:+0.000;-0.000; 0.000}");
        GUILayout.Label($"Slope accel:{_lastSlopeAccel:+0.00;-0.00; 0.00} m/s²");
        GUILayout.Label($"Drag accel: {_lastDragAccel:+0.00;-0.00; 0.00} m/s²");

        GUILayout.EndArea();
    }
}
