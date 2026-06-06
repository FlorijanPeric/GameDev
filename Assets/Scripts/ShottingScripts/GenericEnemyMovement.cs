using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Generic movement AI for any enemy that doesn't have ZombieChaseAI.
/// Handles NavMeshAgent pathfinding and ground snapping for proper ground-level movement.
/// </summary>
public class GenericEnemyMovement : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public string playerTag = "Player";

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float attackRange = 2f;
    public float repathInterval = 0.3f;

    [Header("Grounding")]
    public LayerMask groundMask = ~0;
    public float groundProbeHeight = 2f;
    public float groundProbeDistance = 5f;
    public float groundOffset = 0.02f;
    public float groundSnapInterval = 0.15f;

    private NavMeshAgent agent;
    private float nextRepathTime;
    private float nextGroundSnapTime;
    private bool initialized;

    private void OnEnable()
    {
        if (!initialized)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        agent.speed = moveSpeed;
        agent.acceleration = moveSpeed * 3f;
        agent.stoppingDistance = Mathf.Max(0.3f, attackRange - 0.3f);
        agent.radius = Mathf.Max(0.35f, agent.radius);
        agent.avoidancePriority = Random.Range(20, 80);

        // Ensure rigidbody is kinematic
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        initialized = true;
        ResolveTarget();
    }

    private void Update()
    {
        if (!initialized || agent == null || !agent.enabled)
            return;

        // Update target if needed
        if (target == null)
        {
            ResolveTarget();
        }

        // Move towards target
        if (target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);

            if (Time.time >= nextRepathTime)
            {
                nextRepathTime = Time.time + repathInterval;
                if (agent.isOnNavMesh)
                {
                    agent.SetDestination(target.position);
                }
            }

            // Face target
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            if (dirToTarget.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(dirToTarget), Time.deltaTime * 5f);
            }
        }

        // Ground snap
        if (Time.time >= nextGroundSnapTime)
        {
            nextGroundSnapTime = Time.time + groundSnapInterval;
            SnapToGround();
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

        if (Camera.main != null)
        {
            target = Camera.main.transform;
        }
    }

    private void SnapToGround()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        Vector3 agentPos = agent.transform.position;
        Vector3 probeStart = agentPos + Vector3.up * groundProbeHeight;

        RaycastHit hit;
        if (Physics.Raycast(probeStart, Vector3.down, out hit, groundProbeDistance + groundProbeHeight, ~0, QueryTriggerInteraction.Ignore))
        {
            float targetY = hit.point.y + groundOffset;
            if (Mathf.Abs(agentPos.y - targetY) > 0.05f)
            {
                agentPos.y = targetY;
                agent.transform.position = agentPos;
            }
        }
    }

    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
        if (agent != null && agent.enabled)
        {
            agent.speed = moveSpeed;
        }
    }
}
