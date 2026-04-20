using FMODUnity;
using UnityEngine;
using UnityEngine.Events;

public class HoopDetector : MonoBehaviour
{
    [SerializeField] UnityEvent     OnHoopCollected;
    [SerializeField] EventReference CollectSound;

    public bool IsConsumed { get; private set; }

    HoopVisual _visual;

    void Awake()
    {
        _visual = GetComponentInParent<HoopVisual>(true);
    }

    void OnTriggerEnter(Collider iCollider)
    {
        if (IsConsumed) return;
        if (iCollider.GetComponentInParent<Surfer>() == null) return;

        IsConsumed = true;
        enabled    = false;

        // 1. Advance chain highlight + score accounting.
        GetComponentInParent<HoopChain>()?.RegisterPass();

        // 2. Begin shader dissolve first so the visual hand-off is authoritative
        //    before any listener in OnHoopCollected can touch the GameObject.
        _visual?.TriggerDissolve();

        // 3. Play per-hoop collection SFX via FMOD (one-shot, detached from GO).
        if (!CollectSound.IsNull)
            RuntimeManager.PlayOneShot(CollectSound, transform.position);

        // 4. Fire designer-wirable VFX callback last.
        OnHoopCollected.Invoke();

        Debug.Log($"[HoopDetector] pass on {transform.parent.name}");
    }

    public void ForceConsume()
    {
        IsConsumed = true;
        enabled    = false;
    }
}
