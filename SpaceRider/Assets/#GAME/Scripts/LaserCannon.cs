using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Laser cannon that fires at the player's area when in range.
///
/// Prefab setup:
///   Root — LaserCannon + SphereCollider (trigger, detection radius)
///          + Rigidbody (kinematic) for trigger events
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
    [Min(0f)] public float laserWidth       = 0.5f;
    [Tooltip("World units per second the beam tip travels from muzzle outward on fire.")]
    [Min(0f)] public float beamExtendSpeed  = 2f;
    [Tooltip("Maximum beam length / how far the beam extends when nothing is hit.")]
    [Min(0f)] public float laserLength      = 80f;
    [Tooltip("Radius of the sphere cast used for hit detection.")]
    [Min(0f)] public float hitRadius        = 0.6f;

    [Header("Spread — Static")]
    [Tooltip("Aim offset locked once at fire start, held for the full burst. Set to 0 to disable.")]
    [Min(0f)] public float staticSpread = 3f;

    [Header("Spread — Dynamic")]
    [Tooltip("Aim offset randomised every frame while firing. Mutually exclusive with Static — set one to 0. Default 0.")]
    [Min(0f)] public float dynamicSpread = 0f;

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
    private Phase       _phase = Phase.Idle;
    private float       _phaseTimer;
    private Transform   _hero;
    private PlayerDeath _heroDeathComp;
    private Vector3     _aimTarget;       // direction target locked at fire start
    private Vector3     _staticAimOffset; // world-space offset locked at fire start
    private float       _beamCurrentLength;
    private bool        _playerDead;

    private LineRenderer         _line;
    private MaterialPropertyBlock _mpb;
    private float                 _baseIntensity;

    // ── lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
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
        // Dim tracking beam follows player area during charge
        Vector3 chargeTarget = GetSpreadTarget(dynamicSpread > 0f ? dynamicSpread : staticSpread);
        float   hitDist      = CastBeam(chargeTarget, laserLength);
        ShowBeam(chargeTarget, hitDist, chargeIntensityScale);

        if (_phaseTimer <= 0f)
            EnterPhase(Phase.Firing);
    }

    private void UpdateFiring()
    {
        // Extend beam tip outward at beamExtendSpeed
        _beamCurrentLength = Mathf.MoveTowards(_beamCurrentLength, laserLength,
                                                beamExtendSpeed * Time.deltaTime);

        // Static spread: reuse the locked offset. Dynamic: new random each frame.
        Vector3 target = staticSpread > 0f
            ? (_hero != null ? _hero.position + _staticAimOffset : _aimTarget)
            : GetSpreadTarget(dynamicSpread);

        float hitDist = CastBeam(target, _beamCurrentLength);
        ShowBeam(target, hitDist, 1f);
        DamageCheck(target, hitDist);

        if (_phaseTimer <= 0f)
            EnterPhase(Phase.Cooldown);
    }

    private void UpdateCooldown()
    {
        _line.enabled = false;
        if (_phaseTimer <= 0f)
            EnterPhase(Phase.Charging);
    }

    private void EnterPhase(Phase next)
    {
        _phase = next;
        switch (next)
        {
            case Phase.Charging:
                _phaseTimer   = chargeTime;
                _line.enabled = true;
                OnChargeStart.Invoke();
                break;

            case Phase.Firing:
                _phaseTimer        = fireTime;
                _beamCurrentLength = 0f;
                // Lock static offset once here; dynamic recalculates every frame
                if (staticSpread > 0f)
                {
                    _staticAimOffset = RandomDiscOffset(staticSpread);
                    _aimTarget       = _hero != null
                        ? _hero.position + _staticAimOffset
                        : Origin + transform.forward * laserLength;
                }
                else
                {
                    _aimTarget = GetSpreadTarget(dynamicSpread);
                }
                OnFire.Invoke();
                break;

            case Phase.Cooldown:
                _phaseTimer   = cooldownTime;
                _line.enabled = false;
                OnCooldown.Invoke();
                break;
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private Vector3 GetSpreadTarget(float spread)
    {
        if (_hero == null) return Origin + transform.forward * laserLength;
        return _hero.position + RandomDiscOffset(spread);
    }

    private Vector3 RandomDiscOffset(float radius)
    {
        if (radius <= 0f || _hero == null) return Vector3.zero;
        Vector3 toHero = (_hero.position - Origin).normalized;
        Vector3 right  = Vector3.Cross(toHero, Vector3.up).normalized;
        Vector3 up     = Vector3.Cross(right, toHero).normalized;
        Vector2 disc   = Random.insideUnitCircle * radius;
        return right * disc.x + up * disc.y;
    }

    // Returns distance to the first NON-PLAYER collider along the beam (laserLength if nothing hit).
    // The player's body is a trigger so we use SphereCastAll and skip any hit that has PlayerDeath.
    private float CastBeam(Vector3 target, float maxDist)
    {
        Vector3 direction = (target - Origin).normalized;
        var hits = Physics.SphereCastAll(Origin, hitRadius, direction, maxDist,
                                         Physics.AllLayers, QueryTriggerInteraction.Collide);
        float closest = maxDist;
        foreach (var hit in hits)
        {
            if (hit.collider.GetComponentInParent<PlayerDeath>() != null) continue;
            if (hit.distance < closest) closest = hit.distance;
        }
        return closest;
    }

    private void ShowBeam(Vector3 target, float hitDistance, float intensityScale)
    {
        Vector3 direction = (target - Origin).normalized;
        _line.SetPosition(0, Origin);
        _line.SetPosition(1, Origin + direction * hitDistance);
        _line.startWidth = laserWidth;
        _line.endWidth   = laserWidth;
        _line.enabled    = true;

        _line.GetPropertyBlock(_mpb);
        _mpb.SetFloat("_Intensity", _baseIntensity * intensityScale);
        _line.SetPropertyBlock(_mpb);
    }

    // Damage check uses QueryTriggerInteraction.Collide so it detects the player's trigger collider.
    // Also destroys any FlyingSaucer in the beam path.
    private void DamageCheck(Vector3 target, float beamReach)
    {
        if (_playerDead) return;
        Vector3 direction = (target - Origin).normalized;
        var hits = Physics.SphereCastAll(Origin, hitRadius, direction, beamReach,
                                         Physics.AllLayers, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
        {
            // Player
            var death = hit.collider.GetComponentInParent<PlayerDeath>();
            if (death != null)
            {
                _playerDead   = true;
                _line.enabled = false;
                death.Die();
                return;
            }

            // Flying saucer
            var saucer = hit.collider.GetComponentInParent<FlyingSaucer>();
            if (saucer != null)
            {
                saucer.Die();
                return;
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
            float spread = staticSpread > 0f ? staticSpread : dynamicSpread;
            if (spread > 0f)
                Gizmos.DrawWireSphere(_hero.position, spread);
        }
    }
#endif
}
