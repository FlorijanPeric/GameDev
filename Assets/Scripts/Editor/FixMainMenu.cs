using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Fixes the main menu UI: move title higher, change background color
/// </summary>
public static class FixMainMenu
{
    [MenuItem("Tools/UI/Fix Main Menu - Title Higher + Weird Color")]
    public static void FixMainMenu_Complete()
    {
        Debug.Log("=== FIXING MAIN MENU ===\n");

        // Step 1: Find title text
        Debug.Log("Step 1: Finding title text...");
        TextMeshProUGUI titleText = FindTitleText();
        if (titleText != null)
        {
            Debug.Log($"  ✓ Found title: {titleText.gameObject.name}");
            MoveTextHigher(titleText);
        }
        else
        {
            Debug.LogWarning("  ⚠ Could not find title text");
        }

        // Step 2: Find and change background color
        Debug.Log("\nStep 2: Finding background...");
        Image backgroundImage = FindBackgroundImage();
        if (backgroundImage != null)
        {
            Debug.Log($"  ✓ Found background: {backgroundImage.gameObject.name}");
            ChangeToWeirdColor(backgroundImage);
        }
        else
        {
            Debug.LogWarning("  ⚠ Could not find background image");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("\n✓ MAIN MENU FIXED!\n");
    }

    private static TextMeshProUGUI FindTitleText()
    {
        // Search for Text components
        TextMeshProUGUI[] allTexts = Object.FindObjectsOfType<TextMeshProUGUI>();
        
        foreach (TextMeshProUGUI text in allTexts)
        {
            string name = text.gameObject.name.ToLower();
            string content = text.text.ToLower();
            
            // Look for likely title candidates
            if (name.Contains("title") || name.Contains("name") || name.Contains("header") ||
                content.Contains("survival") || content.Contains("game") || content.Contains("title"))
            {
                return text;
            }
        }

        // If not found, just return the first large text element
        foreach (TextMeshProUGUI text in allTexts)
        {
            if (text.fontSize > 40)
            {
                return text;
            }
        }

        return null;
    }

    private static void MoveTextHigher(TextMeshProUGUI titleText)
    {
        RectTransform rectTransform = titleText.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // Move it higher (increase Y position)
        Vector2 anchoredPos = rectTransform.anchoredPosition;
        anchoredPos.y += 150f;  // Move 150 pixels higher
        rectTransform.anchoredPosition = anchoredPos;

        EditorUtility.SetDirty(rectTransform);
        Debug.Log($"  ✓ Moved text higher to Y: {anchoredPos.y}");
    }

    private static Image FindBackgroundImage()
    {
        // Look for images that cover the full screen
        Image[] allImages = Object.FindObjectsOfType<Image>();
        
        foreach (Image img in allImages)
        {
            string name = img.gameObject.name.ToLower();
            
            // Look for background candidates
            if (name.Contains("background") || name.Contains("panel") || 
                name.Contains("bg") || name.Contains("screen"))
            {
                RectTransform rect = img.GetComponent<RectTransform>();
                if (rect != null && rect.sizeDelta.magnitude > 500)  // Large element
                {
                    return img;
                }
            }
        }

        // Look for large image with no specific name
        foreach (Image img in allImages)
        {
            RectTransform rect = img.GetComponent<RectTransform>();
            if (rect != null && rect.sizeDelta.magnitude > 1000)  // Very large element
            {
                return img;
            }
        }

        return null;
    }

    private static void ChangeToWeirdColor(Image backgroundImage)
    {
        // Random weird colors
        Color[] weirdColors = new Color[]
        {
            new Color(0.8f, 0.2f, 0.8f, 1f),  // Magenta/Purple
            new Color(1f, 0.5f, 0f, 1f),       // Orange
            new Color(0.2f, 0.8f, 1f, 1f),     // Cyan
            new Color(0.5f, 1f, 0.5f, 1f),     // Lime Green
            new Color(1f, 0.2f, 0.5f, 1f),     // Hot Pink
            new Color(0.3f, 0.5f, 1f, 1f),     // Sky Blue
            new Color(1f, 1f, 0.2f, 1f),       // Yellow
            new Color(0.8f, 0.4f, 1f, 1f),     // Light Purple
        };

        // Pick a random weird color
        Color randomColor = weirdColors[Random.Range(0, weirdColors.Length)];
        backgroundImage.color = randomColor;

        EditorUtility.SetDirty(backgroundImage);
        Debug.Log($"  ✓ Changed background to weird color: RGB({randomColor.r:F2}, {randomColor.g:F2}, {randomColor.b:F2})");
    }

    [MenuItem("Tools/UI/Show All UI Elements")]
    public static void ShowAllUIElements()
    {
        Debug.Log("\n=== ALL UI ELEMENTS IN SCENE ===\n");

        TextMeshProUGUI[] texts = Object.FindObjectsOfType<TextMeshProUGUI>();
        Debug.Log($"Text Elements ({texts.Length}):");
        foreach (TextMeshProUGUI text in texts)
        {
            RectTransform rect = text.GetComponent<RectTransform>();
            Vector2 pos = rect != null ? rect.anchoredPosition : Vector2.zero;
            Debug.Log($"  • {text.gameObject.name}: '{text.text}' at Y:{pos.y}");
        }

        Image[] images = Object.FindObjectsOfType<Image>();
        Debug.Log($"\nImage Elements ({images.Length}):");
        foreach (Image img in images)
        {
            RectTransform rect = img.GetComponent<RectTransform>();
            Vector2 size = rect != null ? rect.sizeDelta : Vector2.zero;
            Debug.Log($"  • {img.gameObject.name}: Color={img.color}, Size={size}");
        }

        Debug.Log("\n=== END ===\n");
    }

    [MenuItem("Tools/UI/Move Title Text Higher")]
    public static void MenuMoveTitle()
    {
        TextMeshProUGUI titleText = FindTitleText();
        if (titleText != null)
        {
            MoveTextHigher(titleText);
            EditorUtility.SetDirty(titleText);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("✓ Title moved higher!");
        }
        else
        {
            Debug.LogError("Could not find title text");
        }
    }

    [MenuItem("Tools/UI/Change Background to Weird Color")]
    public static void MenuChangeBackgroundColor()
    {
        Image backgroundImage = FindBackgroundImage();
        if (backgroundImage != null)
        {
            ChangeToWeirdColor(backgroundImage);
            EditorUtility.SetDirty(backgroundImage);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("✓ Background color changed!");
        }
        else
        {
            Debug.LogError("Could not find background image");
        }
    }
}
