using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class ZombieChaseAI : MonoBehaviour
{
    public enum ZombieArchetype
    {
        Walker,
        Runner,
        Tank
    }

    [Header("Target")]
    public Transform target;
    public string playerTag = "Player";

    [Header("Movement")]
    public ZombieArchetype archetype = ZombieArchetype.Walker;
    public float moveSpeed = 3.4f;
    public float turnSpeed = 7f;
    public float repathInterval = 0.2f;

    [Header("Combat")]
    public float attackRange = 1.8f;
    public float attackDamage = 10f;
    public float attackCooldown = 1f;

    [Header("Animation")]
    public Animator animator;
    public string moveSpeedParam = "MoveSpeed";
    public string isMovingParam = "IsMoving";
    public string attackTriggerParam = "Attack";
    public string attackVariantParam = "AttackVariant";
    public string idleStateParam = "Idle";
    public float animatorSpeedScale = 0.35f;
    public float animationBlendSpeed = 8f;

    [Header("Grounding")]
    public LayerMask groundMask = ~0;
    public float groundProbeHeight = 2.2f;
    public float groundProbeDistance = 40f;
    public float groundOffset = 0.02f;
    public float groundSnapInterval = 0.2f;

    [Header("Height Lock")]
    public bool keepSameHeightAsTarget = false;
    public float heightMatchLerpSpeed = 12f;

    private NavMeshAgent agent;
    private EnemyHealth enemyHealth;
    private float nextRepathTime;
    private float nextAttackTime;
    private float nextGroundSnapTime;
    private float baseMoveSpeed;
    private float lastAnimationSpeed = 0f;
    private float externalSpeedScale = 1f;
    private float externalDamageScale = 1f;
    private bool avoidanceConfigured = false;
    private EnemyAudioPlayer audioPlayer;
    private float nextFootstepTime = 0f;
    private float footstepInterval = 0.5f;
    private bool attackEventPending;
    private bool attackEventConsumed;
    private float attackEventDeadline;
    private bool hasMoveSpeedParam;
    private bool hasIsMovingParam;
    private bool hasAttackTriggerParam;
    private bool hasAttackVariantParam;
    private bool hasIdleStateParam;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        ResolveAnimatorParameterNames();
        CacheAnimatorParams();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        baseMoveSpeed = moveSpeed;
        audioPlayer = GetComponent<EnemyAudioPlayer>();
    }

    private void Start()
    {
        ResolveTarget();
        ApplyArchetypeDefaults();
        ApplyAgentSettings();
        SnapToGround(true);
    }

    private void Update()
    {
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            StopMovement();
            return;
        }

        if (attackEventPending && !attackEventConsumed && Time.time >= attackEventDeadline)
        {
            ResolveAttackDamage();
        }

        if (Time.time >= nextGroundSnapTime)
        {
            nextGroundSnapTime = Time.time + Mathf.Max(0.02f, groundSnapInterval);
            SnapToGround(false);
        }

        if (target == null)
        {
            ResolveTarget();
            if (target == null)
            {
                UpdateAnimatorMovement(0f);
                return;
            }
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= attackRange)
        {
            StopMovement();
            FaceTarget();
            TryAttack();
            return;
        }

        MoveTowardsTarget();
    }

    public void ApplyWaveScaling(int wave)
    {
        int safeWave = Mathf.Max(1, wave);
        float speedScale = 1f + (safeWave - 1) * 0.02f;
        moveSpeed = baseMoveSpeed * speedScale;

        ApplyAgentSettings();
    }

    public void SetCombatTuning(float speedScale, float damageScale)
    {
        externalSpeedScale = Mathf.Max(0.25f, speedScale);
        externalDamageScale = Mathf.Max(0.25f, damageScale);
        ApplyAgentSettings();
    }

    public void ForceGroundSnap()
    {
        SnapToGround(true);
    }

    public void ConfigureAnimator(Animator newAnimator)
    {
        animator = newAnimator;
        if (animator != null)
        {
            animator.enabled = true;
        }
        ResolveAnimatorParameterNames();
        CacheAnimatorParams();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    private void ResolveTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            target = playerObj.transform;
            return;
        }

        PlayerSurvivalHealth survivalHealth = FindObjectOfType<PlayerSurvivalHealth>();
        if (survivalHealth != null)
        {
            target = survivalHealth.transform;
            return;
        }

        FPSMovement movement = FindObjectOfType<FPSMovement>();
        if (movement != null)
        {
            target = movement.transform;
            return;
        }

        PlayerInput input = FindObjectOfType<PlayerInput>();
        if (input != null)
        {
            target = input.transform;
            return;
        }

        if (Camera.main != null)
        {
            target = Camera.main.transform;
        }
    }

    private void ApplyArchetypeDefaults()
    {
        switch (archetype)
        {
            case ZombieArchetype.Walker:
                moveSpeed = 3.4f;
                attackDamage = 10f;
                attackCooldown = 0.85f;
                break;
            case ZombieArchetype.Runner:
                moveSpeed = 5f;
                attackDamage = 7f;
                attackCooldown = 0.5f;
                break;
            case ZombieArchetype.Tank:
                moveSpeed = 2.4f;
                attackDamage = 16f;
                attackCooldown = 1.2f;
                break;
        }

        baseMoveSpeed = moveSpeed;
    }

    private void ApplyAgentSettings()
    {
        if (agent == null)
        {
            return;
        }

        agent.speed = moveSpeed;
        agent.speed *= externalSpeedScale;
        agent.acceleration = Mathf.Max(6f, moveSpeed * 4f);
        agent.angularSpeed = 720f;
        agent.stoppingDistance = Mathf.Max(0.5f, attackRange - 0.2f);
        agent.baseOffset = 0f;
        // Improve obstacle avoidance to reduce stacking and stuck behaviour
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.radius = Mathf.Max(0.35f, agent.radius);
        if (!avoidanceConfigured)
        {
            agent.avoidancePriority = Random.Range(20, 80);
            avoidanceConfigured = true;
        }
    }

    private void MoveTowardsTarget()
    {
        if (target == null)
        {
            return;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (Time.time >= nextRepathTime)
            {
                nextRepathTime = Time.time + Mathf.Max(0.05f, repathInterval);
                agent.SetDestination(target.position);
            }

            float speed01 = agent.speed > 0.01f ? Mathf.Clamp01(agent.velocity.magnitude / agent.speed) : 0f;
            UpdateAnimatorMovement(speed01);

                // Footstep SFX when moving
                if (audioPlayer != null && Time.time >= nextFootstepTime && speed01 > 0.2f)
                {
                    nextFootstepTime = Time.time + Mathf.Max(0.15f, footstepInterval * (1f / Mathf.Max(0.01f, speed01)));
                    audioPlayer.PlayFootstep();
                }

            return;
        }

        Vector3 targetPos = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 direction = (targetPos - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        SnapToGround(false);
        FaceTarget();
        UpdateAnimatorMovement(1f);
    }

    private void StopMovement()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        UpdateAnimatorMovement(0f);
    }

    private void FaceTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPos = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 direction = (targetPos - transform.position);
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desired, Time.deltaTime * turnSpeed);
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + Mathf.Max(0.1f, attackCooldown);
        attackEventPending = true;
        attackEventConsumed = false;
        attackEventDeadline = Time.time + Mathf.Max(0.35f, attackCooldown);

        if (animator != null && hasAttackTriggerParam)
        {
            if (hasAttackVariantParam)
            {
                animator.SetFloat(attackVariantParam, Random.Range(0, 5));
            }

            animator.SetTrigger(attackTriggerParam);
        }
    }

    public void OnAttackImpact()
    {
        ResolveAttackDamage();
    }

    public void OnFootstep()
    {
        if (audioPlayer != null)
        {
            audioPlayer.PlayFootstep();
        }
    }

    private void ResolveAttackDamage()
    {
        if (!attackEventPending || attackEventConsumed)
        {
            return;
        }

        attackEventConsumed = true;
        attackEventPending = false;

        if (audioPlayer != null)
        {
            audioPlayer.PlayAttack();
        }

        if (target != null)
        {
            float finalDamage = attackDamage * externalDamageScale;
            // Uses SendMessage so this works with any player health script naming style.
            target.gameObject.SendMessage("TakeDamage", finalDamage, SendMessageOptions.DontRequireReceiver);
            target.gameObject.SendMessage("ApplyDamage", finalDamage, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void UpdateAnimatorMovement(float speed01)
    {
        if (animator == null)
        {
            return;
        }

        bool isMoving = speed01 > 0.05f;

        // Update move speed param with smooth blending
        if (hasMoveSpeedParam)
        {
            float targetSpeed = isMoving ? (speed01 / Mathf.Max(0.01f, animatorSpeedScale)) : 0f;
            lastAnimationSpeed = Mathf.Lerp(lastAnimationSpeed, targetSpeed, Time.deltaTime * animationBlendSpeed);
            animator.SetFloat(moveSpeedParam, lastAnimationSpeed);
        }

        // Update moving bool
        if (hasIsMovingParam)
        {
            animator.SetBool(isMovingParam, isMoving);
        }

        // Update idle state if param exists
        if (hasIdleStateParam)
        {
            animator.SetBool(idleStateParam, !isMoving);
        }
    }

    private void ResolveAnimatorParameterNames()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        // Try to find move speed parameter (Float).
        if (!HasAnimatorParam(moveSpeedParam, AnimatorControllerParameterType.Float))
        {
            if (HasAnimatorParam("Speed", AnimatorControllerParameterType.Float))
                moveSpeedParam = "Speed";
            else if (HasAnimatorParam("Move", AnimatorControllerParameterType.Float))
                moveSpeedParam = "Move";
            else if (HasAnimatorParam("Velocity", AnimatorControllerParameterType.Float))
                moveSpeedParam = "Velocity";
        }

        // Try to find is moving parameter (Bool).
        if (!HasAnimatorParam(isMovingParam, AnimatorControllerParameterType.Bool))
        {
            if (HasAnimatorParam("Moving", AnimatorControllerParameterType.Bool))
                isMovingParam = "Moving";
            else if (HasAnimatorParam("Walk", AnimatorControllerParameterType.Bool))
                isMovingParam = "Walk";
            else if (HasAnimatorParam("IsMoving", AnimatorControllerParameterType.Bool))
                isMovingParam = "IsMoving";
        }

        // Try to find attack trigger parameter (Trigger).
        if (!HasAnimatorParam(attackTriggerParam, AnimatorControllerParameterType.Trigger))
        {
            if (HasAnimatorParam("AttackTrigger", AnimatorControllerParameterType.Trigger))
                attackTriggerParam = "AttackTrigger";
            else if (HasAnimatorParam("Attack", AnimatorControllerParameterType.Trigger))
                attackTriggerParam = "Attack";
            else if (HasAnimatorParam("Bite", AnimatorControllerParameterType.Trigger))
                attackTriggerParam = "Bite";
        }

        if (!HasAnimatorParam(attackVariantParam, AnimatorControllerParameterType.Float))
        {
            if (HasAnimatorParam("AttackIndex", AnimatorControllerParameterType.Float))
                attackVariantParam = "AttackIndex";
            else if (HasAnimatorParam("AttackVariant", AnimatorControllerParameterType.Float))
                attackVariantParam = "AttackVariant";
        }

        // Try to find idle state parameter (Bool).
        if (!HasAnimatorParam(idleStateParam, AnimatorControllerParameterType.Bool))
        {
            if (HasAnimatorParam("IdleState", AnimatorControllerParameterType.Bool))
                idleStateParam = "IdleState";
            else if (HasAnimatorParam("Idle", AnimatorControllerParameterType.Bool))
                idleStateParam = "Idle";
        }
    }

    private void CacheAnimatorParams()
    {
        if (animator == null)
        {
            return;
        }

        hasMoveSpeedParam = HasAnimatorParam(moveSpeedParam, AnimatorControllerParameterType.Float);
        hasIsMovingParam = HasAnimatorParam(isMovingParam, AnimatorControllerParameterType.Bool);
        hasAttackTriggerParam = HasAnimatorParam(attackTriggerParam, AnimatorControllerParameterType.Trigger);
        hasAttackVariantParam = HasAnimatorParam(attackVariantParam, AnimatorControllerParameterType.Float);
        hasIdleStateParam = HasAnimatorParam(idleStateParam, AnimatorControllerParameterType.Bool);
    }

    private bool HasAnimatorParam(string paramName, AnimatorControllerParameterType expectedType)
    {
        if (animator == null || string.IsNullOrEmpty(paramName))
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == paramName && parameters[i].type == expectedType)
            {
                return true;
            }
        }

        return false;
    }

    private void SnapToGround(bool force)
    {
        if (!force && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            return;
        }

        Vector3 currentPos = transform.position;
        Vector3 rayStart = currentPos + Vector3.up * Mathf.Max(0.4f, groundProbeHeight);

        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, Mathf.Max(groundProbeDistance, groundProbeHeight) + 1f, groundMask, QueryTriggerInteraction.Ignore);
        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null || IsSelfCollider(hit.collider))
                {
                    continue;
                }

                currentPos.y = hit.point.y + groundOffset;
                transform.position = currentPos;
                return;
            }
        }

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(transform.position, out navHit, 3f, NavMesh.AllAreas))
        {
            currentPos.y = navHit.position.y + groundOffset;
            transform.position = currentPos;
        }
    }

    private bool IsSelfCollider(Collider collider)
    {
        if (collider == null)
        {
            return true;
        }

        Transform root = transform.root;
        return collider.transform == transform || collider.transform.IsChildOf(transform) || collider.transform == root || collider.transform.IsChildOf(root);
    }
}
