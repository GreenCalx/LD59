using UnityEngine;

public class GameServices : MonoBehaviour
{
    public static GameServices Instance { get; private set; }

    [Header("Scene Services")]
    [SerializeField] public WaveGenerator  waveGenerator;
    [SerializeField] public LevelScope     levelScope;
    [SerializeField] public ProgressDriver progressDriver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
