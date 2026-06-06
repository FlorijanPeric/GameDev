#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class CreateMainMenuMusic
{
    [MenuItem("Tools/Survival/Create Main Menu Music Player")]
    public static void Create()
    {
        // accept both lower/upper 'sound' and plural 'Sounds' folder names
        string[] candidateFolders = new[] { "Assets/sound/IntroMusic", "Assets/Sound/IntroMusic", "Assets/Sounds/IntroMusic" };
        string folder = null;
        AudioClip[] clips = null;
        foreach (var f in candidateFolders) if (AssetDatabase.IsValidFolder(f)) { folder = f; break; }

        if (!string.IsNullOrEmpty(folder))
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
            clips = new AudioClip[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[i]));
            }
        }

        GameObject go = GameObject.Find("MainMenuMusic");
        if (go == null) {
            go = new GameObject("MainMenuMusic");
            Undo.RegisterCreatedObjectUndo(go, "Create MainMenuMusic");
        }

        AudioSource src = go.GetComponent<AudioSource>();
        if (src == null) src = Undo.AddComponent<AudioSource>(go);
        src.playOnAwake = false;
        src.spatialBlend = 0f;

        var player = go.GetComponent<MainMenuMusicPlayer>();
        if (player == null) player = Undo.AddComponent<MainMenuMusicPlayer>(go);

        Undo.RecordObject(player, "Assign MainMenu Music");
        if (clips != null && clips.Length > 0)
        {
            player.introClips = clips;
            // use the last clip as loop clip if there are multiple, otherwise reuse first
            player.loopClip = clips.Length > 1 ? clips[clips.Length - 1] : clips[0];
        }

        player.volume = 0.65f;
        player.playOnStart = true;
        player.fadeInSeconds = 0.9f;
        player.loopAfterIntro = true;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = go;

        Debug.Log($"CreateMainMenuMusic: Created/updated MainMenuMusic player. Clips assigned from {folder ?? "<none>"} (found {(clips!=null?clips.Length:0)} clips).");
    }
}
#endif
