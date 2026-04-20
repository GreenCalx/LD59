using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Flying saucer that idles with a gentle bob, then orbits and harasses the hero
/// when the hero enters the detection sphere trigger.
///
/// Prefab setup:
///   Root           — FlyingSaucer + Rigidbody (kinematic) + small SphereCollider (trigger, body hit)
///   └── child GO   — SaucerDetector + SphereCollider (trigger, detection radius)
///                    (SaucerDetector [RequireComponent] auto-adds its own Rigidbody so events
///                     are isolated from the root and don't cross-fire.)
///
/// FlyingSaucer.Awake auto-wires the SaucerDetector reference and syncs its sphere radius.
/// </summary>
[DefaultExecutionOrder(50)]   // after Surfer (-50)
[RequireComponent(typeof(Rigidbody))]
public class FlyingSaucer : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Radius of the detection sphere — auto-applied to the SaucerDetector child collider.")]
    [Min(0f)] public float detectionRange = 25f;
    [Tooltip("Distance at which the saucer gives up chasing (hysteresis).")]
    [Min(0f)] public float leaveRange     = 35f;

    [Header("Orbit")]
    [Min(0f)] public float orbitRadius        = 8f;
    [Min(0f)] public float orbitSpeed         = 60f;   // degrees / second
    [Min(0f)] public float positionSmoothTime = 0.4f;

    [Header("Wiggle")]
    public float wiggleAmplitude  = 1.5f;
    [Min(0f)] public float wiggleFrequency   = 1.2f;
    [Tooltip("Time-phase offset — stagger multiple saucers so they don't bob in sync.")]
    public float wigglePhaseOffset = 0f;

    [Header("Idle Bob")]
    public float bobAmplitude  = 0.4f;
    [Min(0f)] public float bobFrequency  = 0.6f;

    [Header("Self-Spin")]
    public float yawSpeed  = 45f;
    public float rollSway  = 12f;  // degrees of peak roll when idling

    [Header("Events")]
    [Tooltip("Fired just before the saucer is destroyed. Hook up VFX / FMOD here.")]
    public UnityEvent OnDeath;

    // ── runtime state ────────────────────────────────────────────────────────
    private bool      _harassing;
    private bool      _dead;
    private float     _orbitAngle;
    private Vector3   _velPos;
    private float     _baseLocalY;
    private Transform _hero;

    // ── lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        var rb         = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // Auto-wire detection child and sync its sphere radius
        var detector = GetComponentInChildren<SaucerDetector>(true);
        if (detector != null)
        {
            detector.Saucer = this;
            var sc = detector.GetComponent<SphereCollider>();
            if (sc != null) sc.radius = detectionRange;
        }
    }

    private void Start()
    {
        _baseLocalY = transform.localPosition.y;
        _orbitAngle = Random.Range(0f, 360f);   // randomise start so multiple saucers don't align
    }

    private void Update()
    {
        if (_dead) return;

        // ── state: enter / leave harassment ───────────────────────────────
        if (_hero != null)
        {
            float dist = Vector3.Distance(transform.position, _hero.position);
            if (!_harassing && dist < detectionRange) _harassing = true;
            if ( _harassing && dist > leaveRange)     _harassing = false;
        }
        else
        {
            _harassing = false;
        }

        // ── position ──────────────────────────────────────────────────────
        if (_harassing) UpdateOrbit();
        else            UpdateIdleBob();

        // ── rotation ──────────────────────────────────────────────────────
        UpdateRotation();
    }

    // ── called by SaucerDetector when the hero enters the detection sphere ──

    public void OnHeroEntered(Transform heroTransform)
    {
        if (_dead || _hero != null) return;
        _hero = heroTransform;
    }

    // ── body hit: die if an obstacle (HeroDamager) enters the body collider ─

    private void OnTriggerEnter(Collider other)
    {
        if (_dead) return;
        if (other.GetComponentInParent<HeroDamager>() != null) Die();
    }

    // ── position helpers ─────────────────────────────────────────────────────

    private void UpdateOrbit()
    {
        if (_hero == null) return;

        _orbitAngle += orbitSpeed * Time.deltaTime;

        float   wiggle    = Mathf.Sin((Time.time + wigglePhaseOffset) * wiggleFrequency) * wiggleAmplitude;
        Vector3 radialDir = Quaternion.AngleAxis(_orbitAngle, Vector3.up) * Vector3.right;
        Vector3 target    = _hero.position + radialDir * orbitRadius + Vector3.up * wiggle;

        transform.position = Vector3.SmoothDamp(
            transform.position, target, ref _velPos, positionSmoothTime);
    }

    private void UpdateIdleBob()
    {
        // Only touch Y so a parent treadmill can keep moving Z freely
        Vector3 lp = transform.localPosition;
        lp.y = _baseLocalY + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.localPosition = lp;
    }

    // ── rotation helper ──────────────────────────────────────────────────────

    private void UpdateRotation()
    {
        if (_harassing && _hero != null)
        {
            // Slowly face the hero
            Vector3 toHero = _hero.position - transform.position;
            if (toHero.sqrMagnitude > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(toHero.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 4f);
            }
        }
        else
        {
            // Free-spin: continuous yaw + subtle roll sway
            transform.Rotate(Vector3.up, yawSpeed * Time.deltaTime, Space.World);
            Vector3 euler = transform.eulerAngles;
            euler.z = Mathf.Sin(Time.time * 0.9f) * rollSway;
            transform.eulerAngles = euler;
        }
    }

    // ── death ────────────────────────────────────────────────────────────────

    private void Die()
    {
        if (_dead) return;
        _dead = true;
        OnDeath.Invoke();
        Destroy(gameObject);
    }

    // ── editor gizmos ────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        bool active = Application.isPlaying && _harassing;

        Gizmos.color = active
            ? new Color(1f, 0.2f, 0.2f, 0.5f)
            : new Color(1f, 0.6f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (active || UnityEditor.Selection.Contains(gameObject))
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, leaveRange);
        }

        if (active && _hero != null)
        {
            Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(_hero.position, orbitRadius);
        }
    }
#endif
}
