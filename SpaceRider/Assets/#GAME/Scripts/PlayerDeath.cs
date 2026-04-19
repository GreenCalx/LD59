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

    [Header("References")]
    [SerializeField] private WaveGenerator waveGenerator;
    [SerializeField] private Surfer        surfer;

    [Header("Death Explosion")]
    [SerializeField] private Transform[] explosionTargets;
    [SerializeField] private float       explosionForce  = 600f;
    [SerializeField] private float       explosionRadius = 6f;
    [SerializeField] private float       explosionUpward = 1.5f;

    private bool     _dead;
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();

        var rb         = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        if (GetComponent<Collider>() == null)
        {
            var c       = gameObject.AddComponent<CapsuleCollider>();
            c.isTrigger = true;
            c.height    = 1.6f;
            c.radius    = 0.4f;
        }

        if (deathOverlay != null)
            deathOverlay.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_dead) return;
        if (other.GetComponentInParent<Asteroid>() != null)
            Die();
    }

    public void Die()
    {
        if (_dead) return;
        _dead = true;

        if (surfer        != null) surfer.enabled = false;
        if (waveGenerator != null) waveGenerator.Stop();
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
            while (elapsed < fadeDuration)
            {
                elapsed           += Time.unscaledDeltaTime;
                deathOverlay.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            deathOverlay.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(holdDuration);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Explode(Vector3 center)
    {
        foreach (var t in explosionTargets)
        {
            if (t == null) continue;
            t.SetParent(null, worldPositionStays: true);
            var rb = t.gameObject.AddComponent<Rigidbody>();
            rb.AddExplosionForce(explosionForce, center, explosionRadius, explosionUpward, ForceMode.Impulse);
        }
    }

    private float GetDeathAnimLength()
    {
        if (_animator?.runtimeAnimatorController == null) return 0f;
        foreach (var clip in _animator.runtimeAnimatorController.animationClips)
            if (clip.name == "Armature|Death")
                return clip.length;
        return 2.46f;
    }
}
