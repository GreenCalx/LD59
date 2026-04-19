using NUnit.Framework;
using UnityEngine;

public class BoundaryMonitorTests
{
    [Test]
    public void BoundaryT_HeroAtCenter_IsZero()
    {
        float t = BoundaryMonitor.ComputeBoundaryT(Vector3.zero, 8f);
        Assert.AreEqual(0f, t, 1e-5f);
    }

    [Test]
    public void BoundaryT_HeroAtEdge_IsOne()
    {
        float t = BoundaryMonitor.ComputeBoundaryT(new Vector3(8f, 0f, 999f), 8f);
        Assert.AreEqual(1f, t, 1e-5f);
    }

    [Test]
    public void BoundaryT_HeroBeyondEdge_ExceedsOne()
    {
        float t = BoundaryMonitor.ComputeBoundaryT(new Vector3(10f, 0f, 0f), 8f);
        Assert.Greater(t, 1f);
    }

    [Test]
    public void BoundaryT_ZAxisIgnored()
    {
        float t1 = BoundaryMonitor.ComputeBoundaryT(new Vector3(4f, 0f,   0f), 8f);
        float t2 = BoundaryMonitor.ComputeBoundaryT(new Vector3(4f, 0f, 500f), 8f);
        Assert.AreEqual(t1, t2, 1e-5f);
    }

    [Test]
    public void EffectT_BelowFeedbackThreshold_IsZero()
    {
        float t = BoundaryMonitor.ComputeEffectT(0.3f, 0.5f);
        Assert.AreEqual(0f, t, 1e-5f);
    }

    [Test]
    public void EffectT_AtFeedbackThreshold_IsZero()
    {
        float t = BoundaryMonitor.ComputeEffectT(0.5f, 0.5f);
        Assert.AreEqual(0f, t, 1e-5f);
    }

    [Test]
    public void EffectT_AtBoundary_IsOne()
    {
        float t = BoundaryMonitor.ComputeEffectT(1f, 0.5f);
        Assert.AreEqual(1f, t, 1e-5f);
    }

    [Test]
    public void EffectT_Midpoint_IsHalf()
    {
        float t = BoundaryMonitor.ComputeEffectT(0.75f, 0.5f);
        Assert.AreEqual(0.5f, t, 1e-5f);
    }
}
