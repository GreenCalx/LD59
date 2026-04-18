using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "MAUVE/Game Config")]
public class GameConfig : ScriptableObject
{
    public WaveInputConfig     waveInput;
    public WaveGeneratorConfig waveGenerator;
    public LevelConfig         level;
    public ProgressConfig      progress;
    public CameraConfig        camera;
    public SurferConfig        surfer;
    public RibbonConfig        ribbon;
}
