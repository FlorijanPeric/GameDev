using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class MeshyAIMutantAnimatorControllerSetup
{
    private const string AssetFolderPath = "Assets/Enemies/Meshy_AI_mutant_enemy_that_att_0512070420_texture_obj";
    private const string ControllerPath = "Assets/Enemies/Meshy_AI_mutant_enemy_that_att_0512070420_texture_obj/MeshyAIMutantAnimator.controller";
    private const string PreferredPrefabNameHint = "Meshy";

    [MenuItem("Survival/Setup Meshy Mutant Animator Controller")]
    public static void SetupController()
    {
        AnimationClip fightIdle = LoadFirstClip(new[] { "fight idle", "idle" }, shouldLoop: true);
        AnimationClip run = LoadFirstClip(new[] { "run" }, shouldLoop: true);
        AnimationClip jumpAttack = LoadFirstClip(new[] { "jump attack", "jump" }, shouldLoop: true);
        AnimationClip brutalAssassination = LoadFirstClip(new[] { "brutal assassination", "assassination" }, shouldLoop: false);

        if (fightIdle == null || run == null || jumpAttack == null || brutalAssassination == null)
        {
            Debug.LogError("MeshyAIMutantAnimatorControllerSetup: Missing one or more animation clips in the Meshy asset folder.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("MeshyAIMutantAnimatorControllerSetup: Failed to create animator controller.");
            return;
        }

        controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        AnimatorState locomotionState = stateMachine.AddState("Locomotion");
        locomotionState.motion = CreateLocomotionBlendTree(fightIdle, run);
        stateMachine.defaultState = locomotionState;
        // Slightly speed up locomotion for snappier movement
        locomotionState.speed = 1.08f;

        AnimatorState attackState = stateMachine.AddState("Attack");
        attackState.motion = jumpAttack;
        // Play attack a bit faster
        attackState.speed = 1.12f;

        AnimatorState deathState = stateMachine.AddState("Death");
        deathState.motion = brutalAssassination;
        // Keep death at normal speed
        deathState.speed = 1f;

        AnimatorStateTransition toAttack = locomotionState.AddTransition(attackState);
        toAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
        toAttack.hasExitTime = false;
        toAttack.exitTime = 0f;

        AnimatorStateTransition attackToLocomotion = attackState.AddTransition(locomotionState);
        attackToLocomotion.hasExitTime = true;
        attackToLocomotion.exitTime = 0.9f;

        AnimatorStateTransition anyToDeath = stateMachine.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0f, "Death");
        anyToDeath.hasExitTime = false;
        anyToDeath.exitTime = 0f;

        AttachAnimationEvents(fightIdle, new[] { new AnimationEventSpec(0.25f, "OnFootstep"), new AnimationEventSpec(0.75f, "OnFootstep") });
        AttachAnimationEvents(run, new[] { new AnimationEventSpec(0.2f, "OnFootstep"), new AnimationEventSpec(0.65f, "OnFootstep") });
        AttachAnimationEvents(jumpAttack, new[] { new AnimationEventSpec(0.45f, "OnAttackImpact") });
        AttachAnimationEvents(brutalAssassination, new[] { new AnimationEventSpec(0.9f, "OnDeathAnimationEvent") });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ApplyToMatchingPrefabIfFound(controller);
        Debug.Log("Meshy mutant animator controller created at: " + ControllerPath);
    }

    [MenuItem("Survival/Setup Meshy Mutant Animator Controller + Apply")]
    public static void SetupBoth()
    {
        SetupController();
    }

    private static BlendTree CreateLocomotionBlendTree(AnimationClip idle, AnimationClip run)
    {
        BlendTree tree = new BlendTree
        {
            name = "MeshyLocomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "MoveSpeed",
            useAutomaticThresholds = false
        };

        tree.AddChild(idle, 0f);
        tree.AddChild(run, 1f);
        return tree;
    }

    private static AnimationClip LoadFirstClip(IEnumerable<string> nameHints, bool shouldLoop = true)
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { AssetFolderPath });
        foreach (string hint in nameHints)
        {
            string hintLower = hint.ToLowerInvariant();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.ToLowerInvariant().Contains(hintLower))
                {
                    continue;
                }

                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int a = 0; a < assets.Length; a++)
                {
                    if (assets[a] is AnimationClip clip)
                    {
                        if (clip.name.ToLowerInvariant().Contains("__preview__"))
                        {
                            continue;
                        }

                        if (shouldLoop)
                        {
                            clip.wrapMode = WrapMode.Loop;
                        }

                        return clip;
                    }
                }
            }
        }

        return null;
    }

    private static void AttachAnimationEvents(AnimationClip clip, AnimationEventSpec[] eventsToAdd)
    {
        if (clip == null || eventsToAdd == null || eventsToAdd.Length == 0)
        {
            return;
        }

        List<AnimationEvent> events = new List<AnimationEvent>();
        for (int i = 0; i < eventsToAdd.Length; i++)
        {
            AnimationEventSpec spec = eventsToAdd[i];
            events.Add(new AnimationEvent
            {
                functionName = spec.FunctionName,
                time = Mathf.Clamp01(spec.NormalizedTime) * Mathf.Max(0.01f, clip.length)
            });
        }

        AnimationUtility.SetAnimationEvents(clip, events.ToArray());
        EditorUtility.SetDirty(clip);
    }

    private static void ApplyToMatchingPrefabIfFound(AnimatorController controller)
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { AssetFolderPath });
        GameObject chosenPrefab = null;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            if (!path.ToLowerInvariant().Contains(PreferredPrefabNameHint.ToLowerInvariant()))
            {
                continue;
            }

            chosenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (chosenPrefab != null)
            {
                break;
            }
        }

        if (chosenPrefab == null)
        {
            CreatePrefabFromModelAsset(controller);
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(chosenPrefab));
        if (root == null)
        {
            return;
        }

        Animator animator = root.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            animator = root.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        // Extract and assign Avatar from the FBX model
        string[] modelGuids = AssetDatabase.FindAssets("t:GameObject", new[] { AssetFolderPath });
        if (modelGuids != null && modelGuids.Length > 0)
        {
            for (int i = 0; i < modelGuids.Length; i++)
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(modelGuids[i]);
                string lower = modelPath.ToLowerInvariant();
                if (!lower.EndsWith(".fbx") && !lower.EndsWith(".obj"))
                {
                    continue;
                }

                Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(modelPath);
                if (avatar != null)
                {
                    animator.avatar = avatar;
                    break;
                }
            }
        }

        PrefabUtility.SaveAsPrefabAsset(root, AssetDatabase.GetAssetPath(chosenPrefab));
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void CreatePrefabFromModelAsset(AnimatorController controller)
    {
        string[] modelGuids = AssetDatabase.FindAssets("t:GameObject", new[] { AssetFolderPath });
        if (modelGuids == null || modelGuids.Length == 0)
        {
            return;
        }

        GameObject modelAsset = null;
        string modelPath = null;

        for (int i = 0; i < modelGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(modelGuids[i]);
            string lower = path.ToLowerInvariant();
            if (!lower.EndsWith(".fbx") && !lower.EndsWith(".obj"))
            {
                continue;
            }

            if (!lower.Contains("meshy") && !lower.Contains("mutant"))
            {
                continue;
            }

            GameObject candidate = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (candidate == null)
            {
                continue;
            }

            modelAsset = candidate;
            modelPath = path;
            break;
        }

        if (modelAsset == null || string.IsNullOrEmpty(modelPath))
        {
            return;
        }

        GameObject instance = Object.Instantiate(modelAsset);
        instance.name = "MeshyAIMutant";

        Animator animator = instance.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            animator = instance.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        // Extract and assign Avatar from the FBX model
        Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(modelPath);
        if (avatar != null)
        {
            animator.avatar = avatar;
        }

        string prefabPath = AssetFolderPath + "/MeshyAIMutant.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
        Debug.Log("Meshy prefab created at: " + prefabPath + " from model asset: " + modelPath + (avatar != null ? " (Avatar assigned)" : " (WARNING: No Avatar found)"));
    }

    private readonly struct AnimationEventSpec
    {
        public readonly float NormalizedTime;
        public readonly string FunctionName;

        public AnimationEventSpec(float normalizedTime, string functionName)
        {
            NormalizedTime = normalizedTime;
            FunctionName = functionName;
        }
    }
}
