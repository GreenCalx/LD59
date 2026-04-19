using UnityEngine;

public class HoopDetector : MonoBehaviour
{
    [SerializeField] private float innerRadius = 2f;

    private Transform       _hero;
    private CapsuleCollider _heroCapsule;
    private LevelScope      _levelScope;
    private float           _crossingDist;
    private float           _prevVD;
    private bool            _evaluated;
    private bool            _initialized;

    void Awake()
    {
        var all = GetComponents<HoopDetector>();
        for (int i = 1; i < all.Length; i++) Destroy(all[i]);
    }

    void Start()
    {
        var surfer   = FindObjectOfType<Surfer>();
        _hero        = surfer?.transform;
        _heroCapsule = surfer?.GetComponentInChildren<CapsuleCollider>();
        _levelScope  = FindObjectOfType<LevelScope>();
        if (_levelScope == null) { enabled = false; }
    }

    void Update()
    {
        if (_evaluated) return;

        // Compute crossingDist on the first Update so ProgressDriver (order -100)
        // has already synced World.position.z = -VirtualDistance this frame.
        if (!_initialized)
        {
            _initialized  = true;
            _crossingDist = transform.position.z + _levelScope.VirtualDistance;
            _prevVD       = _levelScope.VirtualDistance;
            if (_crossingDist <= _prevVD) { enabled = false; return; }
        }

        float vd = _levelScope.VirtualDistance;

        if (_prevVD < _crossingDist && vd >= _crossingDist)
        {
            _evaluated = true;
            enabled    = false;

            Vector3 heroCenter = _heroCapsule != null
                ? _hero.TransformPoint(_heroCapsule.center)
                : _hero.position;

            float dx     = heroCenter.x - transform.position.x;
            float dy     = heroCenter.y - transform.position.y;
            bool  passed = dx * dx + dy * dy <= innerRadius * innerRadius;

            if (passed) HoopTracker.Instance?.RegisterPass();
            else        HoopTracker.Instance?.RegisterMiss();
            return;
        }

        _prevVD = vd;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = _evaluated ? Color.gray : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, innerRadius);
    }
}
