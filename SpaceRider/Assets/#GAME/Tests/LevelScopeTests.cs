using NUnit.Framework;
using UnityEngine;

public class LevelScopeTests
{
    private LevelScope MakeScope(float length)
    {
        var go = new GameObject("LevelScope");
        var scope = go.AddComponent<LevelScope>();
        scope.LevelLength = length;
        return scope;
    }

    [TearDown]
    public void Cleanup()
    {
        foreach (var go in Object.FindObjectsByType<LevelScope>(FindObjectsSortMode.None))
            Object.DestroyImmediate(go.gameObject);
    }

    [Test]
    public void Progress01_Is_Zero_At_Start()
    {
        var s = MakeScope(100f);
        Assert.AreEqual(0f, s.Progress01, 1e-6f);
    }

    [Test]
    public void Progress01_Is_Half_At_Midpoint()
    {
        var s = MakeScope(100f);
        s.VirtualDistance = 50f;
        Assert.AreEqual(0.5f, s.Progress01, 1e-6f);
    }

    [Test]
    public void Progress01_Clamps_At_One_When_Past_End()
    {
        var s = MakeScope(100f);
        s.VirtualDistance = 500f;
        Assert.AreEqual(1f, s.Progress01, 1e-6f);
    }

    [Test]
    public void Progress01_Is_One_When_LevelLength_Is_Zero()
    {
        var s = MakeScope(0f);
        Assert.AreEqual(1f, s.Progress01, 1e-6f);
    }

    [Test]
    public void IsFinished_True_When_VirtualDistance_Reaches_LevelLength()
    {
        var s = MakeScope(100f);
        s.VirtualDistance = 100f;
        Assert.IsTrue(s.IsFinished);
    }

    [Test]
    public void IsFinished_False_Before_End()
    {
        var s = MakeScope(100f);
        s.VirtualDistance = 99.99f;
        Assert.IsFalse(s.IsFinished);
    }
}
