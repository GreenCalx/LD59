using UnityEngine;

// Attach to HeroBundle root. Prevents Unity from frustum- or distance-culling
// any renderer in the subtree:
//   - SkinnedMeshRenderers: updateWhenOffscreen keeps bounds live every frame
//   - All Renderers: localBounds set to a huge volume so the frustum test always passes
[DefaultExecutionOrder(-200)]
public class NeverCull : MonoBehaviour
{
    private static readonly Bounds HugeBounds =
        new Bounds(Vector3.zero, Vector3.one * 100000f);

    private void Awake()
    {
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.updateWhenOffscreen = true;

        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.localBounds = HugeBounds;
    }
}
