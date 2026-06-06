using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SurvivalEnemySetupTool
{
    private const string ZombieControllerPath = "Assets/ArtStore3D/Fat Zombie(Low Poly)/Anim/ZombieAnimator.controller";
    private const string FatZombiePrefabPath = "Assets/ArtStore3D/Fat Zombie(Low Poly)/Prefab/FatZombie.prefab";

    [MenuItem("Tools/Survival/Fix Purple Enemy Materials")]
    public static void FixPurpleEnemyMaterials()
    {
        int fixedMaterials = 0;
        int touchedPrefabs = 0;

        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string lower = path.ToLowerInvariant();

            bool looksLikeEnemy =
                lower.Contains("zombie") ||
                lower.Contains("enemy") ||
                lower.Contains("undead") ||
                lower.Contains("monster");

            if (!looksLikeEnemy)
            {
                continue;
            }

            if (lower.Contains("icon") || lower.Contains("lod") || lower.Contains("pose"))
            {
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                continue;
            }

            int fixedInPrefab = EnemyMaterialFixer.FixObjectMaterials(root, true);
            if (fixedInPrefab > 0)
            {
                fixedMaterials += fixedInPrefab;
                touchedPrefabs++;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        ZombieChaseAI[] sceneEnemies = Object.FindObjectsOfType<ZombieChaseAI>(true);
        for (int i = 0; i < sceneEnemies.Length; i++)
        {
            if (sceneEnemies[i] == null)
            {
                continue;
            }

            fixedMaterials += EnemyMaterialFixer.FixObjectMaterials(sceneEnemies[i].gameObject, true);
            SetDirtySafe(sceneEnemies[i].gameObject);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        MarkSceneDirtySafe();

        Debug.Log($"Enemy material repair complete. Fixed materials: {fixedMaterials}, prefabs updated: {touchedPrefabs}, scene enemies scanned: {sceneEnemies.Length}.");
    }

    [MenuItem("Tools/Survival/Auto Assign Enemy Prefabs To Spawner")]
    public static void AutoAssignEnemyPrefabs()
    {
        SurvivalEnemySpawner spawner = Object.FindObjectOfType<SurvivalEnemySpawner>();
        if (spawner == null)
        {
            Debug.LogError("No SurvivalEnemySpawner found in the active scene.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        List<GameObject> found = new List<GameObject>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string lower = path.ToLowerInvariant();

            bool looksLikeEnemy =
                lower.Contains("zombie") ||
                lower.Contains("enemy") ||
                lower.Contains("undead") ||
                lower.Contains("monster");

            if (!looksLikeEnemy)
            {
                continue;
            }

            if (lower.Contains("pose") || lower.Contains("lod") || lower.Contains("icon"))
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                continue;
            }

            found.Add(prefab);
        }

        if (found.Count == 0)
        {
            Debug.LogWarning("No enemy-like prefabs found by name. Rename prefabs to include zombie/enemy or assign manually.");
            return;
        }

        Undo.RecordObject(spawner, "Assign enemy prefabs");
        spawner.enemyPrefabs = found.ToArray();
        AssignSpawnerDefaultAnimatorController(spawner);

        MarkSceneDirtySafe();
        Debug.Log($"Assigned {found.Count} enemy prefabs to SurvivalEnemySpawner.");
    }

    [MenuItem("Tools/Survival/Create Wave Director In Scene")]
    public static void CreateWaveDirectorInScene()
    {
        SurvivalWaveDirector existing = Object.FindObjectOfType<SurvivalWaveDirector>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("SurvivalWaveDirector already exists. Selected existing object.");
            return;
        }

        GameObject go = new GameObject("SurvivalWaveDirector");
        if (!EditorApplication.isPlaying)
        {
            Undo.RegisterCreatedObjectUndo(go, "Create SurvivalWaveDirector");
        }
        SurvivalWaveDirector director = go.AddComponent<SurvivalWaveDirector>();
        director.spawner = Object.FindObjectOfType<SurvivalEnemySpawner>();

        MarkSceneDirtySafe();
        Selection.activeGameObject = go;
        Debug.Log("Created SurvivalWaveDirector in scene.");
    }

    [MenuItem("Tools/Survival/Apply Animator Controller To Scene Enemies")]
    public static void ApplyAnimatorControllerToSceneEnemies()
    {
        AnimatorController controller = GetOrCreateZombieAnimatorController();

        if (controller == null)
        {
            Debug.LogError("Could not load or create ZombieAnimator.controller.");
            return;
        }

        ZombieChaseAI[] sceneEnemies = Object.FindObjectsOfType<ZombieChaseAI>(true);
        int updated = 0;

        for (int i = 0; i < sceneEnemies.Length; i++)
        {
            if (sceneEnemies[i] == null)
            {
                continue;
            }

            Animator animator = sceneEnemies[i].GetComponent<Animator>();
            if (animator == null)
            {
                animator = sceneEnemies[i].gameObject.AddComponent<Animator>();
            }

            if (animator.runtimeAnimatorController != controller)
            {
                animator.runtimeAnimatorController = controller;
                updated++;
                SetDirtySafe(sceneEnemies[i].gameObject);
            }
        }

        MarkSceneDirtySafe();
        Debug.Log($"Applied ZombieAnimator.controller to {updated} scene enemies.");
    }

    [MenuItem("Tools/Survival/Create Zombie Animator Controller")]
    public static void CreateZombieAnimatorController()
    {
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ZombieControllerPath);
        if (existing != null)
        {
            Debug.Log("Zombie Animator Controller already exists at: " + ZombieControllerPath);
            return;
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ZombieControllerPath);

        if (controller == null)
        {
            Debug.LogError("Failed to create animator controller");
            return;
        }

        controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Idle", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;

        AnimatorState idleState = stateMachine.AddState("Idle");
        AnimatorState moveState = stateMachine.AddState("Move");
        AnimatorState attackState = stateMachine.AddState("Attack");
        AnimatorState deathState = stateMachine.AddState("Death");

        stateMachine.defaultState = idleState;

        // Idle -> Move
        AnimatorStateTransition idleToMove = idleState.AddTransition(moveState);
        idleToMove.AddCondition(AnimatorConditionMode.Greater, 0.1f, "MoveSpeed");
        idleToMove.exitTime = 0f;
        idleToMove.hasExitTime = false;

        // Move -> Idle
        AnimatorStateTransition moveToIdle = moveState.AddTransition(idleState);
        moveToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "MoveSpeed");
        moveToIdle.exitTime = 0f;
        moveToIdle.hasExitTime = false;

        // Idle -> Attack
        AnimatorStateTransition idleToAttack = idleState.AddTransition(attackState);
        idleToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        idleToAttack.exitTime = 0f;
        idleToAttack.hasExitTime = false;

        // Move -> Attack
        AnimatorStateTransition moveToAttack = moveState.AddTransition(attackState);
        moveToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        moveToAttack.exitTime = 0f;
        moveToAttack.hasExitTime = false;

        // Attack -> Idle
        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.exitTime = 0.8f;
        attackToIdle.hasExitTime = true;

        // Any -> Death
        AnimatorStateTransition anyToDeath = stateMachine.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Death");
        anyToDeath.exitTime = 0f;
        anyToDeath.hasExitTime = false;

        SetDirtySafe(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Zombie Animator Controller created at: " + ZombieControllerPath);
    }

    [MenuItem("Tools/Survival/Apply Animator To FatZombie Prefab")]
    public static void ApplyAnimatorToFatZombiePrefab()
    {
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FatZombiePrefabPath);

        if (prefabAsset == null)
        {
            Debug.LogError("Failed to load FatZombie prefab at " + FatZombiePrefabPath);
            return;
        }

        AnimatorController controller = GetOrCreateZombieAnimatorController();

        if (controller == null)
        {
            Debug.LogError("Could not load or create ZombieAnimator.controller.");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(FatZombiePrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError("Failed to open prefab contents for: " + FatZombiePrefabPath);
            return;
        }

        Animator animator = prefabRoot.GetComponent<Animator>();
        if (animator == null)
        {
            animator = prefabRoot.AddComponent<Animator>();
            Debug.Log("Added Animator component to FatZombie prefab");
        }

        animator.runtimeAnimatorController = controller;
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, FatZombiePrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        AssetDatabase.SaveAssets();
        Debug.Log("Animator Controller assigned to FatZombie prefab");
    }

    [MenuItem("Tools/Survival/Setup Zombie (All Steps)")]
    public static void SetupZombieComplete()
    {
        Debug.Log("Starting zombie setup...");
        CreateZombieAnimatorController();
        EditorApplication.delayCall += () => {
            ApplyAnimatorToFatZombiePrefab();
            EditorApplication.delayCall += () => {
                ApplyAnimatorControllerToSceneEnemies();
                SurvivalEnemySpawner spawner = Object.FindObjectOfType<SurvivalEnemySpawner>();
                if (spawner != null)
                {
                    AssignSpawnerDefaultAnimatorController(spawner);
                    MarkSceneDirtySafe();
                }
            };
        };
    }

    private static void MarkSceneDirtySafe()
    {
        if (EditorApplication.isPlaying)
        {
            return;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void SetDirtySafe(Object obj)
    {
        if (obj == null || EditorApplication.isPlaying)
        {
            return;
        }

        EditorUtility.SetDirty(obj);
    }

    private static AnimatorController GetOrCreateZombieAnimatorController()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ZombieControllerPath);
        if (controller != null)
        {
            return controller;
        }

        CreateZombieAnimatorController();
        AssetDatabase.Refresh();
        return AssetDatabase.LoadAssetAtPath<AnimatorController>(ZombieControllerPath);
    }

    private static void AssignSpawnerDefaultAnimatorController(SurvivalEnemySpawner spawner)
    {
        if (spawner == null)
        {
            return;
        }

        AnimatorController controller = GetOrCreateZombieAnimatorController();
        if (controller == null)
        {
            return;
        }

        if (spawner.defaultZombieAnimatorController != controller)
        {
            Undo.RecordObject(spawner, "Assign spawner zombie animator controller");
            spawner.defaultZombieAnimatorController = controller;
            SetDirtySafe(spawner);
        }
    }
}

