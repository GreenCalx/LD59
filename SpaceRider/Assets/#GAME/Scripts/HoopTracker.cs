using System;
using UnityEngine;

public class HoopTracker : MonoBehaviour
{
    public static HoopTracker Instance { get; private set; }

    public int TotalPassed { get; private set; }
    public int Combo       { get; private set; }

    public event Action<int, int> OnCountersChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public void RegisterPass()
    {
        TotalPassed++;
        Combo++;
        OnCountersChanged?.Invoke(TotalPassed, Combo);
    }

    public void RegisterMiss()
    {
        if (Combo == 0) return;
        Combo = 0;
        OnCountersChanged?.Invoke(TotalPassed, Combo);
    }
}
