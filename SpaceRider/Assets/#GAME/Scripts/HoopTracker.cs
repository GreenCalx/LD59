using System;
using UnityEngine;

public class HoopTracker : MonoBehaviour
{
    public static HoopTracker Instance { get; private set; }

    public int TotalScore { get; private set; }

    public event Action       OnPassRegistered;
    public event Action<bool> OnChainComplete;
    public event Action<int>  OnScoreChanged;
    public event Action<int, int> OnChainUpdated; // (passed, total) — (0,0) clears

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public void AddScore(int points)
    {
        if (points <= 0) return;
        TotalScore += points;
        OnScoreChanged?.Invoke(TotalScore);
    }

    public void NotifyPass()                      => OnPassRegistered?.Invoke();
    public void NotifyChainComplete(bool perfect) => OnChainComplete?.Invoke(perfect);
    public void UpdateChain(int passed, int total) => OnChainUpdated?.Invoke(passed, total);
    public void ClearChain()                       => OnChainUpdated?.Invoke(0, 0);
}
