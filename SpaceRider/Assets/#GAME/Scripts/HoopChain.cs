using UnityEngine;

public class HoopChain : MonoBehaviour
{
    int _total;
    int _passed;
    int _resolved;

    void Start()
    {
        _total = GetComponentsInChildren<HoopDetector>().Length;
        Debug.Log($"[HoopChain] {gameObject.name} — {_total} hoops");
    }

    public void RegisterPass()
    {
        _passed++;
        HoopTracker.Instance?.NotifyPass();
        HoopTracker.Instance?.UpdateChain(_passed, _total);
        Resolve();
    }

    public void RegisterMiss()
    {
        Resolve();
    }

    void Resolve()
    {
        _resolved++;
        Debug.Log($"[HoopChain] {gameObject.name} — resolved {_resolved}/{_total}, passed {_passed}");
        if (_resolved < _total) return;

        bool perfect = _passed == _total && _total > 0;
        int  score   = perfect ? _total * 2 : _passed;
        Debug.Log($"[HoopChain] {gameObject.name} — chain complete! score={score} perfect={perfect}");

        HoopTracker.Instance?.AddScore(score);
        HoopTracker.Instance?.NotifyChainComplete(perfect);
        HoopTracker.Instance?.ClearChain();
    }
}
