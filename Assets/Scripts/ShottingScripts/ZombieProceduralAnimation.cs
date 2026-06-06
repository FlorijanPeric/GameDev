using UnityEngine;
using UnityEngine.AI;

public class ZombieProceduralAnimation : MonoBehaviour
{
    [Header("References")]
    public Transform visualRoot;
    public ZombieChaseAI chaseAI;

    [Header("Motion")]
    public float moveLerpSpeed = 8f;
    public float bobAmplitude = 0.045f;
    public float bobFrequency = 7f;
    public float swayAngle = 5f;

    [Header("Attack")]
    public float attackLungeDistance = 0.08f;
    public float attackLungeSpeed = 10f;
    public float attackPulseCooldown = 0.8f;

    private NavMeshAgent agent;
    private Vector3 baseLocalPos;
    private Quaternion baseLocalRot;
    private float move01;
    private float nextPulseTime;
    private float pulseWeight;
    private Vector3 lastWorldPosition;

    private void Awake()
    {
        if (visualRoot == null)
        {
            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                visualRoot = animator.transform;
            }
            else
            {
                visualRoot = transform;
            }
        }

        if (chaseAI == null)
        {
            chaseAI = GetComponent<ZombieChaseAI>();
        }

        agent = GetComponent<NavMeshAgent>();
        baseLocalPos = visualRoot.localPosition;
        baseLocalRot = visualRoot.localRotation;
        lastWorldPosition = transform.position;
    }

    private void Update()
    {
        if (visualRoot == null)
        {
            return;
        }

        float speed = 0f;
        float maxSpeed = 1f;
        if (agent != null && agent.enabled)
        {
            speed = agent.velocity.magnitude;
            maxSpeed = Mathf.Max(0.01f, agent.speed);
        }

        float actualMoveSpeed = Vector3.Distance(transform.position, lastWorldPosition) / Mathf.Max(0.0001f, Time.deltaTime);
        lastWorldPosition = transform.position;
        speed = Mathf.Max(speed, actualMoveSpeed);

        float targetMove = Mathf.Clamp01(speed / maxSpeed);
        move01 = Mathf.Lerp(move01, targetMove, Time.deltaTime * moveLerpSpeed);

        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude * move01;
        float sway = Mathf.Sin(Time.time * bobFrequency * 0.6f) * swayAngle * move01;

        if (chaseAI != null && chaseAI.target != null)
        {
            float distance = Vector3.Distance(transform.position, chaseAI.target.position);
            if (distance <= chaseAI.attackRange + 0.2f && Time.time >= nextPulseTime)
            {
                nextPulseTime = Time.time + Mathf.Max(0.2f, attackPulseCooldown);
                pulseWeight = 1f;
            }
        }

        pulseWeight = Mathf.MoveTowards(pulseWeight, 0f, Time.deltaTime * attackLungeSpeed);
        float lunge = pulseWeight * attackLungeDistance;

        visualRoot.localPosition = baseLocalPos + new Vector3(0f, bob, lunge);
        visualRoot.localRotation = baseLocalRot * Quaternion.Euler(0f, sway, 0f);
    }
}
