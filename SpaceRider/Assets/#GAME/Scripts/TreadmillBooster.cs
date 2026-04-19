using UnityEngine;

public class TreadmillBooster : MonoBehaviour
{
    [Tooltip("Extra world-units/sec added on top of the treadmill scroll speed (toward the hero).")]
    [Min(0f)] public float extraSpeed = 20f;

    [Tooltip("If true, boost starts immediately on play. If false, wait for Activate().")]
    public bool activeOnStart = true;

    private void Start()
    {
        enabled = activeOnStart;
    }

    public void Activate()   => enabled = true;
    public void Deactivate() => enabled = false;

    private void Update()
    {
        Vector3 p = transform.localPosition;
        p.z -= extraSpeed * Time.deltaTime;
        transform.localPosition = p;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = enabled || activeOnStart ? Color.yellow : new Color(1f, 1f, 0f, 0.25f);
        Vector3 tip = transform.position + Vector3.forward * -2f;
        Gizmos.DrawLine(transform.position, tip);
        Gizmos.DrawWireSphere(tip, 0.3f);
    }
#endif
}
