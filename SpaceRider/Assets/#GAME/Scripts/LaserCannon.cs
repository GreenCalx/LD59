using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Laser cannon that fires at the player's area when in range.
///
/// Prefab setup:
///   Root — LaserCannon + SphereCollider (trigger, detection radius)
///   └── (optional) child GO "Muzzle" — sets the beam origin point
///
/// Assign a Material using MAUVE/LaserBeam to laserMaterial.
/// The LineRenderer is created automatically if not already on the GameObject.
/// </summary>
public class LaserCannon : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Sphere trigger radius — player entering starts the charge cycle.")]
    [Min(0f)] public float detectionRange = 40f;

    [Header("Laser")]
    [Tooltip("Width of the LineRenderer beam in world units.")]
    [Min(0f)] public float laserWidth  = 0.5f;
    [Tooltip("Max radial offset from the player's centre — 0 = dead-on, higher = more spread.")]
    [Min(0f)] public float laserSpread = 4f;
    [Tooltip("How far the beam extends when it doesn't hit anything.")]
    [Min(0f)] public float laserLength = 80f;
    [Tooltip("Radius of the sphere cast used for hit detection.")]
    [Min(0f)] public float hitRadius   = 0.6f;

    [Header("Timing")]
    [Tooltip("Seconds of charge-up before the beam fires. A dim tracking beam is shown.")]
    [Min(0f)] public float chargeTime   = 1.5f;
    [Tooltip("Seconds the beam stays active and deals damage.")]
    [Min(0f)] public float fireTime     = 2f;
    [Tooltip("Seconds of cooldown between bursts.")]
    [Min(0f)] public float cooldownTime = 3f;

    [Header("Visuals")]
    [Tooltip("Material using MAUVE/LaserBeam shader. Required.")]
    public Material laserMaterial;
    [Tooltip("Multiplier applied to the material's Intensity during charge-up (dim effect).")]
    [Range(0f, 1f)] public float chargeIntensityScale = 0.2f;
    [Tooltip("Optional transform used as beam origin. Defaults to this transform.")]
    public Transform muzzle;

    [Header("Events")]
    public UnityEvent OnChargeStart;
    public UnityEvent OnFire;
    public UnityEvent OnCooldown;

    // ── state ────────────────────────────────────────────────────────────────
    private enum Phase { Idle, Charging, Firing, Cooldown }
    private Phase      _phase      = Phase.Idle;
    private float      _phaseTimer;
    private Transform  _hero;
    private PlayerDeath _heroDeathComp;
    private Vector3    _aimTarget;   // locked target for current fire burst
    private bool       _playerDead;

    private LineRenderer _line;
    private MaterialPropertyBlock _mpb;
    private float _baseIntensity;

    // ── lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-create LineRenderer if not already on the GO
        _line = GetComponent<LineRenderer>();
        if (_line == null) _line = gameObject.AddComponent<LineRenderer>();

        _line.positionCount     = 2;
        _line.useWorldSpace     = true;
        _line.textureMode       = LineTextureMode.Stretch;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows    = false;
        _line.enabled           = false;

        if (laserMaterial != null)
        {
            _line.material = laserMaterial;
            _baseIntensity = laserMaterial.GetFloat("_Intensity");
        }

        _mpb = new MaterialPropertyBlock();

        // Auto-size the detection collider
        var sc = GetComponent<SphereCollider>();
        if (sc != null) { sc.isTrigger = true; sc.radius = detectionRange; }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hero != null || _phase != Phase.Idle) return;
        var death = other.GetComponentInParent<PlayerDeath>();
        if (death == null) return;
        _hero          = death.transform;
        _heroDeathComp = death;
        _playerDead    = false;
        EnterPhase(Phase.Charging);
    }

    private void Update()
    {
        if (_playerDead || _hero == null) return;

        _phaseTimer -= Time.deltaTime;

        switch (_phase)
        {
            case Phase.Charging: UpdateCharging(); break;
            case Phase.Firing:   UpdateFiring();   break;
            case Phase.Cooldown: UpdateCooldown(); break;
        }
    }

    // ── phase logic ──────────────────────────────────────────────────────────

    private void UpdateCharging()
    {
        // Show a dim tracking beam that drifts toward the player area
        Vector3 chargeTarget = GetPlayerAreaTarget();
        float   hitDist      = CastBeam(chargeTarget);
        ShowBeam(chargeTarget, hitDist, chargeIntensityScale);

        if (_phaseTimer <= 0f)
        {
            // Lock aim at player position + random offset within spread disc
            _aimTarget = GetPlayerAreaTarget();
            EnterPhase(Phase.Firing);
        }
    }

    private void UpdateFiring()
    {
        float hitDist = CastBeam(_aimTarget);
        ShowBeam(_aimTarget, hitDist, 1f);
        DamageCheck(hitDist);

        if (_phaseTimer <= 0f)
            EnterPhase(Phase.Cooldown);
    }

    private void UpdateCooldown()
    {
        _line.enabled = false;

        if (_phaseTimer <= 0f)
            EnterPhase(Phase.Charging);   // fire again while player is still in range
    }

    private void EnterPhase(Phase next)
    {
        _phase      = next;
        switch (next)
        {
            case Phase.Charging:
                _phaseTimer = chargeTime;
                _line.enabled = true;
                OnChargeStart.Invoke();
                break;
            case Phase.Firing:
                _phaseTimer = fireTime;
                OnFire.Invoke();
                break;
            case Phase.Cooldown:
                _phaseTimer = cooldownTime;
                _line.enabled = false;
                OnCooldown.Invoke();
                break;
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    // Returns a point near the player's position offset by a random amount within laserSpread.
    private Vector3 GetPlayerAreaTarget()
    {
        if (_hero == null) return Origin + transform.forward * laserLength;

        Vector3 toHero    = (_hero.position - Origin).normalized;
        Vector3 right     = Vector3.Cross(toHero, Vector3.up).normalized;
        Vector3 up        = Vector3.Cross(right, toHero).normalized;

        Vector2 disc      = Random.insideUnitCircle * laserSpread;
        return _hero.position + right * disc.x + up * disc.y;
    }

    // Returns the distance to the first collider along the beam (laserLength if nothing hit).
    private float CastBeam(Vector3 target)
    {
        Vector3 origin    = Origin;
        Vector3 direction = (target - origin).normalized;
        if (Physics.SphereCast(origin, hitRadius, direction, out RaycastHit hit, laserLength))
            return hit.distance;
        return laserLength;
    }

    private void ShowBeam(Vector3 target, float hitDistance, float intensityScale)
    {
        Vector3 origin    = Origin;
        Vector3 direction = (target - origin).normalized;
        Vector3 endpoint  = origin + direction * hitDistance;

        _line.SetPosition(0, origin);
        _line.SetPosition(1, endpoint);
        _line.startWidth = laserWidth;
        _line.endWidth   = laserWidth;
        _line.enabled    = true;

        // Adjust intensity via property block so the shared material isn't modified
        _line.GetPropertyBlock(_mpb);
        _mpb.SetFloat("_Intensity", _baseIntensity * intensityScale);
        _line.SetPropertyBlock(_mpb);
    }

    private void DamageCheck(float hitDistance)
    {
        if (_heroDeathComp == null || _playerDead) return;

        Vector3 origin    = Origin;
        Vector3 direction = (_aimTarget - origin).normalized;

        if (Physics.SphereCast(origin, hitRadius, direction, out RaycastHit hit, hitDistance + 0.1f))
        {
            var death = hit.collider.GetComponentInParent<PlayerDeath>();
            if (death != null)
            {
                _playerDead   = true;
                _line.enabled = false;
                death.Die();
            }
        }
    }

    private Vector3 Origin => muzzle != null ? muzzle.position : transform.position;

    // ── gizmos ───────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (_hero != null)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawLine(Origin, _aimTarget);
            Gizmos.DrawWireSphere(_hero.position, laserSpread);
        }
    }
#endif
}
