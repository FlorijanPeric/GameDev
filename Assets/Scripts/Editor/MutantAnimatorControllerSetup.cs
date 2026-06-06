using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class MutantAnimatorControllerSetup
{
    private const string ControllerPath = "Assets/Enemies/MonsterMutant 7/MonsterMutant7 Animator Controller.controller";
    private const string PrefabFolderPath = "Assets/Enemies/MonsterMutant 7/Prefab";
    private const string AnimFolderPath = "Assets/Enemies/MonsterMutant 7/Animations";

    [MenuItem("Survival/Setup Mutant Animator Controller")]
    public static void SetupMutantAnimator()
    {
        AnimationClip[] idleClips = LoadClips(new[] { "idle1", "idle2", "idle3", "idle4" }, shouldLoop: true);
        AnimationClip[] walkClips = LoadClips(new[] { "walk2", "walk3", "walk4" }, shouldLoop: true);
        AnimationClip[] runClips = LoadClips(new[] { "run1", "run2", "run3" }, shouldLoop: true);
        AnimationClip[] attackClips = LoadClips(new[] { "attack1", "attack2", "attack3", "attack4", "attack5" }, shouldLoop: true);
        AnimationClip[] deathClips = LoadClips(new[] { "death1", "death2", "death3", "death4" }, shouldLoop: false);

        AddAnimationEvents(walkClips, new[] { new ClipEventSpec(0.25f, "OnFootstep"), new ClipEventSpec(0.75f, "OnFootstep") });
        AddAnimationEvents(runClips, new[] { new ClipEventSpec(0.2f, "OnFootstep"), new ClipEventSpec(0.65f, "OnFootstep") });
        AddAnimationEvents(attackClips, new[] { new ClipEventSpec(0.45f, "OnAttackImpact") });
        AddAnimationEvents(deathClips, new[] { new ClipEventSpec(0.9f, "OnDeathAnimationEvent") });

        if (idleClips.Length == 0 || walkClips.Length == 0 || runClips.Length == 0)
        {
            Debug.LogError("MutantAnimatorControllerSetup: Missing locomotion clips in Animations folder.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("MutantAnimatorControllerSetup: Failed to create animator controller.");
            return;
        }

        controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackVariant", AnimatorControllerParameterType.Float);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("DeathVariant", AnimatorControllerParameterType.Float);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        AnimatorState locomotionState = stateMachine.AddState("Locomotion");
        locomotionState.motion = CreateLocomotionBlendTree(idleClips, walkClips, runClips);
        stateMachine.defaultState = locomotionState;
        // Slightly speed up locomotion for snappier movement
        locomotionState.speed = 1.08f;

        AnimatorState attackState = stateMachine.AddState("Attack");
        attackState.motion = CreateVariantBlendTree("AttackBlend", "AttackVariant", attackClips);
        // Make attacks play a bit faster
        attackState.speed = 1.12f;

        AnimatorState deathState = stateMachine.AddState("Death");
        deathState.motion = CreateVariantBlendTree("DeathBlend", "DeathVariant", deathClips);
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

        if (attackClips.Length == 0)
        {
            Debug.LogWarning("MutantAnimatorControllerSetup: No attack clips found. Attack state will be empty.");
        }

        if (deathClips.Length == 0)
        {
            Debug.LogWarning("MutantAnimatorControllerSetup: No death clips found. Death state will be empty.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Mutant animator controller created at: " + ControllerPath);
    }

    [MenuItem("Survival/Apply Animator to Mutant Prefabs")]
    public static void ApplyToMutantPrefabs()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("Mutant animator controller not found. Create it first.");
            return;
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolderPath });
        int applied = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                continue;
            }

            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                SkinnedMeshRenderer mesh = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (mesh != null)
                {
                    animator = mesh.gameObject.AddComponent<Animator>();
                }
                else
                {
                    animator = root.AddComponent<Animator>();
                }
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            // Extract and assign Avatar from the model asset
            string[] modelGuids = AssetDatabase.FindAssets("t:GameObject", new[] { AnimFolderPath });
            if (modelGuids != null && modelGuids.Length > 0)
            {
                for (int m = 0; m < modelGuids.Length; m++)
                {
                    string modelPath = AssetDatabase.GUIDToAssetPath(modelGuids[m]);
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

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            applied++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Applied mutant animator controller to {applied} prefab(s).");
    }

    [MenuItem("Survival/Setup Mutant Animator Controller + Apply")]
    public static void SetupBoth()
    {
        SetupMutantAnimator();
        EditorApplication.delayCall += ApplyToMutantPrefabs;
    }

    private static AnimationClip[] LoadClips(IEnumerable<string> nameHints, bool shouldLoop = true)
    {
        List<AnimationClip> clips = new List<AnimationClip>();
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { AnimFolderPath });

        foreach (string hint in nameHints)
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.ToLowerInvariant().Contains(hint.ToLowerInvariant()))
                {
                    continue;
                }

                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int a = 0; a < allAssets.Length; a++)
                {
                    if (allAssets[a] is AnimationClip clip)
                    {
                        string clipName = clip.name.ToLowerInvariant();
                        if (clipName.Contains("__preview__"))
                        {
                            continue;
                        }

                        if (shouldLoop)
                        {
                            clip.wrapMode = WrapMode.Loop;
                        }

                        clips.Add(clip);
                        goto NextHint;
                    }
                }
            }

        NextHint:
            continue;
        }

        return clips.ToArray();
    }

    private static BlendTree CreateLocomotionBlendTree(AnimationClip[] idleClips, AnimationClip[] walkClips, AnimationClip[] runClips)
    {
        BlendTree tree = new BlendTree
        {
            name = "MutantLocomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "MoveSpeed",
            useAutomaticThresholds = false
        };

        if (idleClips.Length > 0)
        {
            tree.AddChild(idleClips[0], 0f);
        }

        if (walkClips.Length > 0)
        {
            tree.AddChild(walkClips[0], 0.5f);
        }

        if (runClips.Length > 0)
        {
            tree.AddChild(runClips[0], 1f);
        }

        return tree;
    }

    private static BlendTree CreateVariantBlendTree(string treeName, string parameterName, AnimationClip[] clips)
    {
        BlendTree tree = new BlendTree
        {
            name = treeName,
            blendType = BlendTreeType.Simple1D,
            blendParameter = parameterName,
            useAutomaticThresholds = false
        };

        if (clips.Length == 0)
        {
            return tree;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            float threshold = clips.Length == 1 ? 0f : i;
            tree.AddChild(clips[i], threshold);
        }

        return tree;
    }

    private static void AddAnimationEvents(AnimationClip[] clips, ClipEventSpec[] eventsToAdd)
    {
        if (clips == null || clips.Length == 0 || eventsToAdd == null || eventsToAdd.Length == 0)
        {
            return;
        }

        foreach (AnimationClip clip in clips)
        {
            if (clip == null)
            {
                continue;
            }

            List<AnimationEvent> events = new List<AnimationEvent>();
            foreach (ClipEventSpec spec in eventsToAdd)
            {
                AnimationEvent evt = new AnimationEvent
                {
                    functionName = spec.FunctionName,
                    time = Mathf.Clamp01(spec.NormalizedTime) * Mathf.Max(0.01f, clip.length)
                };
                events.Add(evt);
            }

            AnimationUtility.SetAnimationEvents(clip, events.ToArray());
            EditorUtility.SetDirty(clip);
        }
    }

    private readonly struct ClipEventSpec
    {
        public readonly float NormalizedTime;
        public readonly string FunctionName;

        public ClipEventSpec(float normalizedTime, string functionName)
        {
            NormalizedTime = normalizedTime;
            FunctionName = functionName;
        }
    }
}
