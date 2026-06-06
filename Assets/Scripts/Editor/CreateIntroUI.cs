#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor.SceneManagement;

public static class CreateIntroUI
{
    private static GameObject FindExistingMainMenuRoot(GameObject introCanvas)
    {
        // Try common names first.
        string[] names = { "MainMenu", "MainMenuPanel", "Menu", "UI_Canvas", "Canvas" };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null && go != introCanvas) return go;
        }

        // Fallback: any canvas with interactive UI that is not this intro canvas.
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
        foreach (var c in canvases)
        {
            if (c == null || c.gameObject == introCanvas) continue;
            if (c.gameObject.GetComponentInChildren<Button>(true) != null)
                return c.gameObject;
        }

        return null;
    }

    [MenuItem("Tools/Survival/Create Intro UI")]
    public static void Create()
    {
        // Create Canvas
        GameObject canvasGO = new GameObject("IntroCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create IntroCanvas");
        var canvas = Undo.AddComponent<Canvas>(canvasGO);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Undo.AddComponent<CanvasScaler>(canvasGO);
        Undo.AddComponent<GraphicRaycaster>(canvasGO);

        // Panel background
        GameObject panel = new GameObject("IntroPanel");
        Undo.RegisterCreatedObjectUndo(panel, "Create IntroPanel");
        panel.transform.SetParent(canvasGO.transform, false);
        var img = Undo.AddComponent<Image>(panel);
        // keep panel transparent so video/image can render full-screen
        img.color = new Color(0f, 0f, 0f, 0f);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // Intro image
        GameObject imageGO = new GameObject("IntroImage");
        imageGO.transform.SetParent(panel.transform, false);
        var image = Undo.AddComponent<Image>(imageGO);
        RectTransform imgRt = imageGO.GetComponent<RectTransform>();
        // make intro image full-screen (used when no video)
        imgRt.anchorMin = Vector2.zero;
        imgRt.anchorMax = Vector2.one;
        imgRt.offsetMin = Vector2.zero;
        imgRt.offsetMax = Vector2.zero;

        // Subtitle text
        GameObject subtitleGO = new GameObject("IntroSubtitle");
        subtitleGO.transform.SetParent(panel.transform, false);
        var text = Undo.AddComponent<Text>(subtitleGO);
        text.alignment = TextAnchor.LowerCenter;
        text.color = Color.white;
        text.fontSize = 22;
        // assign default font (use LegacyRuntime.ttf for newer Unity versions)
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform subRt = subtitleGO.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.05f, 0.05f);
        subRt.anchorMax = new Vector2(0.95f, 0.18f);
        subRt.offsetMin = Vector2.zero;
        subRt.offsetMax = Vector2.zero;

        // Skip button
        GameObject skipGO = new GameObject("SkipButton");
        skipGO.transform.SetParent(panel.transform, false);
        var btn = Undo.AddComponent<Button>(skipGO);
        var btnImg = Undo.AddComponent<Image>(skipGO);
        btnImg.color = new Color(1f, 1f, 1f, 0.15f);
        RectTransform btnRt = skipGO.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.85f, 0.02f);
        btnRt.anchorMax = new Vector2(0.98f, 0.12f);
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;

        GameObject btnTextGO = new GameObject("Text");
        btnTextGO.transform.SetParent(skipGO.transform, false);
        var btText = Undo.AddComponent<Text>(btnTextGO);
        btText.text = "Skip";
        btText.alignment = TextAnchor.MiddleCenter;
        btText.color = Color.white;
        btText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform btRt = btnTextGO.GetComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.offsetMin = Vector2.zero;
        btRt.offsetMax = Vector2.zero;

        // Attach IntroSequencePlayer
        GameObject playerGO = new GameObject("IntroSequence");
        Undo.RegisterCreatedObjectUndo(playerGO, "Create IntroSequence");
        playerGO.transform.SetParent(canvasGO.transform, false);
        var player = Undo.AddComponent<IntroSequencePlayer>(playerGO);

        // Video RawImage (behind subtitles/image)
        GameObject rawGO = new GameObject("IntroVideoRaw");
        rawGO.transform.SetParent(panel.transform, false);
        var raw = Undo.AddComponent<RawImage>(rawGO);
        RectTransform rawRt = rawGO.GetComponent<RectTransform>();
        // video covers the whole screen
        rawRt.anchorMin = Vector2.zero;
        rawRt.anchorMax = Vector2.one;
        rawRt.offsetMin = Vector2.zero;
        rawRt.offsetMax = Vector2.zero;

        // VideoPlayer component on playerGO
        var vp = Undo.AddComponent<VideoPlayer>(playerGO);
        vp.playOnAwake = false;

        // Try to link the existing main menu instead of creating a replacement.
        GameObject existingMenuRoot = FindExistingMainMenuRoot(canvasGO);
        Image existingMenuBackground = null;
        if (existingMenuRoot != null)
        {
            Image[] imgs = existingMenuRoot.GetComponentsInChildren<Image>(true);
            foreach (var i in imgs)
            {
                RectTransform rt = i.rectTransform;
                if (rt != null && rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one)
                {
                    existingMenuBackground = i;
                    break;
                }
            }
        }

        // Wire references
        Undo.RecordObject(player, "Wire Intro UI");
        player.uiImage = image;
        player.subtitleText = text;
        player.skipButton = btn;
        player.audioSource = Undo.AddComponent<AudioSource>(playerGO);
        player.audioSource.playOnAwake = false;
        player.introAudio = null; // user will assign
        player.introImage = null; // user will assign
        player.videoRawImage = raw;
        player.videoPlayer = vp;
        player.startUI = existingMenuRoot;
        player.startBackgroundImage = existingMenuBackground;
        player.hideMenuDuringIntro = true;
        player.totalDuration = 30f;
        player.playOnStart = true;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = canvasGO;

        Debug.Log($"CreateIntroUI: Intro UI created. Existing menu linked: {(existingMenuRoot != null ? existingMenuRoot.name : "<none>")}. Assign your image/audio and subtitle timings on the IntroSequence component.");
    }
}
#endif
