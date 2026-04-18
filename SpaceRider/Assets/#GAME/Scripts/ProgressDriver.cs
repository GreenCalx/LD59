using System;
using UnityEngine;

/// <summary>
/// Drives virtualDistance on LevelScope using a constant base speed modulated
/// by the wave's derivative at the hero. Clamped to [minMult, maxMult] of the
/// base speed and to LevelLength.
/// </summary>
[DefaultExecutionOrder(-100)]
public class ProgressDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelScope    levelScope;
    [SerializeField] private WaveGenerator waveGenerator;

    [Header("Speed")]
    [SerializeField] private float baseScrollSpeed    = 10f;
    [SerializeField] private float signalGain         = 0.05f;
    [SerializeField] private float minSpeedMultiplier = 0.5f;
    [SerializeField] private float maxSpeedMultiplier = 2.0f;

    private bool _finished;

    public event Action OnFinish;

    public float BaseScrollSpeed    { get => baseScrollSpeed;    set => baseScrollSpeed = value; }
    public float SignalGain         { get => signalGain;         set => signalGain = value; }
    public float MinSpeedMultiplier { get => minSpeedMultiplier; set => minSpeedMultiplier = value; }
    public float MaxSpeedMultiplier { get => maxSpeedMultiplier; set => maxSpeedMultiplier = value; }

    public void Setup(LevelScope scope, WaveGenerator gen)
    {
        levelScope = scope;
        waveGenerator = gen;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        Tick(Time.deltaTime);
    }

    /// <summary>Advances the treadmill by dt seconds. Exposed for tests.</summary>
    public void Tick(float dt)
    {
        if (levelScope == null || waveGenerator == null) return;

        if (_finished)
        {
            levelScope.ScrollSpeed = 0f;
            return;
        }

        float derivative = waveGenerator.SampleDerivativeAtHero();
        float multiplier = Mathf.Clamp(
            1f + signalGain * derivative,
            minSpeedMultiplier,
            maxSpeedMultiplier);

        float speed = baseScrollSpeed * multiplier;
        levelScope.ScrollSpeed = speed;
        levelScope.VirtualDistance = levelScope.VirtualDistance + speed * dt;

        if (levelScope.IsFinished)
        {
            _finished = true;
            levelScope.ScrollSpeed = 0f;
            OnFinish?.Invoke();
        }
    }
}
