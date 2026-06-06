using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Saves current primary weapon pose into WeaponManager and as an empty anchor prefab preset.
/// </summary>
public static class SavePrimaryWeaponPosePreset
{
    private const string AnchorFolderRoot = "Assets/Prefabs";
    private const string AnchorFolder = "Assets/Prefabs/WeaponAnchors";
    private const string AnchorPrefabPath = "Assets/Prefabs/WeaponAnchors/PrimaryWeaponAnchor.prefab";

    [MenuItem("Tools/Weapons/Anchors/Save Current Primary Pose Preset")]
    public static void SaveCurrentPrimaryPosePreset()
    {
        WeaponManager manager = Object.FindObjectOfType<WeaponManager>();
        if (manager == null)
        {
            Debug.LogError("No WeaponManager found in the active scene.");
            return;
        }

        if (manager.primaryWeapon == null)
        {
            Debug.LogError("WeaponManager has no primary weapon assigned.");
            return;
        }

        if (manager.primaryWeapon.weaponModel == null)
        {
            Debug.LogError("Primary weapon has no weaponModel assigned.");
            return;
        }

        Transform model = manager.primaryWeapon.weaponModel.transform;

        // Stored pose excludes the global offset because WeaponManager reapplies it when snapping.
        manager.usePrimaryPoseOverride = true;
        manager.primaryLocalPosition = model.localPosition - manager.globalWeaponPositionOffset;
        manager.primaryLocalEuler = model.localEulerAngles;
        manager.primaryLocalScale = model.localScale;

        EnsureAnchorFolders();
        CreateOrUpdateAnchorPrefab(manager.primaryLocalPosition, manager.primaryLocalEuler, manager.primaryLocalScale);

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(manager.primaryWeapon);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Saved primary weapon pose preset and updated prefab: " + AnchorPrefabPath);
    }

    private static void EnsureAnchorFolders()
    {
        if (!AssetDatabase.IsValidFolder(AnchorFolderRoot))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        if (!AssetDatabase.IsValidFolder(AnchorFolder))
        {
            AssetDatabase.CreateFolder(AnchorFolderRoot, "WeaponAnchors");
        }
    }

    private static void CreateOrUpdateAnchorPrefab(Vector3 localPosition, Vector3 localEuler, Vector3 localScale)
    {
        GameObject anchor = new GameObject("PrimaryWeaponAnchor");
        anchor.transform.localPosition = localPosition;
        anchor.transform.localRotation = Quaternion.Euler(localEuler);
        anchor.transform.localScale = localScale;

        PrefabUtility.SaveAsPrefabAsset(anchor, AnchorPrefabPath);
        Object.DestroyImmediate(anchor);
    }
}
