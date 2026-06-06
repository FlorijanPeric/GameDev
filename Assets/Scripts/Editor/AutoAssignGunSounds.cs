#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

public static class AutoAssignGunSounds
{
    [MenuItem("Tools/Survival/Auto Assign Gun Sounds")]
    public static void AssignSounds()
    {
        // Find audio clips in project
        string[] guids = AssetDatabase.FindAssets("t:AudioClip");
        AudioClip shotClip = null;
        AudioClip reloadClip = null;

        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            string name = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) continue;

            if (shotClip == null && (name.Contains("shot") || name.Contains("fire") || name.Contains("shoot") ))
            {
                shotClip = clip;
            }

            if (reloadClip == null && (name.Contains("reload") || name.Contains("clip") || name.Contains("mag")))
            {
                reloadClip = clip;
            }

            if (shotClip != null && reloadClip != null) break;
        }

        // Fallback: pick first clip if none matched
        if (shotClip == null && guids.Length > 0) shotClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));

        // Assign to all Weapon prefabs under Assets/Prefabs/Weapons and scene instances
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Weapons", "Assets/Prefabs" });
        int assignedCount = 0;

        foreach (var pg in prefabGuids)
        {
            string ppath = AssetDatabase.GUIDToAssetPath(pg);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ppath);
            if (prefab == null) continue;

            var gun = prefab.GetComponentInChildren<GunShootTracer>(true);
            if (gun != null)
            {
                SerializedObject so = new SerializedObject(gun);
                if (shotClip != null) so.FindProperty("shotClip").objectReferenceValue = shotClip;
                if (reloadClip != null) so.FindProperty("reloadClip").objectReferenceValue = reloadClip;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(prefab);
                assignedCount++;
            }
        }

        // Also assign to scene instances
        var sceneGuns = Object.FindObjectsOfType<GunShootTracer>(true);
        foreach (var sg in sceneGuns)
        {
            Undo.RecordObject(sg, "Assign gun sounds");
            if (shotClip != null) sg.shotClip = shotClip;
            if (reloadClip != null) sg.reloadClip = reloadClip;
            EditorUtility.SetDirty(sg);
            assignedCount++;
        }

        Debug.Log($"AutoAssignGunSounds: Assigned clips. ShotClip={(shotClip!=null?shotClip.name:"<none>")}, ReloadClip={(reloadClip!=null?reloadClip.name:"<none>")}. Targets updated: {assignedCount}");
    }
}
#endif
