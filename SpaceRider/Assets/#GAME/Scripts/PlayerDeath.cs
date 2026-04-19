using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlayerDeath : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup deathOverlay;
    [SerializeField] private float       fadeDuration = 0.4f;
    [SerializeField] private float       holdDuration = 0.5f;

    [Header("References (auto-resolved if left empty)")]
    [SerializeField] private WaveGenerator    waveGenerator;
    [SerializeField] private Surfer           surfer;
    [SerializeField] private ProgressDriver   progressDriver;
    [SerializeField] private RibbonVisualizer ribbonVisualizer;
    [SerializeField] private float            ribbonFadeDuration = 1f;

    [Header("Death Explosion")]
    [SerializeField] private Transform[] explosionTargets;
    [SerializeField] private float       explosionForce        = 1200f;
    [SerializeField] private float       explosionRadius       = 8f;
    [SerializeField] private float       explosionUpward       = 3f;
    [SerializeField] private float       explosionAngularForce = 20f;

    private bool     _dead;
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();

        // Auto-resolve refs from the HeroBundle root so no manual cross-prefab wiring is needed
        Transform root = transform.root;
        if (progressDriver   == null) progressDriver   = root.GetComponentInChildren<ProgressDriver>();
        if (waveGenerator    == null) waveGenerator    = root.GetComponentInChildren<WaveGenerator>();
        if (surfer           == null) surfer           = root.GetComponentInChildren<Surfer>();
        if (ribbonVisualizer == null) ribbonVisualizer = root.GetComponentInChildren<RibbonVisualizer>();

        var rb         = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        var col = GetComponent<Collider>();
        if (col == null)
        {
            var c    = gameObject.AddComponent<CapsuleCollider>();
            c.height = 1.6f;
            c.radius = 0.4f;
            col      = c;
        }
        col.isTrigger = true;

        if (deathOverlay != null)
            deathOverlay.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_dead) return;
        if (other.GetComponentInParent<HeroDamager>() != null)
            Die();
    }

    public void Die()
    {
        if (_dead) return;
        _dead = true;

        if (surfer           != null) surfer.enabled = false;
        if (waveGenerator    != null) waveGenerator.Stop();
        if (progressDriver   != null) progressDriver.Stop();
        if (ribbonVisualizer != null) ribbonVisualizer.FadeOut(ribbonFadeDuration);

        Explode(transform.position);

        if (_animator != null)
        {
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            _animator.SetTrigger("Die");
        }

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSecondsRealtime(GetDeathAnimLength());

        if (deathOverlay != null)
        {
            deathOverlay.gameObject.SetActive(true);
            deathOverlay.alpha = 0f;
            float elapsed = 0f;
            // Use WaitForSecondsRealtime steps so timeScale=0 never stalls the fade
            while (elapsed < fadeDuration)
            {
                yield return new WaitForSecondsRealtime(0.016f);
                elapsed           += 0.016f;
                deathOverlay.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            }
            deathOverlay.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(holdDuration);

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Explode(Vector3 center)
    {
        foreach (var t in explosionTargets)
        {
            if (t == null) continue;

            // Fully detach from hierarchy so nothing can drive this transform anymore
            t.SetParent(null, worldPositionStays: true);

            // Destroy any MonoBehaviours that could override the transform (BoomerBeat, etc.)
            // Scene is reloaded on death so this is safe to be destructive
            foreach (var mb in t.GetComponents<MonoBehaviour>())
                Destroy(mb);

            var rb = t.gameObject.AddComponent<Rigidbody>();
            rb.AddExplosionForce(explosionForce, center, explosionRadius, explosionUpward, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * explosionAngularForce, ForceMode.Impulse);
        }
    }

    private float GetDeathAnimLength()
    {
        if (_animator?.runtimeAnimatorController != null)
            foreach (var clip in _animator.runtimeAnimatorController.animationClips)
                if (clip.name == "Armature|Death")
                    return clip.length;
        return 2.46f; // fallback keeps timing consistent even without an animator
    }
}
