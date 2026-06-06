#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CreateBackgroundMusic
{
    [MenuItem("Tools/Survival/Create Background Music Player")]
    public static void CreateMusicPlayer()
    {
        // Prefer clips under Assets/sound/music or Assets/Sounds/Music (case-insensitive), then fall back to other folders
        AudioClip selected = null;
        string[] preferFolders = new[] { "Assets/sound/music", "Assets/Sound/Music", "Assets/Sounds/Music", "Assets/Audio/Music" };
        foreach (var folder in preferFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            string[] folderGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
            if (folderGuids != null && folderGuids.Length > 0)
            {
                // pick first matching ambient-like clip in the folder
                foreach (var g in folderGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    string name = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                    if (name.Contains("ambient") || name.Contains("background") || name.Contains("soft") || name.Contains("atmospheric") || name.Contains("pad") || name.Contains("amb"))
                    {
                        selected = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                        break;
                    }
                }

                // if none matched heuristics, just take the first clip in the folder
                if (selected == null && folderGuids.Length > 0)
                {
                    selected = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(folderGuids[0]));
                }

                if (selected != null) break;
            }
        }

        // Global fallback: search entire project for a suitable clip
        if (selected == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip");
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                string name = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (name.Contains("ambient") || name.Contains("background") || name.Contains("soft") || name.Contains("atmospheric") || name.Contains("pad") || name.Contains("amb"))
                {
                    selected = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    break;
                }
            }

            if (selected == null && guids.Length > 0)
            {
                selected = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        GameObject go = GameObject.Find("BackgroundMusic");
        if (go == null)
        {
            go = new GameObject("BackgroundMusic");
            Undo.RegisterCreatedObjectUndo(go, "Create BackgroundMusic");
        }

        AudioSource src = go.GetComponent<AudioSource>();
        if (src == null) src = Undo.AddComponent<AudioSource>(go);
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f; // 2D

        var player = go.GetComponent<BackgroundMusicPlayer>();
        if (player == null) player = Undo.AddComponent<BackgroundMusicPlayer>(go);

        Undo.RecordObject(player, "Assign background music");
        player.volume = 0.45f;
        player.loop = true;
        player.playOnStart = true;
        player.fadeInSeconds = 1.2f;

        // Populate playlist from preferred folders if present, otherwise use selected single clip
        System.Collections.Generic.List<AudioClip> playlist = new System.Collections.Generic.List<AudioClip>();
        foreach (var folder in new[] { "Assets/sound/music", "Assets/Sound/Music", "Assets/Sounds/Music", "Assets/Audio/Music" })
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            string[] folderGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
            foreach (var g in folderGuids)
            {
                AudioClip c = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g));
                if (c != null) playlist.Add(c);
            }
            if (playlist.Count > 0) break;
        }

        if (playlist.Count == 0)
        {
            if (selected != null) playlist.Add(selected);
        }

        if (playlist.Count > 0)
        {
            player.playlist = playlist.ToArray();
            player.usePlaylist = true;
            player.GetComponent<AudioSource>().clip = player.playlist[0];
        }
        else
        {
            player.playlist = null;
            player.musicClip = selected;
            player.usePlaylist = false;
            player.GetComponent<AudioSource>().clip = selected;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = go;

        Debug.Log($"CreateBackgroundMusic: Created/updated BackgroundMusic player. Assigned clip: {(selected!=null?selected.name:"<none>")}");
    }
}
#endif
