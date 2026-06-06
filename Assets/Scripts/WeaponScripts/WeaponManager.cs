using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    private const string AutoWeaponHolderName = "WeaponHolder_Auto";

    public Transform weaponHolder;
    public Transform defaultAimSource;
    public GameObject defaultTracerPrefab;
    public HitmarkerUI defaultHitmarkerUI;
    public bool autoSetupWeapons = true;
    public bool syncShooterOffsetsFromWeapon = true;
    public Vector3 defaultAdsOffset = new Vector3(0f, -0.04f, 0.06f);
    public Vector3 globalWeaponPositionOffset = Vector3.zero;
    public bool rotateWeapons180Y = true;
    public bool autoCreateHolderOnMainCamera = true;
    public Vector3 cameraHolderLocalPosition = new Vector3(0.20f, -0.18f, 0.45f);
    public Vector3 cameraHolderLocalEuler = Vector3.zero;
    public bool forceSlotPoseOverrides = true;
    public bool autoTunePoseByWeaponName = true;
    public bool usePrimaryPoseOverride = true;
    public Vector3 primaryLocalPosition = new Vector3(0.18f, -0.18f, 0.35f);
    public Vector3 primaryLocalEuler = Vector3.zero;
    public Vector3 primaryLocalScale = Vector3.one;
    public bool autoReplacePrimaryWithPistol = true;
    public bool usePistolPoseOverride = true;
    public Vector3 pistolLocalPosition = new Vector3(0.16f, -0.16f, 0.34f);
    public Vector3 pistolLocalEuler = new Vector3(0f, -10f, 0f);
    public Vector3 pistolLocalScale = Vector3.one;
    public bool useSecondaryPoseOverride = true;
    public Vector3 secondaryLocalPosition = new Vector3(0.16f, -0.17f, 0.32f);
    public Vector3 secondaryLocalEuler = new Vector3(0f, -10f, 0f);
    public Vector3 secondaryLocalScale = Vector3.one;
    public bool autoReparentWeaponModels = true;

    public Weapon primaryWeapon;
    public Weapon secondaryWeapon;
    public Weapon meleeWeapon;

    private Weapon currentWeapon;

    public event System.Action<Weapon> WeaponEquipped;
    public Weapon CurrentWeapon => currentWeapon;

    void Start()
    {
        EnsureWeaponHolderResolved();
        DisableMisplacedGunShootTracer();
        EnsureRuntimeSlotWeapons();

        if (autoSetupWeapons)
        {
            AutoSetupWeapon(primaryWeapon);
            AutoSetupWeapon(secondaryWeapon);
            AutoSetupWeapon(meleeWeapon);
        }

        HideAllWeaponModels();
        Equip(primaryWeapon != null ? primaryWeapon : meleeWeapon);
    }

    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame && primaryWeapon != null)
            Equip(primaryWeapon);

        if (Keyboard.current.digit2Key.wasPressedThisFrame && secondaryWeapon != null)
            Equip(secondaryWeapon);

        if (Keyboard.current.digit3Key.wasPressedThisFrame && meleeWeapon != null)
            Equip(meleeWeapon);
    }

    public void Equip(Weapon weapon)
    {
        if (weapon == null) return;

        Weapon originalReference = weapon;
        weapon = EnsureRuntimeWeaponInstance(weapon);
        if (weapon != originalReference)
        {
            if (primaryWeapon == originalReference) primaryWeapon = weapon;
            if (secondaryWeapon == originalReference) secondaryWeapon = weapon;
            if (meleeWeapon == originalReference) meleeWeapon = weapon;
        }

        EnsureWeaponModelResolved(weapon);
        if (weapon.weaponModel == null) return;

        HideAllWeaponModels();
        SnapWeaponModelToHolder(weapon);
        currentWeapon = weapon;
        currentWeapon.isEquipped = true;
        currentWeapon.weaponModel.SetActive(true);
        WeaponEquipped?.Invoke(currentWeapon);
    }

    public GunShootTracer GetCurrentShooter()
    {
        if (currentWeapon == null || currentWeapon.weaponModel == null)
        {
            return null;
        }

        return currentWeapon.weaponModel.GetComponent<GunShootTracer>();
    }

    public MeleeSlashAttack GetCurrentMeleeAttack()
    {
        if (currentWeapon == null || currentWeapon.weaponModel == null)
        {
            return null;
        }

        return currentWeapon.weaponModel.GetComponent<MeleeSlashAttack>();
    }

    public void PickupWeapon(Weapon newWeapon)
    {
        if (autoSetupWeapons)
        {
            AutoSetupWeapon(newWeapon);
        }

        // Determine which slot this weapon targets
        WeaponSlot targetSlot = newWeapon.slot;
        if (autoReplacePrimaryWithPistol && IsPistolWeapon(newWeapon))
            targetSlot = WeaponSlot.Primary;

        // If we already have a weapon in this slot, refill its ammo instead of replacing
        Weapon existingWeapon = GetWeaponInSlot(targetSlot);
        if (existingWeapon != null)
        {
            GunShootTracer existingShooter = existingWeapon.weaponModel != null
                ? existingWeapon.weaponModel.GetComponent<GunShootTracer>()
                : existingWeapon.GetComponentInChildren<GunShootTracer>();

            if (existingShooter != null)
            {
                GunShootTracer pickupShooter = newWeapon.weaponModel != null
                    ? newWeapon.weaponModel.GetComponent<GunShootTracer>()
                    : newWeapon.GetComponentInChildren<GunShootTracer>();

                int ammoGain = pickupShooter != null ? pickupShooter.magazineSize : existingShooter.magazineSize;
                existingShooter.AddReserveAmmo(ammoGain);
                return;
            }
        }

        // No weapon in that slot yet — do a normal equip
        switch (targetSlot)
        {
            case WeaponSlot.Primary:
                ReplaceWeapon(ref primaryWeapon, newWeapon);
                Equip(primaryWeapon);
                break;

            case WeaponSlot.Secondary:
                ReplaceWeapon(ref secondaryWeapon, newWeapon);
                Equip(secondaryWeapon);
                break;

            case WeaponSlot.Melee:
                ReplaceWeapon(ref meleeWeapon, newWeapon);
                Equip(meleeWeapon);
                break;
        }
    }

    Weapon GetWeaponInSlot(WeaponSlot slot)
    {
        switch (slot)
        {
            case WeaponSlot.Primary:   return primaryWeapon;
            case WeaponSlot.Secondary: return secondaryWeapon;
            case WeaponSlot.Melee:     return meleeWeapon;
            default:                   return null;
        }
    }

    void ReplaceWeapon(ref Weapon oldWeapon, Weapon newWeapon)
    {
        newWeapon = EnsureRuntimeWeaponInstance(newWeapon);

        // Drop the old weapon to the ground
        if (oldWeapon != null && oldWeapon.weaponModel != null && oldWeapon.gameObject.scene.IsValid())
        {
            // Create a dropped weapon instance
            GameObject droppedWeapon = Instantiate(oldWeapon.gameObject);
            droppedWeapon.name = oldWeapon.name + " (Dropped)";
            droppedWeapon.transform.SetParent(null); // Unparent from holder
            droppedWeapon.transform.position = oldWeapon.transform.position + Vector3.forward * 1f; // Drop in front
            
            // Add Rigidbody to make it fall
            Rigidbody rb = droppedWeapon.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = droppedWeapon.AddComponent<Rigidbody>();
            }
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.down * 2f; // Gentle drop

            // Re-add WeaponPickup component
            WeaponPickup pickup = droppedWeapon.GetComponent<WeaponPickup>();
            if (pickup == null)
            {
                pickup = droppedWeapon.AddComponent<WeaponPickup>();
            }
            
            // Reset the weapon component
            Weapon droppedWeaponComponent = droppedWeapon.GetComponent<Weapon>();
            if (droppedWeaponComponent != null)
            {
                droppedWeaponComponent.isEquipped = false;
                pickup.weapon = droppedWeaponComponent;
                if (droppedWeaponComponent.weaponModel != null)
                {
                    droppedWeaponComponent.weaponModel.SetActive(true);
                }
            }
        }

        // Setup new weapon
        newWeapon.isEquipped = true;
        if (weaponHolder != null)
        {
            newWeapon.transform.SetParent(weaponHolder);
            newWeapon.transform.localPosition = Vector3.zero;
            newWeapon.transform.localRotation = Quaternion.identity;
        }

        SnapWeaponModelToHolder(newWeapon);

        oldWeapon = newWeapon;
    }

    void EnsureRuntimeSlotWeapons()
    {
        primaryWeapon = EnsureRuntimeWeaponInstance(primaryWeapon);
        secondaryWeapon = EnsureRuntimeWeaponInstance(secondaryWeapon);
        meleeWeapon = EnsureRuntimeWeaponInstance(meleeWeapon);
    }

    Weapon EnsureRuntimeWeaponInstance(Weapon weapon)
    {
        if (weapon == null)
        {
            return null;
        }

        if (weapon.gameObject.scene.IsValid())
        {
            return weapon;
        }

        GameObject runtimeObject = Instantiate(weapon.gameObject, transform);
        runtimeObject.name = weapon.gameObject.name + " (Runtime)";
        runtimeObject.transform.localPosition = Vector3.zero;
        runtimeObject.transform.localRotation = Quaternion.identity;

        Weapon runtimeWeapon = runtimeObject.GetComponent<Weapon>();
        if (runtimeWeapon == null)
        {
            runtimeWeapon = runtimeObject.AddComponent<Weapon>();
        }

        runtimeWeapon.isEquipped = false;
        return runtimeWeapon;
    }

    void HideAllWeaponModels()
    {
        SetWeaponActiveState(primaryWeapon, false);
        SetWeaponActiveState(secondaryWeapon, false);
        SetWeaponActiveState(meleeWeapon, false);
    }

    void SetWeaponActiveState(Weapon weapon, bool active)
    {
        if (weapon == null)
        {
            return;
        }

        weapon.isEquipped = active;
        if (weapon.weaponModel != null)
        {
            weapon.weaponModel.SetActive(active);
        }
    }

    void DisableMisplacedGunShootTracer()
    {
        GunShootTracer misplaced = GetComponent<GunShootTracer>();
        if (misplaced != null)
        {
            misplaced.enabled = false;
            Debug.LogWarning("WeaponManager: Disabled GunShootTracer on player root. GunShootTracer should be on each weapon model instead.", this);
        }
    }

    void EnsureWeaponHolderResolved()
    {
        if (weaponHolder != null)
        {
            return;
        }

        if (!autoCreateHolderOnMainCamera)
        {
            return;
        }

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            return;
        }

        Transform existing = mainCam.transform.Find(AutoWeaponHolderName);
        if (existing != null)
        {
            weaponHolder = existing;
            return;
        }

        GameObject holderObj = new GameObject(AutoWeaponHolderName);
        Transform holder = holderObj.transform;
        holder.SetParent(mainCam.transform, false);
        holder.localPosition = cameraHolderLocalPosition;
        holder.localRotation = Quaternion.Euler(cameraHolderLocalEuler);
        holder.localScale = Vector3.one;
        weaponHolder = holder;
    }

    void AutoSetupWeapon(Weapon weapon)
    {
        if (weapon == null)
        {
            return;
        }

        EnsureWeaponModelResolved(weapon);
        if (weapon.weaponModel == null)
        {
            return;
        }

        SnapWeaponModelToHolder(weapon);

        if (weapon.slot == WeaponSlot.Melee)
        {
            GunShootTracer meleeShooter = weapon.weaponModel.GetComponent<GunShootTracer>();
            if (meleeShooter != null)
            {
                meleeShooter.enabled = false;
            }

            MeleeSlashAttack meleeSlash = weapon.weaponModel.GetComponent<MeleeSlashAttack>();
            if (meleeSlash == null)
            {
                meleeSlash = weapon.weaponModel.AddComponent<MeleeSlashAttack>();
            }

            meleeSlash.weaponVisual = weapon.weaponModel.transform;
            meleeSlash.idleEuler = weapon.equippedLocalEuler;
            meleeSlash.slashOrigin = ResolveAimSource();
            return;
        }

        MeleeSlashAttack slash = weapon.weaponModel.GetComponent<MeleeSlashAttack>();
        if (slash != null)
        {
            slash.enabled = false;
        }

        GunShootTracer shooter = weapon.weaponModel.GetComponent<GunShootTracer>();
        if (shooter == null)
        {
            shooter = weapon.weaponModel.AddComponent<GunShootTracer>();
        }
        shooter.enabled = true;

        WeaponFollowCamera follow = weapon.weaponModel.GetComponent<WeaponFollowCamera>();
        if (follow != null)
        {
            follow.enabled = false;
        }

        if (shooter.aimSource == null)
        {
            shooter.aimSource = ResolveAimSource();
        }

        if (shooter.weaponVisual == null)
        {
            shooter.weaponVisual = weapon.weaponModel.transform;
        }

        if (syncShooterOffsetsFromWeapon)
        {
            GetTargetPose(weapon, out Vector3 baseLocalPosition, out _, out _);

            Vector3 basePosition = baseLocalPosition + globalWeaponPositionOffset;
            shooter.hipLocalPosition = basePosition;
            shooter.adsLocalPosition = basePosition + defaultAdsOffset;
        }

        shooter.useAdsPositioning = false;

        if (shooter.firePoint == null)
        {
            shooter.firePoint = FindTransformByName(weapon.weaponModel.transform, "FirePoint");
            if (shooter.firePoint == null)
            {
                shooter.firePoint = FindTransformByName(weapon.weaponModel.transform, "Muzzle");
            }
        }

        if (shooter.tracerPrefab == null && defaultTracerPrefab != null)
        {
            shooter.tracerPrefab = defaultTracerPrefab;
        }

        if (shooter.hitmarkerUI == null && defaultHitmarkerUI != null)
        {
            shooter.hitmarkerUI = defaultHitmarkerUI;
        }
    }

    Transform ResolveAimSource()
    {
        if (defaultAimSource != null)
        {
            return defaultAimSource;
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            return mainCam.transform;
        }

        return transform;
    }

    Transform FindTransformByName(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == targetName)
            {
                return child;
            }

            Transform nested = FindTransformByName(child, targetName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    void SnapWeaponModelToHolder(Weapon weapon)
    {
        if (weapon == null || weapon.weaponModel == null || weaponHolder == null)
        {
            return;
        }

        Transform model = weapon.weaponModel.transform;

        if (IsUnsafeWeaponModel(model))
        {
            Debug.LogError($"WeaponManager: Weapon '{weapon.name}' has an unsafe weaponModel reference ('{model.name}'). Assign only the gun mesh/model object, not player/camera/controller roots.", weapon);
            return;
        }

        if (model.parent != weaponHolder)
        {
            // Always attach to holder so local offsets are applied in the correct space.
            model.SetParent(weaponHolder, false);
            if (!autoReparentWeaponModels)
            {
                Debug.LogWarning($"WeaponManager: Auto-attached '{model.name}' to weaponHolder to prevent bad world-space placement.", weapon);
            }
        }

        GetTargetPose(weapon, out Vector3 targetLocalPosition, out Vector3 targetLocalEuler, out Vector3 targetLocalScale);

        if (rotateWeapons180Y)
        {
            targetLocalEuler.y += 180f;
        }

        model.localPosition = targetLocalPosition + globalWeaponPositionOffset;
        model.localRotation = Quaternion.Euler(targetLocalEuler);
        model.localScale = targetLocalScale;
    }

    void GetTargetPose(Weapon weapon, out Vector3 localPosition, out Vector3 localEuler, out Vector3 localScale)
    {
        localPosition = weapon.equippedLocalPosition;
        localEuler = weapon.equippedLocalEuler;
        localScale = weapon.equippedLocalScale;

        if (IsPistolWeapon(weapon) && (usePistolPoseOverride || forceSlotPoseOverrides))
        {
            localPosition = pistolLocalPosition;
            localEuler = pistolLocalEuler;
            localScale = pistolLocalScale;
        }

        if (weapon.slot == WeaponSlot.Primary && (usePrimaryPoseOverride || forceSlotPoseOverrides))
        {
            localPosition = primaryLocalPosition;
            localEuler = primaryLocalEuler;
            localScale = primaryLocalScale;
        }

        if (weapon.slot == WeaponSlot.Secondary && (useSecondaryPoseOverride || forceSlotPoseOverrides))
        {
            localPosition = secondaryLocalPosition;
            localEuler = secondaryLocalEuler;
            localScale = secondaryLocalScale;
        }

        if (autoTunePoseByWeaponName)
        {
            ApplyNameBasedPoseTweaks(weapon, ref localPosition, ref localEuler);
        }
    }

    void ApplyNameBasedPoseTweaks(Weapon weapon, ref Vector3 localPosition, ref Vector3 localEuler)
    {
        if (weapon == null)
        {
            return;
        }

        string nameLower = weapon.name.ToLowerInvariant();

        if (weapon.slot == WeaponSlot.Primary)
        {
            if (nameLower.Contains("m4") || nameLower.Contains("rifle") || nameLower.Contains("ak") || nameLower.Contains("uzi"))
            {
                localPosition = new Vector3(0.18f, -0.18f, 0.35f);
                localEuler = new Vector3(0f, -2f, 0f);
            }
        }

        if (weapon.slot == WeaponSlot.Secondary || IsPistolWeapon(weapon))
        {
            if (nameLower.Contains("pistol") || nameLower.Contains("handgun") || nameLower.Contains("m1911"))
            {
                localPosition = new Vector3(0.16f, -0.17f, 0.32f);
                localEuler = new Vector3(0f, -10f, 0f);
            }
        }
    }

    bool IsPistolWeapon(Weapon weapon)
    {
        if (weapon == null)
        {
            return false;
        }

        string weaponName = weapon.name.ToLowerInvariant();
        return weaponName.Contains("pistol") || weaponName.Contains("handgun");
    }

    bool IsUnsafeWeaponModel(Transform model)
    {
        if (model == null)
        {
            return true;
        }

        if (model == transform || model == weaponHolder)
        {
            return true;
        }

        if (model.GetComponent<PlayerInput>() != null)
        {
            return true;
        }

        if (model.GetComponent<CharacterController>() != null)
        {
            return true;
        }

        if (model.GetComponentInChildren<Camera>(true) != null)
        {
            return true;
        }

        return false;
    }

    void EnsureWeaponModelResolved(Weapon weapon)
    {
        if (weapon == null)
        {
            return;
        }

        if (weapon.weaponModel != null)
        {
            return;
        }

        Renderer[] renderers = weapon.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Transform candidate = renderers[i].transform;
            if (candidate == weapon.transform)
            {
                continue;
            }

            if (IsUnsafeWeaponModel(candidate))
            {
                continue;
            }

            weapon.weaponModel = candidate.gameObject;
            Debug.LogWarning($"WeaponManager: Auto-assigned weaponModel '{weapon.weaponModel.name}' for weapon '{weapon.name}'.", weapon);
            return;
        }

        if (!IsUnsafeWeaponModel(weapon.transform))
        {
            weapon.weaponModel = weapon.gameObject;
            Debug.LogWarning($"WeaponManager: Fallback weaponModel set to root object for weapon '{weapon.name}'.", weapon);
        }
    }
}
