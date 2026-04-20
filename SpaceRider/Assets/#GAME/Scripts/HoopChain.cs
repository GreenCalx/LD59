using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class HoopChain : MonoBehaviour
{
    [SerializeField] EventReference PerfectChainSound;

    int                 _total;
    int                 _passed;
    int                 _resolved;
    int                 _nextIndex;
    List<HoopDetector>  _hoops   = new();
    List<HoopVisual>    _visuals = new();

    void Start()
    {
        // Build list sorted by sibling index so order matches spline placement.
        var detectors = GetComponentsInChildren<HoopDetector>();
        _hoops = new List<HoopDetector>(detectors);
        // Sibling order matches spline placement order because HoopCurvePath.PlaceHoops
        // appends children sequentially (spline 0 first, then spline 1, etc.).
        _hoops.Sort((a, b) =>
            a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        _total = _hoops.Count;
        Debug.Log($"[HoopChain] {gameObject.name} — {_total} hoops");

        _visuals = _hoops.ConvertAll(h => h.GetComponentInParent<HoopVisual>(true));

        if (_total > 0)
            _visuals[0]?.SetHighlight(true);
    }

    // Clears highlight on the current hoop, advances index, highlights the next one.
    void AdvanceHighlight()
    {
        if (_nextIndex < _visuals.Count)
            _visuals[_nextIndex]?.SetHighlight(false);

        _nextIndex++;

        if (_nextIndex < _visuals.Count)
            _visuals[_nextIndex]?.SetHighlight(true);
    }

    public void RegisterPass()
    {
        AdvanceHighlight();
        _passed++;
        HoopTracker.Instance?.NotifyPass();
        HoopTracker.Instance?.UpdateChain(_passed, _total);
        Resolve();
    }

    public void RegisterMiss()
    {
        AdvanceHighlight();
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

        if (perfect && !PerfectChainSound.IsNull)
            RuntimeManager.PlayOneShot(PerfectChainSound, transform.position);

        HoopTracker.Instance?.AddScore(score);
        HoopTracker.Instance?.NotifyChainComplete(perfect);
        HoopTracker.Instance?.ClearChain();
    }
}
