using UnityEngine;

public class FloatingToastSpawner : MonoBehaviour
{
    [SerializeField] FloatingToast toastPrefab;
    [SerializeField] Canvas        targetCanvas;
    [SerializeField] Color         passColor    = Color.white;
    [SerializeField] Color         perfectColor = new Color(1f, 0.85f, 0.1f, 1f);

    Transform     _player;
    RectTransform _canvasRT;

    void Start()
    {
        _player   = FindFirstObjectByType<Surfer>()?.transform;
        _canvasRT = targetCanvas != null ? (RectTransform)targetCanvas.transform : null;

        if (HoopTracker.Instance != null)
        {
            HoopTracker.Instance.OnPassRegistered += SpawnPass;
            HoopTracker.Instance.OnChainComplete  += SpawnChainComplete;
        }
    }

    void OnDestroy()
    {
        if (HoopTracker.Instance != null)
        {
            HoopTracker.Instance.OnPassRegistered -= SpawnPass;
            HoopTracker.Instance.OnChainComplete  -= SpawnChainComplete;
        }
    }

    void SpawnPass()                         => Spawn("+1",       passColor,    Vector2.zero);
    void SpawnChainComplete(bool perfect)    { if (perfect) Spawn("Perfect!", perfectColor, new Vector2(0f, 35f)); }

    void Spawn(string text, Color color, Vector2 offset)
    {
        if (toastPrefab == null || _player == null || targetCanvas == null) return;

        Vector3 worldPos  = _player.position + Vector3.up * 1.5f;
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT, screenPos, null, out Vector2 localPos);

        var toast = Instantiate(toastPrefab, _canvasRT);
        ((RectTransform)toast.transform).anchoredPosition = localPos + offset;
        toast.Play(text, color);
    }
}
