using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

public class WaveGeneratorTests
{
    private LevelScope _scope;
    private WaveGenerator _gen;

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
        _gen.Amplitude = 1f;
        _gen.Frequency = 1f;
        _gen.Pan = 0f;
        _gen.Bpm = 0f;
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
        Assert.AreEqual(-_scope.DecayLength, pts[0].z, 1e-4f);
        Assert.AreEqual(+_scope.LookAhead,   pts[pts.Length - 1].z, 1e-4f);
        pts.Dispose();
    }

    [Test]
    public void Wave_Scrolls_When_VirtualDistance_Changes()
    {
        _scope.VirtualDistance = 0f;
        var a = _gen.GetWavePoints(64, Allocator.Temp);
        float yBefore = a[32].y;
        a.Dispose();

        _scope.VirtualDistance = 0.25f;
        var b = _gen.GetWavePoints(64, Allocator.Temp);
        float yAfter = b[32].y;
        b.Dispose();

        Assert.AreNotEqual(yBefore, yAfter, "Wave Y at the same local Z must change when VirtualDistance advances.");
    }

    [Test]
    public void Parameter_Change_Does_Not_Reshape_Already_Spawned_Samples()
    {
        // Force the buffer to initialize at amplitude = 1.
        _gen.Amplitude = 1f;
        float yBefore = _gen.SampleAtLocalZ(10f).y;

        // Bump amplitude without advancing VirtualDistance. No new samples
        // should spawn, so the sample already at localZ=10 must keep its
        // frozen Y — the hallmark of the propagating model.
        _gen.Amplitude = 100f;
        _gen.Tick(0f);

        float yAfter = _gen.SampleAtLocalZ(10f).y;
        Assert.AreEqual(yBefore, yAfter, 1e-4f);
    }

    [Test]
    public void New_Amplitude_Only_Reaches_Samples_Spawned_After_Change()
    {
        // Drain the initial buffer by scrolling the full look-ahead window at amp=1.
        _gen.Amplitude = 1f;
        _gen.Tick(0f);
        _scope.VirtualDistance = _scope.LookAhead + _scope.DecayLength + 1f;

        // Jump amplitude and let new samples spawn into the freshly-exposed window.
        _gen.Amplitude = 10f;
        _gen.Tick(0f);

        // Scan well ahead of the hero. Peak magnitude over a window averages
        // out the sine phase, so it should land near the new (smoothed) amp.
        float peak = 0f;
        for (int i = 0; i < 64; i++)
        {
            float z = Mathf.Lerp(_scope.LookAhead * 0.5f, _scope.LookAhead * 0.95f, i / 63f);
            peak = Mathf.Max(peak, Mathf.Abs(_gen.SampleAtLocalZ(z).y));
        }
        Assert.Greater(peak, 2f, "Newly-spawned samples should reflect the larger amplitude.");
    }
}
