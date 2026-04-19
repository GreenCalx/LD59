using UnityEngine;

public class HoopDetector : MonoBehaviour
{
    [SerializeField] private float innerRadius = 2f;

    private bool _consumed;

    void OnTriggerEnter(Collider iCollider)
    {
        if (_consumed) return;
        if (iCollider.GetComponent<HoopCollector>() == null) return;

        _consumed = true;
        enabled   = false;
        HoopTracker.Instance?.RegisterPass();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = _consumed ? Color.gray : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, innerRadius);
    }
}
