using UnityEngine;
using FMODUnity;
using UnityEngine.Events;

/// <summary>
/// Flying saucer with two active modes set in the Inspector:
///   Orbiter  — steady circular orbit around the hero, no aggression.
///   Harasser — front-biased orbit with wiggle, tries to stay in the hero's face.
///
/// Prefab setup:
///   Root           — FlyingSaucer + Rigidbody (kinematic) + small SphereCollider (trigger, body hit)
///   └── child GO   — SaucerDetector + SphereCollider (trigger, detection radius)
/// </summary>
[DefaultExecutionOrder(50)]
[RequireComponent(typeof(Rigidbody))]
public class FlyingSaucer : MonoBehaviour
{
    public enum SaucerMode { Orbiter, Harasser }

    [Header("Mode")]
    [Tooltip("Orbiter: steady circular orbit. Harasser: front-biased with wiggle.")]
    public SaucerMode mode = SaucerMode.Harasser;

    [Header("Detection")]
    [Min(0f)] public float detectionRange = 25f;
    [Min(0f)] public float leaveRange     = 99999f;

    [Header("Orbit — Both Modes")]
    [Min(0f)] public float orbitRadius        = 8f;
    [Min(0f)] public float orbitSpeed         = 60f;
    [Min(0f)] public float positionSmoothTime = 0.1f;

    [Header("Orbit — Harasser Only")]
    [Range(0f, 1f)] public float frontBias    = 0.6f;
    [Min(0f)] public float frontBiasSpeed     = 160f;
    public float wiggleAmplitude              = 1.5f;
    [Min(0f)] public float wiggleFrequency    = 1.2f;
    [Tooltip("Phase offset to stagger multiple saucers.")]
    public float wigglePhaseOffset            = 0f;

    [Header("Idle Bob")]
    public float bobAmplitude  = 0.4f;
    [Min(0f)] public float bobFrequency  = 0.6f;

    [Header("Self-Spin")]
    public float yawSpeed  = 45f;
    public float rollSway  = 12f;

    [Header("Death")]
    public GameObject explosionPrefab;
    public UnityEvent OnDeath;

    // ── runtime state ────────────────────────────────────────────────────────
    private bool      _active;   // hero detected and in range
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

        if (_hero != null && _active)
        {
            if (Vector3.Distance(transform.position, _hero.position) > leaveRange)
                _active = false;
        }
        else if (_hero == null)
        {
            _active = false;
        }

        if (_active) UpdateActiveMode();
        else         UpdateIdleBob();

        UpdateRotation();
    }

    public void OnHeroEntered(Transform heroTransform)
    {
        if (_dead || _hero != null) return;
        _hero   = heroTransform;
        _active = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_dead) return;
        if (other.GetComponentInParent<HeroDamager>() != null) Die();
    }

    // ── mode dispatch ────────────────────────────────────────────────────────

    private void UpdateActiveMode()
    {
        switch (mode)
        {
            case SaucerMode.Orbiter:   UpdateOrbiter();   break;
            case SaucerMode.Harasser:  UpdateHarasser();  break;
        }
    }

    // ── Orbiter: clean steady circle ─────────────────────────────────────────

    private void UpdateOrbiter()
    {
        if (_hero == null) return;
        CancelTreadmill();

        _orbitAngle += orbitSpeed * Time.deltaTime;

        Vector3 radialDir = Quaternion.AngleAxis(_orbitAngle, Vector3.up) * Vector3.right;
        Vector3 target    = _hero.position + radialDir * orbitRadius;

        transform.position = Vector3.SmoothDamp(
            transform.position, target, ref _velPos, positionSmoothTime);
    }

    // ── Harasser: front-biased orbit with wiggle ──────────────────────────────

    private void UpdateHarasser()
    {
        if (_hero == null) return;
        CancelTreadmill();

        _orbitAngle += orbitSpeed * Time.deltaTime;

        if (frontBias > 0f)
        {
            Vector3 heroFwd = _hero.forward;
            heroFwd.y = 0f;
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

    // ── helpers ──────────────────────────────────────────────────────────────

    private void CancelTreadmill()
    {
        if (transform.parent != null)
        {
            Vector3 parentDelta = transform.parent.position - _lastParentWorldPos;
            transform.position -= parentDelta;
        }
        _lastParentWorldPos = transform.parent != null ? transform.parent.position : Vector3.zero;
    }

    private void UpdateIdleBob()
    {
        Vector3 lp = transform.localPosition;
        lp.y = _baseLocalY + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.localPosition = lp;

        _lastParentWorldPos = transform.parent != null ? transform.parent.position : Vector3.zero;
    }

    private void UpdateRotation()
    {
        if (_active && _hero != null)
        {
            Vector3 toHero = _hero.position - transform.position;
            if (toHero.sqrMagnitude > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(toHero.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 4f);
            }
        }
        else
        {
            transform.Rotate(Vector3.up, yawSpeed * Time.deltaTime, Space.World);
            Vector3 euler = transform.eulerAngles;
            euler.z = Mathf.Sin(Time.time * 0.9f) * rollSway;
            transform.eulerAngles = euler;
        }
    }

    public void Die()
    {
        if (_dead) return;
        _dead = true;

        if (explosionPrefab != null)
        {
            var vfx = Instantiate(explosionPrefab, transform.position, transform.rotation);
            vfx.transform.SetParent(null);
        }

        OnDeath.Invoke();
        Destroy(gameObject);
    }

    // ── gizmos ───────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        bool active = Application.isPlaying && _active;

        Gizmos.color = active
            ? new Color(1f, 0.2f, 0.2f, 0.5f)
            : new Color(1f, 0.6f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (active && _hero != null)
        {
            Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(_hero.position, orbitRadius);
        }
    }
#endif
}
