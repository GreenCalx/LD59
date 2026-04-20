using UnityEngine;

public class TreadmillBooster : MonoBehaviour
{
    [Tooltip("Extra world-units/sec moved each frame in local space.")]
    [Min(0f)] public float extraSpeed = 20f;

    [Tooltip("Direction of movement in local space. Default (0,0,-1) preserves original -Z scroll behaviour.")]
    public Vector3 direction = new Vector3(0f, 0f, -1f);

    [Tooltip("If true, boost starts immediately on play. If false, wait for Activate().")]
    public bool activeOnStart = false;

    private void Start()
    {
        enabled = activeOnStart;
    }

    public void Activate()   => enabled = true;
    public void Deactivate() => enabled = false;

    private void Update()
    {
        transform.localPosition += direction.normalized * (extraSpeed * Time.deltaTime);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = enabled || activeOnStart ? Color.yellow : new Color(1f, 1f, 0f, 0.25f);
        Vector3 tip = transform.position + transform.TransformDirection(direction.normalized) * -2f;
        Gizmos.DrawLine(transform.position, tip);
        Gizmos.DrawWireSphere(tip, 0.3f);
    }
#endif
}
