using UnityEngine;
using UnityEditor;

public class MauveGameTuner : EditorWindow
{
    private const string PrefGizmos      = "MAUVE_ShowGizmos";
    private const string PrefFoldWave    = "MAUVE_FoldWaveInput";
    private const string PrefFoldGen     = "MAUVE_FoldWaveGen";
    private const string PrefFoldLevel   = "MAUVE_FoldLevel";
    private const string PrefFoldProg    = "MAUVE_FoldProgress";
    private const string PrefFoldCam     = "MAUVE_FoldCamera";
    private const string PrefFoldSurf    = "MAUVE_FoldSurfer";
    private const string PrefFoldRibbon  = "MAUVE_FoldRibbon";
    private const string PrefFoldBoundary = "MAUVE_FoldBoundary";

    private GameConfig _config;
    private Editor     _waveInputEditor, _waveGenEditor, _levelEditor;
    private Editor     _progressEditor,  _cameraEditor,  _surferEditor, _ribbonEditor;
    private Editor     _boundaryEditor;
    private Vector2    _scroll;
    private bool _foldWave, _foldGen, _foldLevel, _foldProg, _foldCam, _foldSurf, _foldRibbon, _foldBoundary;

    [MenuItem("MAUVE/Game Tuner")]
    public static void Open() => GetWindow<MauveGameTuner>("MAUVE Game Tuner");

    private void OnEnable()
    {
        GameDebug.ShowGizmos = EditorPrefs.GetBool(PrefGizmos, false);
        // cache foldout states so DrawSection doesn't hit EditorPrefs every repaint
        _foldWave  = EditorPrefs.GetBool(PrefFoldWave,  true);
        _foldGen   = EditorPrefs.GetBool(PrefFoldGen,   true);
        _foldLevel = EditorPrefs.GetBool(PrefFoldLevel, true);
        _foldProg  = EditorPrefs.GetBool(PrefFoldProg,  true);
        _foldCam   = EditorPrefs.GetBool(PrefFoldCam,   true);
        _foldSurf   = EditorPrefs.GetBool(PrefFoldSurf,   true);
        _foldRibbon = EditorPrefs.GetBool(PrefFoldRibbon, true);
        _foldBoundary = EditorPrefs.GetBool(PrefFoldBoundary, true);
    }

    private void OnDestroy() => ClearEditors();

    private void OnGUI()
    {
        DrawHeader();
        if (_config == null) { DrawCreatePrompt(); return; }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawSection("Wave Input",     PrefFoldWave,  ref _foldWave,  ref _waveInputEditor, _config.waveInput);
        DrawSection("Wave Generator", PrefFoldGen,   ref _foldGen,   ref _waveGenEditor,   _config.waveGenerator);
        DrawSection("Level",          PrefFoldLevel, ref _foldLevel, ref _levelEditor,     _config.level);
        DrawSection("Progress",       PrefFoldProg,  ref _foldProg,  ref _progressEditor,  _config.progress);
        DrawSection("Camera",         PrefFoldCam,   ref _foldCam,   ref _cameraEditor,    _config.camera);
        DrawSection("Surfer",         PrefFoldSurf,   ref _foldSurf,   ref _surferEditor,   _config.surfer);
        DrawSection("Ribbon",         PrefFoldRibbon,   ref _foldRibbon,   ref _ribbonEditor,    _config.ribbon);
        DrawSection("Boundary",       PrefFoldBoundary, ref _foldBoundary, ref _boundaryEditor,  _config.boundary);
        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        var newCfg = (GameConfig)EditorGUILayout.ObjectField("Game Config", _config, typeof(GameConfig), false);
        if (newCfg != _config) { _config = newCfg; ClearEditors(); }
        if (_config != null && GUILayout.Button("Ping", GUILayout.Width(40)))
            EditorGUIUtility.PingObject(_config);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        bool cur  = EditorPrefs.GetBool(PrefGizmos, false);
        bool next = GUILayout.Toggle(cur, cur ? "\u2b24 Debug Gizmos ON" : "\u25cb Debug Gizmos OFF", "Button");
        if (next != cur)
        {
            EditorPrefs.SetBool(PrefGizmos, next);
            GameDebug.ShowGizmos = next;
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(4);
        GUI.enabled = _config != null;
        if (GUILayout.Button("Save All Config Assets"))
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[MAUVE] Config assets saved.");
        }
        GUI.enabled = true;
        EditorGUILayout.Space(6);
    }

    private void DrawCreatePrompt()
    {
        EditorGUILayout.HelpBox("No GameConfig assigned. Click to create a default asset set.", MessageType.Info);
        if (GUILayout.Button("Create Default Config"))
            CreateDefaultConfig();
    }

    private void DrawSection(string label, string prefKey, ref bool fold, ref Editor editor, ScriptableObject target)
    {
        if (target == null)
        {
            EditorGUILayout.HelpBox($"{label} sub-config is null — assign it on GameConfig.", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        bool newFold = EditorGUILayout.Foldout(fold, label, true, EditorStyles.foldoutHeader);
        if (GUILayout.Button("Ping", GUILayout.Width(40))) EditorGUIUtility.PingObject(target);
        EditorGUILayout.EndHorizontal();
        if (newFold != fold) { fold = newFold; EditorPrefs.SetBool(prefKey, newFold); }

        if (newFold)
        {
            EditorGUI.indentLevel++;
            Editor.CreateCachedEditor(target, null, ref editor);
            editor.OnInspectorGUI();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(4);
    }

    private void ClearEditors()
    {
        DestroyImmediate(_waveInputEditor);  _waveInputEditor = null;
        DestroyImmediate(_waveGenEditor);    _waveGenEditor   = null;
        DestroyImmediate(_levelEditor);      _levelEditor     = null;
        DestroyImmediate(_progressEditor);   _progressEditor  = null;
        DestroyImmediate(_cameraEditor);     _cameraEditor    = null;
        DestroyImmediate(_surferEditor);     _surferEditor    = null;
        DestroyImmediate(_ribbonEditor);     _ribbonEditor    = null;
        DestroyImmediate(_boundaryEditor);   _boundaryEditor  = null;
    }

    private void CreateDefaultConfig()
    {
        const string parent = "Assets/#GAME/Config";
        const string folder = "Assets/#GAME/Config/Default";

        if (!AssetDatabase.IsValidFolder(parent))
            AssetDatabase.CreateFolder("Assets/#GAME", "Config");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(parent, "Default");

        var waveInput     = CreateOrLoad<WaveInputConfig>(folder,     "WaveInput_Default");
        var waveGenerator = CreateOrLoad<WaveGeneratorConfig>(folder, "WaveGenerator_Default");
        var level         = CreateOrLoad<LevelConfig>(folder,         "Level_Default");
        var progress      = CreateOrLoad<ProgressConfig>(folder,      "Progress_Default");
        var camera        = CreateOrLoad<CameraConfig>(folder,        "Camera_Default");
        var surfer        = CreateOrLoad<SurferConfig>(folder,        "Surfer_Default");
        var ribbon        = CreateOrLoad<RibbonConfig>(folder,        "Ribbon_Default");
        var boundary      = CreateOrLoad<BoundaryConfig>(folder,     "Boundary_Default");

        var cfg = CreateOrLoad<GameConfig>(folder, "GameConfig_Default");
        cfg.waveInput     = waveInput;
        cfg.waveGenerator = waveGenerator;
        cfg.level         = level;
        cfg.progress      = progress;
        cfg.camera        = camera;
        cfg.surfer        = surfer;
        cfg.ribbon        = ribbon;
        cfg.boundary      = boundary;

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _config = cfg;
        ClearEditors();
        EditorGUIUtility.PingObject(_config);
    }

    private static T CreateOrLoad<T>(string folder, string name) where T : ScriptableObject
    {
        string path     = $"{folder}/{name}.asset";
        var    existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        var asset = CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
