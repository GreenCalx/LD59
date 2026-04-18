using NUnit.Framework;
using UnityEngine;

public class ProgressDriverTests
{
    private LevelScope    _scope;
    private WaveGenerator _gen;
    private ProgressDriver _driver;

    [SetUp]
    public void SetUp()
    {
        var scopeGo = new GameObject("Scope");
        _scope = scopeGo.AddComponent<LevelScope>();
        _scope.LevelLength = 1000f;
        _scope.LookAhead = 30f;
        _scope.DecayLength = 5f;

        var genGo = new GameObject("Gen");
        _gen = genGo.AddComponent<WaveGenerator>();
        _gen.SetLevelScope(_scope);
        _gen.Amplitude = 0f;
        _gen.Frequency = 1f;
        _gen.Bpm = 0f;

        var drvGo = new GameObject("Drv");
        _driver = drvGo.AddComponent<ProgressDriver>();
        _driver.Setup(_scope, _gen);
        _driver.BaseScrollSpeed = 10f;
        _driver.SignalGain = 0f;
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
        Assert.AreEqual(10f, _scope.ScrollSpeed, 1e-3f);
    }

    [Test]
    public void Tick_Clamps_Speed_By_MaxMultiplier()
    {
        _driver.BaseScrollSpeed = 10f;
        _driver.SignalGain = 1000f;
        _driver.MaxSpeedMultiplier = 2f;
        _gen.Amplitude = 1f;
        _scope.VirtualDistance = 0f;

        _driver.Tick(0.016f);

        Assert.LessOrEqual(_scope.ScrollSpeed, 10f * 2f + 1e-3f);
    }

    [Test]
    public void Tick_Clamps_VirtualDistance_To_LevelLength()
    {
        _scope.VirtualDistance = 999f;
        _driver.BaseScrollSpeed = 10f;

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

        _driver.Tick(5f);
        _driver.Tick(5f);
        _driver.Tick(5f);

        Assert.AreEqual(1, calls);
    }
}
