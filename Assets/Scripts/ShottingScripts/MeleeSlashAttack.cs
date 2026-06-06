using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MeleeSlashAttack : MonoBehaviour
{
    [Header("Slash")]
    public float slashCooldown = 0.5f;
    public float slashDuration = 0.16f;
    public float damage = 50f;

    [Header("Hit Detection")]
    public Transform slashOrigin;
    public float slashRange = 2f;
    public float slashRadius = 0.65f;
    public LayerMask hitMask = ~0;

    [Header("Animation")]
    public Transform weaponVisual;
    public Vector3 idleEuler = Vector3.zero;
    public Vector3 windupEuler = new Vector3(-20f, -35f, 15f);
    public Vector3 slashEuler = new Vector3(18f, 38f, -12f);

    private PlayerInput playerInput;
    private InputAction attackAction;
    private float nextSlashTime;
    private bool isSlashing;

    public event System.Action SlashPerformed;

    void Awake()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            attackAction = playerInput.actions.FindAction("Attack", false);
        }

        if (weaponVisual == null)
        {
            weaponVisual = transform;
        }

        if (slashOrigin == null)
        {
            slashOrigin = playerInput != null && playerInput.camera != null
                ? playerInput.camera.transform
                : Camera.main != null ? Camera.main.transform : transform;
        }

        Weapon weapon = GetComponentInParent<Weapon>();
        if (weapon != null)
        {
            damage = Mathf.Max(1f, weapon.meleeDamage);
        }

        weaponVisual.localRotation = Quaternion.Euler(idleEuler);
    }

    void OnEnable()
    {
        attackAction?.Enable();
    }

    void OnDisable()
    {
        attackAction?.Disable();
    }

    void Update()
    {
        if (isSlashing || Time.time < nextSlashTime)
        {
            return;
        }

        bool wantsSlash = attackAction != null
            ? attackAction.WasPressedThisFrame()
            : Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (wantsSlash)
        {
            StartCoroutine(SlashRoutine());
        }
    }

    private IEnumerator SlashRoutine()
    {
        isSlashing = true;
        nextSlashTime = Time.time + slashCooldown;

        float halfDuration = Mathf.Max(0.01f, slashDuration * 0.5f);
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            weaponVisual.localRotation = Quaternion.Euler(Vector3.Lerp(idleEuler, windupEuler, t));
            yield return null;
        }

        DoSlashDamage();
        SlashPerformed?.Invoke();

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            weaponVisual.localRotation = Quaternion.Euler(Vector3.Lerp(windupEuler, slashEuler, t));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            weaponVisual.localRotation = Quaternion.Euler(Vector3.Lerp(slashEuler, idleEuler, t));
            yield return null;
        }

        weaponVisual.localRotation = Quaternion.Euler(idleEuler);
        isSlashing = false;
    }

    private void DoSlashDamage()
    {
        Transform originTransform = slashOrigin != null ? slashOrigin : transform;
        Vector3 center = originTransform.position + originTransform.forward * slashRange;
        Collider[] hits = Physics.OverlapSphere(center, slashRadius, hitMask, QueryTriggerInteraction.Ignore);
        float appliedDamage = ResolveSlashDamage();

        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable;
            if (hits[i].TryGetComponent<IDamageable>(out damageable) || hits[i].GetComponentInParent<IDamageable>() != null)
            {
                if (damageable == null)
                {
                    damageable = hits[i].GetComponentInParent<IDamageable>();
                }

                damageable?.TakeDamage(appliedDamage);
            }
        }
    }

    private float ResolveSlashDamage()
    {
        Weapon weapon = GetComponentInParent<Weapon>();
        if (weapon != null)
        {
            return Mathf.Max(1f, weapon.meleeDamage);
        }

        return Mathf.Max(1f, damage);
    }
}
