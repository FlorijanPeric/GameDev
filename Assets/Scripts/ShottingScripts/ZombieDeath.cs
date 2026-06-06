using System.Collections;
using UnityEngine;

public class ZombieDeath : MonoBehaviour
{
    [Header("Death")]
    public float ragdollDelay = 0.1f;
    public bool disableAnimatorOnDeath = true;
    public bool enableRagdollOnDeath = true;
    public bool dropToGroundOnDeath = true;
    public float destroyAfterSeconds = 2f;

    private EnemyHealth enemyHealth;
    private Animator animator;
    private Rigidbody rootRigidbody;
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private bool isDead;
    private Coroutine delayedDisableRoutine;
    private bool deathEventReceived;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            enemyHealth = GetComponentInChildren<EnemyHealth>();
        }

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        rootRigidbody = GetComponent<Rigidbody>();
        if (rootRigidbody == null)
        {
            // If root rigidbody isn't on the root, try to find a child rigidbody to use as the main body.
            rootRigidbody = GetComponentInChildren<Rigidbody>();
        }
        navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    private void Start()
    {
        if (enemyHealth != null)
        {
            // Subscribe to death (check isDead flag in Update loop)
        }
    }

    private void Update()
    {
        if (!isDead && enemyHealth != null && enemyHealth.IsDead)
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        isDead = true;

        if (animator != null)
        {
            if (HasAnimatorParam("DeathVariant", AnimatorControllerParameterType.Float))
            {
                animator.SetFloat("DeathVariant", Random.Range(0, 4));
            }

            if (HasAnimatorParam("Death", AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger("Death");
            }
        }

        ZombieChaseAI chaseAI = GetComponent<ZombieChaseAI>();
        if (chaseAI != null)
        {
            chaseAI.enabled = false;
        }

        ZombieChaseAI[] childAis = GetComponentsInChildren<ZombieChaseAI>(true);
        for (int i = 0; i < childAis.Length; i++)
        {
            if (childAis[i] != null)
            {
                childAis[i].enabled = false;
            }
        }

        ZombieProceduralAnimation procedural = GetComponent<ZombieProceduralAnimation>();
        if (procedural == null)
        {
            procedural = GetComponentInChildren<ZombieProceduralAnimation>();
        }

        if (procedural != null)
        {
            procedural.enabled = false;
        }

        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = false;
        }

        UnityEngine.AI.NavMeshAgent[] childAgents = GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true);
        for (int i = 0; i < childAgents.Length; i++)
        {
            if (childAgents[i] != null)
            {
                childAgents[i].enabled = false;
            }
        }

        if (animator != null && disableAnimatorOnDeath)
        {
            if (delayedDisableRoutine != null)
            {
                StopCoroutine(delayedDisableRoutine);
            }

            delayedDisableRoutine = StartCoroutine(DisableAnimatorAfterDelay(Mathf.Clamp(destroyAfterSeconds > 0f ? destroyAfterSeconds * 0.75f : 0.75f, 0.25f, 1.5f)));
        }

        if (enableRagdollOnDeath)
        {
            EnableRagdoll();
        }
        else if (dropToGroundOnDeath && rootRigidbody != null)
        {
            rootRigidbody.isKinematic = false;
            rootRigidbody.useGravity = true;
        }

        // Ensure destruction happens
        if (destroyAfterSeconds > 0f)
        {
            Destroy(gameObject, destroyAfterSeconds);
        }
        else
        {
            // Fallback: destroy immediately if no delay specified
            Destroy(gameObject);
        }
    }

    public void TriggerDeath()
    {
        if (!isDead)
        {
            HandleDeath();
        }
    }

    public void OnDeathAnimationEvent()
    {
        deathEventReceived = true;

        if (animator != null && disableAnimatorOnDeath)
        {
            animator.enabled = false;
        }
    }

    private void EnableRagdoll()
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rb = rigidbodies[i];
            if (rb.CompareTag("Ragdoll") || rb != GetComponent<Rigidbody>())
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].CompareTag("Ragdoll") && colliders[i] == GetComponent<Collider>())
            {
                colliders[i].enabled = false;
            }
            else if (colliders[i].CompareTag("Ragdoll"))
            {
                colliders[i].enabled = true;
            }
        }
    }

    private IEnumerator DisableAnimatorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator != null)
        {
            animator.enabled = false;
        }
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
}
