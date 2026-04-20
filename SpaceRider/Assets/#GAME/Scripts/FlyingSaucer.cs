using UnityEngine;
using FMODUnity;
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
    [Min(0f)] public float positionSmoothTime = 0.1f;
    [Tooltip("How strongly the orbit is pulled toward the hero's forward direction. 0 = free orbit, 1 = strongly front-biased.")]
    [Range(0f, 1f)] public float frontBias    = 0.6f;
    [Tooltip("Speed at which the orbit angle is attracted toward the front-bias target (deg/sec).")]
    [Min(0f)] public float frontBiasSpeed     = 160f;

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

    [Header("Death")]
    [Tooltip("Optional prefab instantiated at the saucer's position on death (VFX explosion). Detached so it survives the GO being destroyed.")]
    public GameObject explosionPrefab;
    [Tooltip("Fired just before the saucer is destroyed. Hook up FMOD here.")]
    public UnityEvent OnDeath;

    // ── runtime state ────────────────────────────────────────────────────────
    private bool      _harassing;
    private bool      _dead;
    private float     _orbitAngle;
    private Vector3   _velPos;
    private float     _baseLocalY;
    private Transform _hero;
    private Vector3   _lastParentWorldPos;

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
        _baseLocalY         = transform.localPosition.y;
        _orbitAngle         = Random.Range(0f, 360f);
        _lastParentWorldPos = transform.parent != null ? transform.parent.position : Vector3.zero;
    }

    private void Update()
    {
        if (_dead) return;

        // ── state: leave harassment if hero out of range ──────────────────
        if (_hero != null)
        {
            if (_harassing && Vector3.Distance(transform.position, _hero.position) > leaveRange)
                _harassing = false;
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
        _hero       = heroTransform;
        _harassing  = true;
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

        // Cancel treadmill scroll: measure how far the parent moved this frame
        // in world space and push the saucer back by the same amount so it
        // appears to float freely while still being a child of the treadmill GO.
        if (transform.parent != null)
        {
            Vector3 parentDelta  = transform.parent.position - _lastParentWorldPos;
            transform.position  -= parentDelta;
        }
        _lastParentWorldPos = transform.parent != null ? transform.parent.position : Vector3.zero;

        _orbitAngle += orbitSpeed * Time.deltaTime;

        // Bias the orbit angle toward the hero's forward direction.
        // We compute the "ideal front angle" (angle of hero.forward projected on XZ),
        // then nudge _orbitAngle toward it each frame — wonky because orbitSpeed also
        // keeps spinning, so they fight each other pleasantly.
        if (frontBias > 0f)
        {
            Vector3 heroFwd      = _hero.forward;
            heroFwd.y            = 0f;
            if (heroFwd.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(heroFwd.x, heroFwd.z) * Mathf.Rad2Deg;
                float delta       = Mathf.DeltaAngle(_orbitAngle, targetAngle);
                _orbitAngle      += delta * frontBias * frontBiasSpeed * Time.deltaTime / 90f;
            }
        }

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

        // Keep parent pos in sync so UpdateOrbit has a clean baseline on transition
        _lastParentWorldPos = transform.parent != null ? transform.parent.position : Vector3.zero;
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

        if (explosionPrefab != null)
        {
            var vfx = Instantiate(explosionPrefab, transform.position, transform.rotation);
            vfx.transform.SetParent(null);   // detach so it survives this GO being destroyed
        }

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
