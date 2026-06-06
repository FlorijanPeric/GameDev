#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class AutoSetupSurvivalManager
{
    [MenuItem("Tools/Survival/Auto Setup SurvivalManager")]
    public static void CreateSurvivalManager()
    {
        // Find or create manager object
        GameObject manager = GameObject.Find("SurvivalManager");
        if (manager == null)
        {
            manager = new GameObject("SurvivalManager");
            Undo.RegisterCreatedObjectUndo(manager, "Create SurvivalManager");
        }

        // Add or get components
        var map = manager.GetComponent<MapEnlargementSystem>();
        if (map == null) map = Undo.AddComponent<MapEnlargementSystem>(manager);

        var round = manager.GetComponent<SurvivalRound2System>();
        if (round == null) round = Undo.AddComponent<SurvivalRound2System>(manager);

        // Auto-find BoxColliders for walls/terrain by name heuristics
        BoxCollider[] allBoxColliders = Object.FindObjectsOfType<BoxCollider>(true);
        List<BoxCollider> walls = new List<BoxCollider>();
        List<BoxCollider> terrains = new List<BoxCollider>();

        foreach (var bc in allBoxColliders)
        {
            string n = bc.gameObject.name.ToLower();
            if (n.Contains("wall") || n.Contains("fence") || n.Contains("barrier") || n.Contains("boundary"))
            {
                walls.Add(bc);
            }
            else if (n.Contains("terrain") || n.Contains("ground") || n.Contains("floor"))
            {
                terrains.Add(bc);
            }
        }

        if (walls.Count == 0 && allBoxColliders.Length > 0)
        {
            // Fallback: treat larger box colliders as walls
            foreach (var bc in allBoxColliders)
            {
                if (bc.size.magnitude > 1.5f) walls.Add(bc);
            }
        }

        // Final fallback: assign all colliders as walls if nothing found
        if (walls.Count == 0 && allBoxColliders.Length > 0)
        {
            walls.AddRange(allBoxColliders);
        }

        map.wallColliders = walls.ToArray();
        map.terrainColliders = terrains.ToArray();

        // Assign spawn points
        var spawns = Object.FindObjectsOfType<SurvivalSpawnPoint>(true);
        map.spawnPoints = spawns;

        // Assign enemy spawner
        var spawner = Object.FindObjectOfType<SurvivalEnemySpawner>();
        if (spawner != null)
        {
            round.enemySpawner = spawner;
        }

        // Link map enlarger to round system
        round.mapEnlarger = map;

        // Try to find a UI Text for announcement
        Text[] texts = Object.FindObjectsOfType<Text>(true);
        Text found = null;
        foreach (var t in texts)
        {
            if (t.gameObject.name.ToLower().Contains("round2") || t.gameObject.name.ToLower().Contains("round"))
            {
                found = t;
                break;
            }
        }
        if (found == null && texts.Length > 0) found = texts[0];
        round.round2AnnounceText = found;

        // Mark scene dirty so user can save
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        // Select manager in editor
        Selection.activeGameObject = manager;

        Debug.Log("AutoSetupSurvivalManager: Created/updated SurvivalManager and wired common references. Please review assignments in the Inspector.");
    }
}
#endif
