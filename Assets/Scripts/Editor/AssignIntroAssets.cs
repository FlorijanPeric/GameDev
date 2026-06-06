#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public static class AssignIntroAssets
{
    private static GameObject FindExistingMainMenuRoot(GameObject introRoot)
    {
        string[] names = { "MainMenu", "MainMenuPanel", "Menu", "UI_Canvas", "Canvas" };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null && go != introRoot) return go;
        }

        Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
        foreach (var c in canvases)
        {
            if (c == null || c.gameObject == introRoot) continue;
            if (c.gameObject.GetComponentInChildren<Button>(true) != null)
                return c.gameObject;
        }

        return null;
    }

    private static Image FindFullscreenBackgroundImage(GameObject root)
    {
        if (root == null) return null;
        Image[] imgs = root.GetComponentsInChildren<Image>(true);
        foreach (var i in imgs)
        {
            RectTransform rt = i.rectTransform;
            if (rt != null && rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one)
                return i;
        }
        return null;
    }

    [MenuItem("Tools/Survival/Auto Assign Intro Assets")]
    public static void Assign()
    {
        string folder = "Assets/Sounds/IntroMusic";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"AssignIntroAssets: folder '{folder}' not found.");
            return;
        }

        string videoPath = folder + "/video (1).mp4";
        string imagePath = folder + "/wallpapersden.com_mutant-year-zero-seeds-of-evil-game_1920x1080.jpg";

        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(videoPath);
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
        if (bgSprite == null)
        {
            // Import the existing image as Sprite instead of creating a .sprite asset.
            var importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }

                bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            }
        }

        // find IntroSequence in scene
        var introGO = GameObject.Find("IntroSequence");
        if (introGO == null)
        {
            Debug.LogWarning("AssignIntroAssets: IntroSequence GameObject not found in scene. Run Create Intro UI first.");
        }
        else
        {
            var player = introGO.GetComponent<IntroSequencePlayer>();
            if (player != null)
            {
                SerializedObject compSo = new SerializedObject(player);
                if (clip != null)
                {
                    compSo.FindProperty("introVideo").objectReferenceValue = clip;
                }
                if (bgSprite != null)
                {
                    compSo.FindProperty("startBackgroundSprite").objectReferenceValue = bgSprite;
                }

                // pick an audio clip from folder to play under video (choose first audio clip)
                string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
                if (guids != null && guids.Length > 0)
                {
                    AudioClip audio = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    compSo.FindProperty("introAudio").objectReferenceValue = audio;
                }

                // assign existing main menu reference
                var menuRoot = FindExistingMainMenuRoot(introGO);
                if (menuRoot != null)
                {
                    compSo.FindProperty("startUI").objectReferenceValue = menuRoot;
                    var img = FindFullscreenBackgroundImage(menuRoot);
                    if (img != null) compSo.FindProperty("startBackgroundImage").objectReferenceValue = img;
                }

                compSo.ApplyModifiedProperties();
                Debug.Log("AssignIntroAssets: Assigned intro video/audio and linked existing main menu/background image.");
            }
            else
            {
                Debug.LogWarning("AssignIntroAssets: IntroSequencePlayer component not found on IntroSequence.");
            }
        }

        // Assign MainMenuMusic player playlist from this folder if present
        var mainMusicGO = GameObject.Find("MainMenuMusic");
        if (mainMusicGO != null)
        {
            var mainComp = mainMusicGO.GetComponent("MainMenuMusicPlayer");
            if (mainComp != null)
            {
                SerializedObject mainSo = new SerializedObject(mainComp);
                string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
                if (guids != null && guids.Length > 0)
                {
                    AudioClip[] clips = new AudioClip[guids.Length];
                    for (int i = 0; i < guids.Length; i++) clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[i]));
                    mainSo.FindProperty("introClips").arraySize = clips.Length;
                    for (int i = 0; i < clips.Length; i++) mainSo.FindProperty("introClips").GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
                    mainSo.FindProperty("loopClip").objectReferenceValue = clips.Length > 1 ? clips[clips.Length - 1] : clips[0];
                    mainSo.ApplyModifiedProperties();
                    Debug.Log("AssignIntroAssets: Assigned intro clips to MainMenuMusicPlayer.");
                }
            }
        }

        AssetDatabase.SaveAssets();
        EditorApplication.RepaintHierarchyWindow();
    }
}
#endif
