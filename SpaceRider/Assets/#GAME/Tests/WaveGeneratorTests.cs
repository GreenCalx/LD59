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
    public void SampleDerivativeAtHero_Matches_Finite_Difference()
    {
        _scope.VirtualDistance = 0.1f;
        float analytical = _gen.SampleDerivativeAtHero();

        float h = 1e-3f;
        float yMinus = _gen.SampleAtLocalZ(-h).y;
        float yPlus  = _gen.SampleAtLocalZ(+h).y;
        float fd = (yPlus - yMinus) / (2f * h);

        Assert.AreEqual(fd, analytical, 1e-2f);
    }
}
