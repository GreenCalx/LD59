using NUnit.Framework;
using UnityEngine;

public class ProgressDriverTests
{
    private LevelScope     _scope;
    private WaveGenerator  _gen;
    private ProgressDriver _driver;
    private GameConfig     _config;

    [SetUp]
    public void SetUp()
    {
        var lc = ScriptableObject.CreateInstance<LevelConfig>();
        lc.levelLength = 1000f; lc.lookAhead = 30f; lc.decayLength = 5f; lc.playfieldRadius = 8f;

        var pc = ScriptableObject.CreateInstance<ProgressConfig>();
        pc.baseScrollSpeed = 10f; pc.signalGain = 0f;
        pc.minSpeedMultiplier = 0.5f; pc.maxSpeedMultiplier = 2f;

        var wc = ScriptableObject.CreateInstance<WaveGeneratorConfig>();
        wc.sampleDensity = 4f; wc.paramSmoothingDistance = 2f; wc.panLateralScale = 0.2f; wc.bpm = 0f;

        _config = ScriptableObject.CreateInstance<GameConfig>();
        _config.level = lc; _config.progress = pc; _config.waveGenerator = wc;

        var scopeGo = new GameObject("Scope");
        _scope = scopeGo.AddComponent<LevelScope>();
        _scope.SetConfig(_config);

        var genGo = new GameObject("Gen");
        _gen = genGo.AddComponent<WaveGenerator>();
        _gen.SetLevelScope(_scope);
        _gen.SetConfig(_config);
        _gen.Amplitude = 0f; _gen.Frequency = 1f;

        var drvGo = new GameObject("Drv");
        _driver = drvGo.AddComponent<ProgressDriver>();
        _driver.Setup(_scope, _gen);
        _driver.SetConfig(_config);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_driver.gameObject);
        Object.DestroyImmediate(_gen.gameObject);
        Object.DestroyImmediate(_scope.gameObject);
    }

    [Test]
    public void Tick_With_Zero_Gain_Advances_At_BaseSpeed()
    {
        for (int i = 0; i < 100; i++) _driver.Tick(0.1f);
        Assert.AreEqual(100f, _scope.VirtualDistance, 1e-3f);
        Assert.AreEqual(10f,  _scope.ScrollSpeed,     1e-3f);
    }

    [Test]
    public void Tick_Clamps_Speed_By_MaxMultiplier()
    {
        _config.progress.signalGain = 1000f;
        _config.progress.maxSpeedMultiplier = 2f;
        _gen.Amplitude = 1f;
        _scope.VirtualDistance = 0f;
        _driver.Tick(0.016f);
        Assert.LessOrEqual(_scope.ScrollSpeed, 10f * 2f + 1e-3f);
    }

    [Test]
    public void Tick_Clamps_VirtualDistance_To_LevelLength()
    {
        _scope.VirtualDistance = 999f;
        _driver.Tick(5f);
        Assert.AreEqual(1000f, _scope.VirtualDistance, 1e-3f);
        Assert.IsTrue(_scope.IsFinished);
    }

    [Test]
    public void OnFinish_Fires_Exactly_Once()
    {
        int calls = 0;
        _driver.OnFinish += () => calls++;
        _scope.VirtualDistance = 999f;
        _driver.Tick(5f); _driver.Tick(5f); _driver.Tick(5f);
        Assert.AreEqual(1, calls);
    }
}
