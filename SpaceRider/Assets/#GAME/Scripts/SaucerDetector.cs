using UnityEngine;

/// <summary>
/// Placed on the detection-sphere child of the FlyingSaucer prefab.
/// Its own Rigidbody isolates trigger events so they don't bleed up to
/// the root FlyingSaucer.OnTriggerEnter (which handles body collisions only).
///
/// FlyingSaucer.Awake auto-wires Saucer and syncs the SphereCollider radius
/// to detectionRange — no manual setup needed beyond adding this component
/// and a SphereCollider (isTrigger) on the same child GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class SaucerDetector : MonoBehaviour
{
    [HideInInspector] public FlyingSaucer Saucer;

    private void Awake()
    {
        var rb         = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        GetComponent<SphereCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var surfer = other.GetComponentInParent<Surfer>();
        if (surfer != null) Saucer?.OnHeroEntered(surfer.transform);
    }
}
