#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public static class CreateBackgroundMusicUI
{
    [MenuItem("Tools/Survival/Create Background Music Toggle UI")]
    public static void CreateUI()
    {
        // Find or create a Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        GameObject canvasGO;
        if (canvas != null)
        {
            canvasGO = canvas.gameObject;
        }
        else
        {
            canvasGO = new GameObject("UI_Canvas");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create UI Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Create panel to hold toggle
        GameObject panel = new GameObject("MusicTogglePanel");
        Undo.RegisterCreatedObjectUndo(panel, "Create Music Panel");
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.01f, 0.95f);
        prt.anchorMax = new Vector2(0.2f, 0.99f);
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f,0f,0f,0.35f);

        // Create Toggle
        GameObject toggleGO = new GameObject("MusicToggle");
        Undo.RegisterCreatedObjectUndo(toggleGO, "Create Music Toggle");
        toggleGO.transform.SetParent(panel.transform, false);
        Toggle toggle = toggleGO.AddComponent<Toggle>();

        RectTransform tr = toggleGO.AddComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.02f, 0.12f);
        tr.anchorMax = new Vector2(0.35f, 0.88f);
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        // Add background and checkmark images
        GameObject bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(toggleGO.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = Color.white;
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.1f);
        bgRt.anchorMax = new Vector2(0.3f, 0.9f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        GameObject mark = new GameObject("Checkmark", typeof(RectTransform));
        mark.transform.SetParent(bg.transform, false);
        Image markImg = mark.AddComponent<Image>();
        markImg.color = Color.green;
        RectTransform markRt = mark.GetComponent<RectTransform>();
        markRt.anchorMin = new Vector2(0.1f, 0.1f);
        markRt.anchorMax = new Vector2(0.9f, 0.9f);
        markRt.offsetMin = Vector2.zero;
        markRt.offsetMax = Vector2.zero;

        toggle.targetGraphic = bgImg;
        toggle.graphic = markImg;

        // Create label
        GameObject labelGO = new GameObject("MusicLabel", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(labelGO, "Create Music Label");
        labelGO.transform.SetParent(panel.transform, false);
        Text label = labelGO.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = 14;
        label.alignment = TextAnchor.MiddleLeft;
        RectTransform lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0.38f, 0.1f);
        lrt.anchorMax = new Vector2(0.98f, 0.9f);
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        label.text = "Music: On";

        // Add BackgroundMusicController if missing
        GameObject bgMusicGO = GameObject.Find("BackgroundMusic");
        if (bgMusicGO == null)
        {
            Debug.LogWarning("CreateBackgroundMusicUI: No BackgroundMusic GameObject found. Create it via Tools -> Survival -> Create Background Music Player first.");
        }
        else
        {
            var controller = bgMusicGO.GetComponent<BackgroundMusicController>();
            if (controller == null) controller = Undo.AddComponent<BackgroundMusicController>(bgMusicGO);
        }

        // Add BackgroundMusicUI component to toggle and wire label
        var uiComp = Undo.AddComponent<BackgroundMusicUI>(toggleGO);
        uiComp.musicToggle = toggle;
        uiComp.labelText = label;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = panel;

        Debug.Log("CreateBackgroundMusicUI: Created music toggle UI. If background music exists, it will be controlled by the toggle.");
    }
}
#endif
