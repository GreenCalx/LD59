using System.Collections;
using UnityEngine;

public class HoopVisual : MonoBehaviour
{
    [SerializeField, Min(0.01f)] float dissolveDuration = 0.35f;

    static readonly int HighlightTID = Shader.PropertyToID("_HighlightT");
    static readonly int DissolveTID  = Shader.PropertyToID("_DissolveT");

    MeshRenderer          _renderer;
    MaterialPropertyBlock _block;
    bool                  _isDissolving;

    void Awake()
    {
        // Use GetComponentInChildren in case the mesh sits on a child object.
        _renderer = GetComponentInChildren<MeshRenderer>();
        _block    = new MaterialPropertyBlock();
        if (_renderer != null)
            ApplyBlock();
    }

    /// <summary>
    /// Toggle the next-in-chain highlight. Called by HoopChain.
    /// </summary>
    public void SetHighlight(bool on)
    {
        if (_renderer == null) return;
        _block.SetFloat(HighlightTID, on ? 1f : 0f);
        _renderer.SetPropertyBlock(_block);
    }

    /// <summary>
    /// Begin the dissolve animation. Safe to call multiple times (no-ops after first).
    /// After dissolveDuration seconds, the hoop GameObject is destroyed.
    /// </summary>
    public void TriggerDissolve()
    {
        if (_isDissolving) return;
        _isDissolving = true;
        _block.SetFloat(HighlightTID, 0f);
        if (_renderer != null) _renderer.SetPropertyBlock(_block);
        StartCoroutine(DissolveRoutine());
    }

    IEnumerator DissolveRoutine()
    {
        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            if (_renderer == null) yield break;
            elapsed += Time.deltaTime;
            _block.SetFloat(DissolveTID, Mathf.Clamp01(elapsed / dissolveDuration));
            _renderer.SetPropertyBlock(_block);
            yield return null;
        }
        _block.SetFloat(DissolveTID, 1f);
        _renderer.SetPropertyBlock(_block);
        Destroy(gameObject);
    }

    // Initialises the property block so the hoop starts in the normal state.
    void ApplyBlock()
    {
        _block.SetFloat(HighlightTID, 0f);
        _block.SetFloat(DissolveTID,  0f);
        _renderer.SetPropertyBlock(_block);
    }
}
