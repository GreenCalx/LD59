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

    private bool _finished;

    public event Action OnFinish;

    private float BaseScrollSpeed    => config?.progress?.baseScrollSpeed    ?? 10f;
    private float SignalGain         => config?.progress?.signalGain         ?? 0.05f;
    private float MinSpeedMultiplier => config?.progress?.minSpeedMultiplier ?? 0.5f;
    private float MaxSpeedMultiplier => config?.progress?.maxSpeedMultiplier ?? 2f;

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

        float derivative = waveGenerator.SampleDerivativeAtHero();
        float multiplier = Mathf.Clamp(1f + SignalGain * derivative, MinSpeedMultiplier, MaxSpeedMultiplier);
        float speed      = BaseScrollSpeed * multiplier;

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
}
