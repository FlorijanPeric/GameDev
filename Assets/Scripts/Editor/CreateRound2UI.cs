#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

public static class CreateRound2UI
{
    [MenuItem("Tools/Survival/Create Round 2 UI")]
    public static void CreateUI()
    {
        // Create Canvas
        GameObject canvasGO = new GameObject("Round2Canvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Round2 Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Create a centered Text
        GameObject textGO = new GameObject("Round2Text");
        Undo.RegisterCreatedObjectUndo(textGO, "Create Round2 Text");
        textGO.transform.SetParent(canvasGO.transform, false);
        Text text = textGO.AddComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 72;
        text.color = Color.red;
        text.text = "";

        RectTransform rt = text.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.7f);
        rt.anchorMax = new Vector2(0.9f, 0.95f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Assign a default font if none
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        // Create an AudioSource on the canvas
        AudioSource audio = canvasGO.AddComponent<AudioSource>();
        audio.playOnAwake = false;

        // Try to find a SurvivalRound2System instance and wire fields
        SurvivalRound2System roundSystem = Object.FindObjectOfType<SurvivalRound2System>();
        if (roundSystem != null)
        {
            Undo.RecordObject(roundSystem, "Assign Round2 UI");
            roundSystem.round2AnnounceText = text;
            roundSystem.round2AudioSource = audio;
            EditorUtility.SetDirty(roundSystem);
        }

        // Mark scene dirty
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Selection.activeGameObject = canvasGO;

        Debug.Log("CreateRound2UI: Canvas, Text, and AudioSource created and wired to SurvivalRound2System (if present).\nReview and assign an AudioClip to the system in Inspector.");
    }
}
#endif
