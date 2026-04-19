using UnityEngine;

public class MissDetector : MonoBehaviour
{
    HoopDetector _hoop;

    void Awake()
    {
        _hoop = transform.parent.GetComponentInChildren<HoopDetector>();
    }

    void OnTriggerEnter(Collider iCollider)
    {
        if (_hoop == null || _hoop.IsConsumed) return;
        if (iCollider.GetComponentInParent<Surfer>() == null) return;

        _hoop.ForceConsume();
        GetComponentInParent<HoopChain>()?.RegisterMiss();
        enabled = false;
        Debug.Log($"[MissDetector] miss on {transform.parent.name}");
    }
}
