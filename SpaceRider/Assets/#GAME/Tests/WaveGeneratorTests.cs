using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

public class WaveGeneratorTests
{
    private LevelScope    _scope;
    private WaveGenerator _gen;
    private GameConfig    _config;

    [SetUp]
    public void SetUp()
    {
        var lc = ScriptableObject.CreateInstance<LevelConfig>();
        lc.levelLength = 1000f; lc.lookAhead = 30f; lc.decayLength = 5f; lc.playfieldRadius = 8f;

        var wc = ScriptableObject.CreateInstance<WaveGeneratorConfig>();
        wc.sampleDensity = 4f; wc.paramSmoothingDistance = 2f; wc.panLateralScale = 0.2f; wc.bpm = 120f;

        _config = ScriptableObject.CreateInstance<GameConfig>();
        _config.level = lc; _config.waveGenerator = wc;

        var scopeGo = new GameObject("Scope");
        _scope = scopeGo.AddComponent<LevelScope>();
        _scope.SetConfig(_config);

        var genGo = new GameObject("Gen");
        _gen = genGo.AddComponent<WaveGenerator>();
        _gen.SetLevelScope(_scope);
        _gen.SetConfig(_config);
        _gen.Amplitude = 1; _gen.Frequency = 1; _gen.Pan = 0;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gen.gameObject);
        Object.DestroyImmediate(_scope.gameObject);
    }

    [Test]
    public void GetWavePoints_Spans_LocalZ_From_Negative_Decay_To_Plus_LookAhead()
    {
        var pts = _gen.GetWavePoints(32, Allocator.Temp);
        Assert.AreEqual(-_config.level.decayLength, pts[0].z, 1e-4f);
        Assert.AreEqual(+_config.level.lookAhead,   pts[pts.Length - 1].z, 1e-4f);
        pts.Dispose();
    }

    [Test]
    public void Wave_Scrolls_When_VirtualDistance_Changes()
    {
        _scope.VirtualDistance = 0f;
        var a = _gen.GetWavePoints(64, Allocator.Temp);
        float yBefore = a[32].y; a.Dispose();

        _scope.VirtualDistance = 0.25f;
        var b = _gen.GetWavePoints(64, Allocator.Temp);
        float yAfter = b[32].y; b.Dispose();

        Assert.AreNotEqual(yBefore, yAfter, "Wave Y must change when VirtualDistance advances.");
    }

    [Test]
    public void Parameter_Change_Does_Not_Reshape_Already_Spawned_Samples()
    {
        _gen.Amplitude = 1;
        float yBefore = _gen.SampleAtLocalZ(10f).y;
        _gen.Amplitude = 100;
        _gen.Tick(0f);
        Assert.AreEqual(yBefore, _gen.SampleAtLocalZ(10f).y, 1e-4f);
    }

    [Test]
    public void New_Amplitude_Only_Reaches_Samples_Spawned_After_Change()
    {
        _gen.Amplitude = 1;
        _gen.Tick(1f);
        _scope.VirtualDistance = _config.level.lookAhead + _config.level.decayLength + 1f;
        _gen.Amplitude = 500;
        _gen.Tick(0f);

        float peak = 0f;
        for (int i = 0; i < 64; i++)
        {
            float z = Mathf.Lerp(_config.level.lookAhead * 0.5f, _config.level.lookAhead * 0.95f, i / 63f);
            peak = Mathf.Max(peak, Mathf.Abs(_gen.SampleAtLocalZ(z).y));
        }
        Assert.Greater(peak, 2f, "Newly-spawned samples should reflect the larger amplitude.");
    }
}
