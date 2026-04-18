using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlayerDeath : MonoBehaviour
{
    [SerializeField] private CanvasGroup deathOverlay;
    [SerializeField] private float       fadeDuration = 0.4f;
    [SerializeField] private float       holdDuration = 2f;

    private bool _dead;

    private void Awake()
    {
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
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        Time.timeScale = 0f;

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

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
