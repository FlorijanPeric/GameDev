using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class GunShootTracer : MonoBehaviour
{
    [Header("Gun Settings")]
    public float damage = 35f;
    public float damageMultiplier = 1f;
    public float range = 100f;
    public float fireRate = 0.1f;
    public LayerMask hitMask = ~0;
    public bool useScreenCenterAiming = true;

    [Header("Tracer")]
    public GameObject tracerPrefab;
    public Transform firePoint;
    public Transform aimSource;

    [Header("Ammo")]
    public int magazineSize = 30;
    public int reserveAmmo = 120;
    public float reloadDuration = 1.6f;
    public bool autoReloadWhenEmpty = true;

    [Header("ADS")]
    public Transform weaponVisual;
    public bool useAdsPositioning = false;
    public Vector3 hipLocalPosition = Vector3.zero;
    public Vector3 adsLocalPosition = new Vector3(0f, -0.04f, 0.06f);
    public float adsLerpSpeed = 14f;
    public Camera adsCamera;
    public float hipFov = 70f;
    public float adsFov = 55f;
    public float fovLerpSpeed = 12f;

    [Header("UI")]
    public HitmarkerUI hitmarkerUI;

    [Header("Effects")]
    public ParticleSystem muzzleFlash;
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shotClip;
    public AudioClip reloadClip;
    [Range(0f,1f)] public float shotVolume = 1f;
    [Range(0f,1f)] public float reloadVolume = 1f;

    [Header("Debug")]
    public bool enableDebugLogs = false;
    public bool showDebugOverlay = false;
    public float shotIndicatorDuration = 0.2f;

    private float nextFireTime;
    private PlayerInput playerInput;
    private InputAction attackAction;
    private InputAction reloadAction;
    private int currentAmmo;
    private bool isReloading;
    private float shotIndicatorUntil;
    private GUIStyle overlayStyle;

    public event System.Action ShotFired;
    public event System.Action<bool> ReloadStateChanged;
    public bool IsAiming { get; private set; }

    void Awake()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            attackAction = playerInput.actions.FindAction("Attack", false);
            reloadAction = playerInput.actions.FindAction("Reload", false);
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("GunShootTracer: PlayerInput or Input Actions asset is missing. Using mouse keyboard fallback where possible.", this);
        }

        currentAmmo = Mathf.Clamp(magazineSize, 0, magazineSize);

        if (weaponVisual == null)
        {
            weaponVisual = ResolveDefaultWeaponVisual();
        }

        if (weaponVisual != null && hipLocalPosition == Vector3.zero)
        {
            hipLocalPosition = weaponVisual.localPosition;
            adsLocalPosition = hipLocalPosition + new Vector3(0f, -0.04f, 0.06f);
        }

        if (firePoint == null)
        {
            firePoint = ResolveDefaultFirePoint();
        }

        if (adsCamera == null)
        {
            Camera inputCamera = playerInput != null ? playerInput.camera : null;
            adsCamera = inputCamera != null ? inputCamera : Camera.main;
        }

        // Ensure audio source exists for weapon sounds
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        if (attackAction == null && enableDebugLogs)
        {
            Debug.LogWarning("GunShootTracer: Attack action not found. Falling back to mouse left button.", this);
        }

        if (weaponVisual == null && enableDebugLogs)
        {
            Debug.LogWarning("GunShootTracer: weaponVisual is not assigned. ADS position lerp is disabled.", this);
        }
    }

    void OnEnable()
    {
        attackAction?.Enable();
        reloadAction?.Enable();
    }

    void OnDisable()
    {
        attackAction?.Disable();
        reloadAction?.Disable();
    }

    void Update()
    {
        UpdateAimState();

        if (isReloading)
        {
            return;
        }

        bool wantsReload = reloadAction != null ? reloadAction.WasPressedThisFrame() : Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        if (wantsReload)
        {
            TryStartReload();
        }

        bool wantsToShoot = attackAction != null
            ? attackAction.IsPressed()
            : Mouse.current != null && Mouse.current.leftButton.isPressed;

        if (wantsToShoot && Time.time >= nextFireTime)
        {
            if (currentAmmo <= 0)
            {
                if (autoReloadWhenEmpty)
                {
                    TryStartReload();
                }
                return;
            }

            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        Vector3 rayOrigin;
        Vector3 rayDirection;
        if (!TryGetAimRay(out rayOrigin, out rayDirection))
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("GunShootTracer: No aim source/camera available, cannot shoot.", this);
            }
            return;
        }

        currentAmmo--;
        shotIndicatorUntil = Time.time + shotIndicatorDuration;

        if (hitmarkerUI != null)
        {
            hitmarkerUI.ShowCrosshairFlash();
        }

        if (muzzleFlash) muzzleFlash.Play();

        RaycastHit hit;
        Vector3 hitPoint;
        bool didDamage = false;
        bool didKill = false;

        if (TryRaycastAim(rayOrigin, rayDirection, out hit))
        {
            hitPoint = hit.point;

            IDamageable damageable = ResolveDamageable(hit.collider);
            if (enableDebugLogs)
            {
                var comp = damageable as Component;
                if (comp != null)
                    Debug.Log($"GunShootTracer: Resolved damageable on hit: {comp.gameObject.name}", this);
                else if (damageable == null)
                    Debug.Log("GunShootTracer: No damageable component found on hit collider.", this);
            }

            if (damageable != null)
            {
                float appliedDamage = ResolveDamageForHit(hit);
                didKill = damageable.TakeDamage(appliedDamage);
                didDamage = true;
            }
        }
        else
        {
            hitPoint = rayOrigin + rayDirection * range;
        }

        if (didDamage && hitmarkerUI != null)
        {
            hitmarkerUI.ShowHitmarker(didKill);
        }

        if (enableDebugLogs)
        {
            Debug.Log($"GunShootTracer: Shot fired. HitPoint={hitPoint}, Ammo={currentAmmo}/{reserveAmmo}", this);
        }

        SpawnTracer(hitPoint);
        ShotFired?.Invoke();

        // Play shot sound
        if (audioSource != null && shotClip != null)
        {
            audioSource.PlayOneShot(shotClip, shotVolume);
        }

        if (currentAmmo <= 0 && autoReloadWhenEmpty)
        {
            TryStartReload();
        }
    }

    void SpawnTracer(Vector3 hitPoint)
    {
        if (tracerPrefab == null)
        {
            return;
        }

        Transform spawnFrom = firePoint != null ? firePoint : transform;

        GameObject tracerObj = Instantiate(
            tracerPrefab,
            spawnFrom.position,
            Quaternion.identity
        );

        BulletTracer tracer = tracerObj.GetComponent<BulletTracer>();
        if (tracer != null)
        {
            tracer.Init(hitPoint);
        }
    }

    private float ResolveDamageForHit(RaycastHit hit)
    {
        Weapon weapon = GetComponentInParent<Weapon>();
        float multiplier = Mathf.Max(0.1f, damageMultiplier);

        if (weapon != null)
        {
            if (weapon.slot == WeaponSlot.Melee)
            {
                return Mathf.Max(0f, weapon.meleeDamage) * multiplier;
            }

            float bodyDamage = Mathf.Max(0f, weapon.bodyDamage);
            float headshotDamageValue = Mathf.Max(bodyDamage, weapon.headshotDamage);
            float profileScale = Mathf.Max(0.1f, damage / Mathf.Max(0.1f, bodyDamage));
            float totalScale = profileScale * multiplier;

            if (!weapon.scaleDamageByHitHeight)
            {
                return bodyDamage * totalScale;
            }

            float height01;
            if (TryGetHitHeightPercent(hit, out height01))
            {
                float headshot01 = Mathf.InverseLerp(weapon.headshotStartNormalizedHeight, weapon.headshotFullNormalizedHeight, height01);
                headshot01 = Mathf.SmoothStep(0f, 1f, headshot01);
                return Mathf.Lerp(bodyDamage, headshotDamageValue, headshot01) * totalScale;
            }

            return bodyDamage * totalScale;
        }

        return damage * multiplier;
    }

    private bool TryGetHitHeightPercent(RaycastHit hit, out float height01)
    {
        height01 = 0f;

        if (hit.collider == null)
        {
            return false;
        }

        Bounds bounds = hit.collider.bounds;
        float minY = bounds.min.y;
        float maxY = bounds.max.y;
        if (maxY <= minY + 0.001f)
        {
            return false;
        }

        height01 = Mathf.Clamp01(Mathf.InverseLerp(minY, maxY, hit.point.y));
        return true;
    }

    private Transform ResolveAimTransform()
    {
        if (aimSource != null)
        {
            return aimSource;
        }

        Camera inputCamera = playerInput != null ? playerInput.camera : null;
        if (inputCamera != null)
        {
            return inputCamera.transform;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        return null;
    }

    private bool TryGetAimRay(out Vector3 origin, out Vector3 direction)
    {
        origin = Vector3.zero;
        direction = Vector3.forward;

        Camera aimCamera = adsCamera;
        if (aimCamera == null && playerInput != null)
        {
            aimCamera = playerInput.camera;
        }

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (useScreenCenterAiming && aimCamera != null)
        {
            Vector2 center = new Vector2(aimCamera.pixelWidth * 0.5f, aimCamera.pixelHeight * 0.5f);
            Ray ray = aimCamera.ScreenPointToRay(center);
            origin = ray.origin;
            direction = ray.direction;
            return true;
        }

        Transform aimTransform = ResolveAimTransform();
        if (aimTransform == null)
        {
            return false;
        }

        origin = aimTransform.position;
        direction = aimTransform.forward;
        return true;
    }

    private bool TryRaycastAim(Vector3 rayOrigin, Vector3 rayDirection, out RaycastHit bestHit)
    {
        bestHit = new RaycastHit();
        // Include trigger colliders so hitboxes implemented as triggers are detected.
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, range, hitMask, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Transform ignoreRoot = playerInput != null ? playerInput.transform : transform.root;
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].collider != null ? hits[i].collider.transform : null;
            if (hitTransform != null && ignoreRoot != null && hitTransform.IsChildOf(ignoreRoot))
            {
                continue;
            }

            bestHit = hits[i];
            return true;
        }

        return false;
    }

    private IDamageable ResolveDamageable(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        EnemyHealth enemyHealth = hitCollider.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null)
        {
            return enemyHealth;
        }

        // Fallback: sometimes EnemyHealth may be attached to children instead of parent
        enemyHealth = hitCollider.GetComponentInChildren<EnemyHealth>();
        if (enemyHealth != null)
        {
            return enemyHealth;
        }

        IDamageable onCollider;
        if (hitCollider.TryGetComponent<IDamageable>(out onCollider))
        {
            return onCollider;
        }

        return hitCollider.GetComponentInParent<IDamageable>();
    }

    private void UpdateAimState()
    {
        IsAiming = Mouse.current != null && Mouse.current.rightButton.isPressed;

        if (useAdsPositioning && weaponVisual != null)
        {
            Vector3 targetLocalPosition = IsAiming ? adsLocalPosition : hipLocalPosition;
            weaponVisual.localPosition = Vector3.Lerp(weaponVisual.localPosition, targetLocalPosition, Time.deltaTime * adsLerpSpeed);
        }

        if (adsCamera != null)
        {
            float targetFov = IsAiming ? adsFov : hipFov;
            adsCamera.fieldOfView = Mathf.Lerp(adsCamera.fieldOfView, targetFov, Time.deltaTime * fovLerpSpeed);
        }
    }

    private void TryStartReload()
    {
        if (isReloading || currentAmmo >= magazineSize || reserveAmmo <= 0)
        {
            return;
        }

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        ReloadStateChanged?.Invoke(true);
        if (enableDebugLogs)
        {
            Debug.Log("GunShootTracer: Reload started.", this);
        }

        // Play reload sound at start
        if (audioSource != null && reloadClip != null)
        {
            audioSource.PlayOneShot(reloadClip, reloadVolume);
        }

        yield return new WaitForSeconds(reloadDuration);

        int missingAmmo = magazineSize - currentAmmo;
        int ammoToLoad = Mathf.Min(missingAmmo, reserveAmmo);
        currentAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;

        isReloading = false;
        ReloadStateChanged?.Invoke(false);

        if (enableDebugLogs)
        {
            Debug.Log($"GunShootTracer: Reload complete. Ammo={currentAmmo}/{reserveAmmo}", this);
        }
    }

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading => isReloading;

    public void AddReserveAmmo(int amount)
    {
        reserveAmmo = Mathf.Max(0, reserveAmmo + Mathf.Max(0, amount));
    }

    public void RefillMagazineAndReserve(int reserveToSetAtLeast)
    {
        currentAmmo = Mathf.Clamp(magazineSize, 0, magazineSize);
        reserveAmmo = Mathf.Max(reserveAmmo, Mathf.Max(0, reserveToSetAtLeast));
    }

    public void SetDamageMultiplier(float newMultiplier)
    {
        damageMultiplier = Mathf.Max(0.1f, newMultiplier);
    }

    private Transform ResolveDefaultWeaponVisual()
    {
        // Avoid moving the player root if this script was put on the player object.
        if (GetComponent<CharacterController>() != null || GetComponent<PlayerInput>() != null)
        {
            Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < childRenderers.Length; i++)
            {
                Transform candidate = childRenderers[i].transform;
                if (candidate != transform)
                {
                    return candidate;
                }
            }

            return null;
        }

        return transform;
    }

    private Transform ResolveDefaultFirePoint()
    {
        Transform named = FindChildByName(transform, "FirePoint");
        if (named != null)
        {
            return named;
        }

        named = FindChildByName(transform, "Muzzle");
        if (named != null)
        {
            return named;
        }

        if (weaponVisual != null)
        {
            return weaponVisual;
        }

        return transform;
    }

    private Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildByName(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private void OnGUI()
    {
        if (!showDebugOverlay)
        {
            return;
        }

        if (overlayStyle == null)
        {
            overlayStyle = new GUIStyle(GUI.skin.box);
            overlayStyle.fontSize = 14;
            overlayStyle.alignment = TextAnchor.MiddleLeft;
        }

        bool wantsToShoot = attackAction != null
            ? attackAction.IsPressed()
            : Mouse.current != null && Mouse.current.leftButton.isPressed;

        string state;
        if (isReloading)
        {
            state = "RELOADING";
        }
        else if (Time.time < shotIndicatorUntil)
        {
            state = "SHOT FIRED";
        }
        else if (wantsToShoot)
        {
            state = currentAmmo > 0 ? "ATTACK HELD" : "EMPTY";
        }
        else
        {
            state = "IDLE";
        }

        string overlayText = $"Gun Debug  |  {state}  |  Ammo: {currentAmmo}/{reserveAmmo}";
        GUI.Box(new Rect(16f, 16f, 420f, 30f), overlayText, overlayStyle);
    }
}
