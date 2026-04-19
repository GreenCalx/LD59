using UnityEngine;

public class HoopDetector : MonoBehaviour
{
    public bool IsConsumed { get; private set; }

    void OnTriggerStay(Collider iCollider)
    {
        if (IsConsumed) return;
        if (iCollider.GetComponentInParent<Surfer>() == null) return;

        IsConsumed = true;
        enabled = false;
        GetComponentInParent<HoopChain>()?.RegisterPass();
        Debug.Log($"[HoopDetector] pass on {transform.parent.name}");
    }

    public void ForceConsume()
    {
        IsConsumed = true;
        enabled = false;
    }
}
