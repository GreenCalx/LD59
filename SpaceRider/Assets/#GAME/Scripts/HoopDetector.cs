using UnityEngine;

public class HoopDetector : MonoBehaviour
{
    [SerializeField] private float innerRadius = 2f;

    private bool _consumed;

    void OnTriggerEnter(Collider iCollider)
    {
        if (_consumed) return;
        Debug.Log("hoop traversed : " + gameObject.name);
        if (iCollider.GetComponentInParent<Surfer>() == null) return;

        _consumed = true;
        enabled   = false;
        HoopTracker.Instance?.RegisterPass();

        Debug.Log("hoop RegisterPass invoked : " + gameObject.name);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = _consumed ? Color.gray : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, innerRadius);
    }
}
