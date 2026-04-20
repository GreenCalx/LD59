using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class EscapeMenuSetup
{
    const string FONT_PATH = "Assets/#GAME/GFX/UI/police/GameFont.asset";

    [MenuItem("MAUVE/Setup Escape Menu")]
    public static void Run()
    {
        // ── 0. Ensure an EventSystem exists (required for all UI interaction) ──
        EnsureEventSystem();

        // ── 1. Find or create a Screen-Space Overlay canvas ──────────────────
        Canvas canvas = FindOrCreateCanvas();

        // ── 2. Root panel (full-screen dark overlay) ─────────────────────────
        GameObject panelGO = CreatePanel(canvas.transform);

        // ── 3. Content container (centred column) ────────────────────────────
        GameObject column = CreateColumn(panelGO.transform);

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);

        // ── 4. Title ──────────────────────────────────────────────────────────
        AddLabel(column.transform, "— PAUSED —", 36, font);

        AddSpacer(column.transform, 20);

        // ── 5. Volume sliders ─────────────────────────────────────────────────
        Slider masterSlider = AddSliderRow(column.transform, "Master Volume", font);
        Slider musicSlider  = AddSliderRow(column.transform, "Music Volume",  font);
        Slider fxSlider     = AddSliderRow(column.transform, "FX Volume",     font);

        AddSpacer(column.transform, 20);

        // ── 6. Buttons ────────────────────────────────────────────────────────
        Button resumeBtn = AddButton(column.transform, "RESUME", font);
        AddSpacer(column.transform, 8);
        Button quitBtn = AddButton(column.transform, "QUIT", font);

        // ── 7. Add EscapeMenu component and wire references ───────────────────
        // Attach to UIOverlay so it lives alongside the other UI controllers.
        EscapeMenu escMenu = canvas.gameObject.GetComponent<EscapeMenu>()
                          ?? canvas.gameObject.AddComponent<EscapeMenu>();

        SerializedObject so = new SerializedObject(escMenu);
        so.FindProperty("menuPanel")    .objectReferenceValue = panelGO;
        so.FindProperty("masterSlider") .objectReferenceValue = masterSlider;
        so.FindProperty("musicSlider")  .objectReferenceValue = musicSlider;
        so.FindProperty("fxSlider")     .objectReferenceValue = fxSlider;
        so.FindProperty("resumeButton") .objectReferenceValue = resumeBtn;
        so.FindProperty("quitButton")   .objectReferenceValue = quitBtn;
        so.ApplyModifiedProperties();

        // Wire buttons → EscapeMenu methods
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            resumeBtn.onClick, escMenu.Resume);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            quitBtn.onClick, escMenu.Quit);

        // ── 8. Mark scene dirty ───────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorGUIUtility.PingObject(panelGO);
        Debug.Log("[MAUVE] Escape Menu created. Save the scene (Ctrl+S) to keep it.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static Canvas FindOrCreateCanvas()
    {
        Canvas canvas = null;

        GameObject named = GameObject.Find("UIOverlay");
        if (named != null)
            canvas = named.GetComponent<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("[MAUVE] No GameObject named 'UIOverlay' found — creating a new Canvas.");
            GameObject cgo = new GameObject("UIOverlay");
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99;
            CanvasScaler scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // Always ensure a GraphicRaycaster exists — without it mouse events never reach UI elements.
        if (canvas.gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log("[MAUVE] Added GraphicRaycaster to UIOverlay.");
        }

        return canvas;
    }

    static GameObject CreatePanel(Transform parent)
    {
        GameObject go = new GameObject("EscapeMenuPanel");
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.75f);

        return go;
    }

    static GameObject CreateColumn(Transform parent)
    {
        GameObject go = new GameObject("Column");
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(500, 500);
        rt.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment           = TextAnchor.MiddleCenter;
        vlg.childControlWidth        = true;
        vlg.childControlHeight       = false;
        vlg.childForceExpandWidth    = true;
        vlg.childForceExpandHeight   = false;
        vlg.spacing                  = 12;
        vlg.padding                  = new RectOffset(20, 20, 20, 20);

        ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return go;
    }

    static void AddLabel(Transform parent, string text, int fontSize, TMP_FontAsset font)
    {
        GameObject go = new GameObject("Label_" + text.Replace(" ", ""));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(460, fontSize + 10);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        if (font != null) tmp.font = font;
    }

    static Slider AddSliderRow(Transform parent, string label, TMP_FontAsset font)
    {
        // Row container
        GameObject row = new GameObject("Row_" + label.Replace(" ", ""));
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.AddComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(460, 40);
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childControlHeight     = true;
        hlg.childControlWidth      = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing                = 12;

        // Highlight background (ignored by layout, sits behind everything)
        Image highlightImg = AddHighlightOverlay(row);

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(row.transform, false);
        RectTransform lrt = labelGO.AddComponent<RectTransform>();
        lrt.sizeDelta = new Vector2(160, 40);
        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color     = Color.white;
        if (font != null) tmp.font = font;

        // Slider
        Slider slider = BuildSlider(row.transform);

        // Wire highlight
        MenuItemHighlight mih = slider.gameObject.AddComponent<MenuItemHighlight>();
        SerializedObject mihSo = new SerializedObject(mih);
        mihSo.FindProperty("highlightImage").objectReferenceValue = highlightImg;
        mihSo.ApplyModifiedProperties();

        return slider;
    }

    static Slider BuildSlider(Transform parent)
    {
        // Root
        GameObject sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(parent, false);
        RectTransform sliderRt = sliderGO.AddComponent<RectTransform>();
        sliderRt.sizeDelta = new Vector2(280, 30);

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin  = new Vector2(0, 0.25f);
        bgRt.anchorMax  = new Vector2(1, 0.75f);
        bgRt.offsetMin  = Vector2.zero;
        bgRt.offsetMax  = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin  = new Vector2(0, 0.25f);
        faRt.anchorMax  = new Vector2(1, 0.75f);
        faRt.offsetMin  = new Vector2(5, 0);
        faRt.offsetMax  = new Vector2(-15, 0);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(0, 1);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = new Vector2(10, 0);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        // Handle area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        RectTransform haRt = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = new Vector2(0, 0);
        haRt.anchorMax = new Vector2(1, 1);
        haRt.offsetMin = new Vector2(10, 0);
        haRt.offsetMax = new Vector2(-10, 0);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hRt = handle.AddComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(20, 0);
        hRt.anchorMin = new Vector2(0, 0);
        hRt.anchorMax = new Vector2(0, 1);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        // Wire Slider component
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.fillRect   = fillRt;
        slider.handleRect = hRt;
        slider.targetGraphic = handleImg;
        slider.minValue   = 0f;
        slider.maxValue   = 1f;
        slider.value      = 1f;
        slider.direction  = Slider.Direction.LeftToRight;

        return slider;
    }

    static Button AddButton(Transform parent, string label, TMP_FontAsset font)
    {
        GameObject go = new GameObject("ResumeButton");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 50);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.15f, 0.15f, 0.15f);
        cb.highlightedColor = new Color(0.15f, 0.15f, 0.15f);
        cb.selectedColor    = new Color(0.15f, 0.15f, 0.15f);
        cb.pressedColor     = new Color(0.05f, 0.05f, 0.05f);
        btn.colors          = cb;
        btn.targetGraphic   = img;

        // Highlight overlay (sits on top of the button background, behind the text)
        Image highlightImg = AddHighlightOverlay(go);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        RectTransform trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        if (font != null) tmp.font = font;

        // Wire highlight
        MenuItemHighlight mih = btn.gameObject.AddComponent<MenuItemHighlight>();
        SerializedObject mihSo = new SerializedObject(mih);
        mihSo.FindProperty("highlightImage").objectReferenceValue = highlightImg;
        mihSo.ApplyModifiedProperties();

        return btn;
    }

    static Image AddHighlightOverlay(GameObject parent)
    {
        GameObject go = new GameObject("Highlight");
        go.transform.SetParent(parent.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Keep it out of any LayoutGroup calculations.
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        Image img = go.AddComponent<Image>();
        img.color        = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = false;

        // Place behind siblings so it doesn't block clicks.
        go.transform.SetAsFirstSibling();

        return img;
    }

    static void AddSpacer(Transform parent, float height)
    {
        GameObject go = new GameObject("Spacer");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1, height);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight      = height;
        le.preferredHeight = height;
    }

    static void EnsureEventSystem()
    {
        EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
        if (existing != null)
        {
            // Replace StandaloneInputModule with InputSystemUIInputModule if needed.
            var legacy = existing.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                Object.DestroyImmediate(legacy);
                existing.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Debug.Log("[MAUVE] Replaced StandaloneInputModule with InputSystemUIInputModule.");
            }
            return;
        }

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        Debug.Log("[MAUVE] Created EventSystem with InputSystemUIInputModule.");
    }
}
