using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class ZombieAnimatorControllerSetup
{
    private const string ControllerPath = "Assets/ArtStore3D/Fat Zombie(Low Poly)/Anim/ZombieAnimator.controller";
    private const string IdleClipPath = "Assets/ArtStore3D/Fat Zombie(Low Poly)/Anim/ZombieIdle.anim";
    private const string WalkClipPath = "Assets/ArtStore3D/Fat Zombie(Low Poly)/Anim/ZombieWalk.anim";
    private const string AttackClipPath = "Assets/ArtStore3D/Fat Zombie(Low Poly)/Anim/ZombieAttack.anim";
    private const string ReferencePrefabPath = "Assets/ArtStore3D/Fat Zombie(Low Poly)/Prefab/FatZombie.prefab";

    [MenuItem("Survival/Setup Zombie Animator Controller")]
    public static void SetupZombieAnimator()
    {
        EnsureZombieClips();

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        if (controller == null)
        {
            Debug.LogError("Failed to create animator controller");
            return;
        }

        // Add parameters
        controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Idle", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

        // Create base layer
        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;

        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        AnimationClip walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkClipPath);
        AnimationClip attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackClipPath);

        // Create states
        AnimatorState idleState = stateMachine.AddState("Idle");
        AnimatorState moveState = stateMachine.AddState("Move");
        AnimatorState attackState = stateMachine.AddState("Attack");
        AnimatorState deathState = stateMachine.AddState("Death");

        idleState.motion = idleClip;

        // Create a simple 1D blend tree for Move using Idle and Walk clips driven by MoveSpeed
        BlendTree moveBlend = new BlendTree();
        moveBlend.name = "MoveBlend";
        moveBlend.blendType = BlendTreeType.Simple1D;
        moveBlend.blendParameter = "MoveSpeed";
        moveBlend.AddChild(idleClip, 0f);
        moveBlend.AddChild(walkClip, 1f);
        moveBlend.useAutomaticThresholds = false;

        moveState.motion = moveBlend;
        attackState.motion = attackClip;

        // Set idle as default
        stateMachine.defaultState = idleState;

        // Create transitions
        // Idle -> Move (when MoveSpeed > 0.1)
        AnimatorStateTransition idleToMove = idleState.AddTransition(moveState);
        idleToMove.AddCondition(AnimatorConditionMode.Greater, 0.1f, "MoveSpeed");
        idleToMove.exitTime = 0f;
        idleToMove.hasExitTime = false;

        // Move -> Idle (when MoveSpeed == 0)
        AnimatorStateTransition moveToIdle = moveState.AddTransition(idleState);
        moveToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "MoveSpeed");
        moveToIdle.exitTime = 0f;
        moveToIdle.hasExitTime = false;

        // Idle -> Attack (when Attack trigger)
        AnimatorStateTransition idleToAttack = idleState.AddTransition(attackState);
        idleToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        idleToAttack.exitTime = 0f;
        idleToAttack.hasExitTime = false;

        // Move -> Attack (when Attack trigger)
        AnimatorStateTransition moveToAttack = moveState.AddTransition(attackState);
        moveToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        moveToAttack.exitTime = 0f;
        moveToAttack.hasExitTime = false;

        // Attack -> Idle (after attack completes)
        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.exitTime = 0.8f;
        attackToIdle.hasExitTime = true;

        // Any -> Death (when Death trigger)
        AnimatorStateTransition anyToDeath = stateMachine.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Death");
        anyToDeath.exitTime = 0f;
        anyToDeath.hasExitTime = false;

        // Set death as exit state (no transitions from death)
        deathState.motion = null;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Zombie Animator Controller created successfully at: " + ControllerPath);
    }

    [MenuItem("Survival/Apply Animator to FatZombie Prefab")]
    public static void ApplyAnimatorToPrefab()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("ZombieAnimator.controller not found. Create it first using 'Setup Zombie Animator Controller' menu.");
            return;
        }

        int applied = 0;
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            string lower = path.ToLowerInvariant();
            if (!lower.Contains("zombie"))
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

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            applied++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Animator Controller assigned to zombie prefabs: " + applied);
    }

    [MenuItem("Survival/Setup Both (Controller + Prefab)")]
    public static void SetupBoth()
    {
        SetupZombieAnimator();
        EditorApplication.delayCall += ApplyAnimatorToPrefab;
    }

    [MenuItem("Survival/Create Zombie Walk + Attack Clips")]
    public static void CreateZombieClipsOnly()
    {
        EnsureZombieClips();
        Debug.Log("Zombie walk/attack clips generated.");
    }

    private static void EnsureZombieClips()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReferencePrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("Zombie clip generation skipped: reference prefab not found at " + ReferencePrefabPath);
            return;
        }

        GameObject temp = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (temp == null)
        {
            temp = Object.Instantiate(prefab);
        }

        Transform root = temp.transform;

        Transform hips = FindFirstByNameContains(root, new[] { "hips", "pelvis" });
        Transform spine = FindFirstByNameContains(root, new[] { "spine", "chest" });
        Transform leftArm = FindFirstByNameContains(root, new[] { "leftarm", "upperarm_l", "arm_l" });
        Transform rightArm = FindFirstByNameContains(root, new[] { "rightarm", "upperarm_r", "arm_r" });
        Transform leftLeg = FindFirstByNameContains(root, new[] { "leftupleg", "thigh_l", "upleg_l" });
        Transform rightLeg = FindFirstByNameContains(root, new[] { "rightupleg", "thigh_r", "upleg_r" });

        AnimationClip idleClip = new AnimationClip();
        idleClip.name = "ZombieIdle";
        idleClip.frameRate = 30f;
        idleClip.wrapMode = WrapMode.Loop;

        AnimationClip walkClip = new AnimationClip();
        walkClip.name = "ZombieWalk";
        walkClip.frameRate = 30f;
        walkClip.wrapMode = WrapMode.Loop;

        AnimationClip attackClip = new AnimationClip();
        attackClip.name = "ZombieAttack";
        attackClip.frameRate = 30f;
        attackClip.wrapMode = WrapMode.Once;

        BuildIdleClip(idleClip, root, hips, spine);
        BuildWalkClip(walkClip, root, hips, spine, leftArm, rightArm, leftLeg, rightLeg);
        BuildAttackClip(attackClip, root, hips, spine, leftArm, rightArm);

        SaveClipAsset(IdleClipPath, idleClip);
        SaveClipAsset(WalkClipPath, walkClip);
        SaveClipAsset(AttackClipPath, attackClip);

        Object.DestroyImmediate(temp);
    }

    private static void BuildIdleClip(AnimationClip clip, Transform root, Transform hips, Transform spine)
    {
        if (hips != null)
        {
            string p = AnimationUtility.CalculateTransformPath(hips, root);
            AddCurve(clip, p, typeof(Transform), "localPosition.y", new[]
            {
                new Keyframe(0f, hips.localPosition.y),
                new Keyframe(0.5f, hips.localPosition.y + 0.01f),
                new Keyframe(1f, hips.localPosition.y)
            });
        }

        if (spine != null)
        {
            string p = AnimationUtility.CalculateTransformPath(spine, root);
            AddCurve(clip, p, typeof(Transform), "localEulerAngles.y", new[]
            {
                new Keyframe(0f, spine.localEulerAngles.y - 1f),
                new Keyframe(0.5f, spine.localEulerAngles.y + 1f),
                new Keyframe(1f, spine.localEulerAngles.y - 1f)
            });
        }
    }

    private static void BuildWalkClip(AnimationClip clip, Transform root, Transform hips, Transform spine, Transform leftArm, Transform rightArm, Transform leftLeg, Transform rightLeg)
    {
        if (hips != null)
        {
            string p = AnimationUtility.CalculateTransformPath(hips, root);
            float y = hips.localPosition.y;
            AddCurve(clip, p, typeof(Transform), "localPosition.y", new[]
            {
                new Keyframe(0f, y),
                new Keyframe(0.25f, y + 0.04f),
                new Keyframe(0.5f, y),
                new Keyframe(0.75f, y - 0.02f),
                new Keyframe(1f, y)
            });
        }

        AnimateLimbSwing(clip, root, leftArm, -28f, true);
        AnimateLimbSwing(clip, root, rightArm, 28f, true);
        AnimateLimbSwing(clip, root, leftLeg, 24f, false);
        AnimateLimbSwing(clip, root, rightLeg, -24f, false);

        if (spine != null)
        {
            string p = AnimationUtility.CalculateTransformPath(spine, root);
            float x = spine.localEulerAngles.x;
            AddCurve(clip, p, typeof(Transform), "localEulerAngles.x", new[]
            {
                new Keyframe(0f, x + 8f),
                new Keyframe(0.5f, x + 10f),
                new Keyframe(1f, x + 8f)
            });
        }
    }

    private static void BuildAttackClip(AnimationClip clip, Transform root, Transform hips, Transform spine, Transform leftArm, Transform rightArm)
    {
        if (spine != null)
        {
            string p = AnimationUtility.CalculateTransformPath(spine, root);
            float x = spine.localEulerAngles.x;
            AddCurve(clip, p, typeof(Transform), "localEulerAngles.x", new[]
            {
                new Keyframe(0f, x + 5f),
                new Keyframe(0.18f, x - 18f),
                new Keyframe(0.45f, x + 7f),
                new Keyframe(0.7f, x + 5f)
            });
        }

        if (hips != null)
        {
            string p = AnimationUtility.CalculateTransformPath(hips, root);
            float z = hips.localPosition.z;
            AddCurve(clip, p, typeof(Transform), "localPosition.z", new[]
            {
                new Keyframe(0f, z),
                new Keyframe(0.18f, z + 0.08f),
                new Keyframe(0.45f, z),
                new Keyframe(0.7f, z)
            });
        }

        AnimateAttackArm(clip, root, leftArm, -55f);
        AnimateAttackArm(clip, root, rightArm, -42f);
    }

    private static void AnimateLimbSwing(AnimationClip clip, Transform root, Transform limb, float peak, bool arm)
    {
        if (limb == null)
        {
            return;
        }

        string p = AnimationUtility.CalculateTransformPath(limb, root);
        float x = limb.localEulerAngles.x;
        float a = arm ? 1f : 0.7f;
        AddCurve(clip, p, typeof(Transform), "localEulerAngles.x", new[]
        {
            new Keyframe(0f, x + peak * a),
            new Keyframe(0.5f, x - peak * a),
            new Keyframe(1f, x + peak * a)
        });
    }

    private static void AnimateAttackArm(AnimationClip clip, Transform root, Transform limb, float peak)
    {
        if (limb == null)
        {
            return;
        }

        string p = AnimationUtility.CalculateTransformPath(limb, root);
        float x = limb.localEulerAngles.x;
        AddCurve(clip, p, typeof(Transform), "localEulerAngles.x", new[]
        {
            new Keyframe(0f, x),
            new Keyframe(0.14f, x + peak),
            new Keyframe(0.33f, x + peak * 0.25f),
            new Keyframe(0.7f, x)
        });
    }

    private static void AddCurve(AnimationClip clip, string path, System.Type type, string propertyName, Keyframe[] keys)
    {
        AnimationCurve curve = new AnimationCurve(keys);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, propertyName), curve);
    }

    private static void SaveClipAsset(string path, AnimationClip clip)
    {
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(clip, path);
            return;
        }

        EditorUtility.CopySerialized(clip, existing);
        EditorUtility.SetDirty(existing);
    }

    private static Transform FindFirstByNameContains(Transform root, string[] contains)
    {
        if (root == null)
        {
            return null;
        }

        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();
            string lower = current.name.ToLowerInvariant().Replace(" ", string.Empty);
            for (int i = 0; i < contains.Length; i++)
            {
                if (lower.Contains(contains[i]))
                {
                    return current;
                }
            }

            for (int i = 0; i < current.childCount; i++)
            {
                queue.Enqueue(current.GetChild(i));
            }
        }

        return null;
    }
}
