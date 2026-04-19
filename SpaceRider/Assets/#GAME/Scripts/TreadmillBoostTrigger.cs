using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TreadmillBoostTrigger : MonoBehaviour
{
    [Tooltip("The booster to activate when the hero crosses this trigger.")]
    [SerializeField] private TreadmillBooster target;

    private bool _fired;

    private void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (other.GetComponentInParent<Surfer>() == null) return;

        _fired  = true;
        enabled = false;
        target?.Activate();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (target == null) return;
        Gizmos.color = _fired ? Color.gray : Color.yellow;
        Gizmos.DrawLine(transform.position, target.transform.position);
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
#endif
}
