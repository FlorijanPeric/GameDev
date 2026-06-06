using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCombatAnimationBridge : MonoBehaviour
{
    [Header("References")]
    public WeaponManager weaponManager;
    public Animator animator;

    [Header("Animator Params")]
    public string weaponTypeParam = "WeaponType";
    public string weaponSwapTrigger = "WeaponSwap";
    public string fireTrigger = "Fire";
    public string meleeTrigger = "Melee";
    public string reloadBool = "Reloading";
    public string isAimingBool = "IsAiming";

    private readonly HashSet<int> animatorParams = new HashSet<int>();

    private Weapon observedWeapon;
    private GunShootTracer observedShooter;
    private MeleeSlashAttack observedMelee;

    private int weaponTypeHash;
    private int weaponSwapHash;
    private int fireHash;
    private int meleeHash;
    private int reloadHash;
    private int isAimingHash;

    private void Awake()
    {
        ResolveReferences();
        CacheParameterHashes();
        BuildAnimatorParamLookup();
    }

    private void OnEnable()
    {
        SubscribeWeaponManager();
        RefreshObservedWeapon(forceSwapTrigger: false);
    }

    private void OnDisable()
    {
        UnsubscribeWeaponManager();
        UnsubscribeCombatSources();
    }

    private void Update()
    {
        if (weaponManager == null)
        {
            ResolveReferences();
            SubscribeWeaponManager();
        }

        if (weaponManager != null && weaponManager.CurrentWeapon != observedWeapon)
        {
            RefreshObservedWeapon(forceSwapTrigger: true);
        }

        if (observedShooter != null)
        {
            SetBoolIfExists(isAimingHash, observedShooter.IsAiming);
        }
        else
        {
            SetBoolIfExists(isAimingHash, false);
        }
    }

    private void ResolveReferences()
    {
        if (weaponManager == null)
        {
            weaponManager = GetComponentInParent<WeaponManager>();
            if (weaponManager == null)
            {
                weaponManager = FindObjectOfType<WeaponManager>();
            }
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = FindObjectOfType<Animator>();
            }
        }
    }

    private void CacheParameterHashes()
    {
        weaponTypeHash = Animator.StringToHash(weaponTypeParam);
        weaponSwapHash = Animator.StringToHash(weaponSwapTrigger);
        fireHash = Animator.StringToHash(fireTrigger);
        meleeHash = Animator.StringToHash(meleeTrigger);
        reloadHash = Animator.StringToHash(reloadBool);
        isAimingHash = Animator.StringToHash(isAimingBool);
    }

    private void BuildAnimatorParamLookup()
    {
        animatorParams.Clear();
        if (animator == null)
        {
            return;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            animatorParams.Add(parameters[i].nameHash);
        }
    }

    private void SubscribeWeaponManager()
    {
        if (weaponManager == null)
        {
            return;
        }

        weaponManager.WeaponEquipped -= OnWeaponEquipped;
        weaponManager.WeaponEquipped += OnWeaponEquipped;
    }

    private void UnsubscribeWeaponManager()
    {
        if (weaponManager == null)
        {
            return;
        }

        weaponManager.WeaponEquipped -= OnWeaponEquipped;
    }

    private void OnWeaponEquipped(Weapon equipped)
    {
        RefreshObservedWeapon(forceSwapTrigger: true);
    }

    private void RefreshObservedWeapon(bool forceSwapTrigger)
    {
        UnsubscribeCombatSources();

        if (weaponManager == null)
        {
            return;
        }

        observedWeapon = weaponManager.CurrentWeapon;
        observedShooter = weaponManager.GetCurrentShooter();
        observedMelee = weaponManager.GetCurrentMeleeAttack();

        if (observedShooter != null)
        {
            observedShooter.ShotFired += OnShotFired;
            observedShooter.ReloadStateChanged += OnReloadStateChanged;
            SetBoolIfExists(reloadHash, observedShooter.IsReloading);
        }
        else
        {
            SetBoolIfExists(reloadHash, false);
        }

        if (observedMelee != null)
        {
            observedMelee.SlashPerformed += OnMeleePerformed;
        }

        SetFloatIfExists(weaponTypeHash, GetWeaponTypeValue(observedWeapon));
        if (forceSwapTrigger)
        {
            SetTriggerIfExists(weaponSwapHash);
        }
    }

    private void UnsubscribeCombatSources()
    {
        if (observedShooter != null)
        {
            observedShooter.ShotFired -= OnShotFired;
            observedShooter.ReloadStateChanged -= OnReloadStateChanged;
            observedShooter = null;
        }

        if (observedMelee != null)
        {
            observedMelee.SlashPerformed -= OnMeleePerformed;
            observedMelee = null;
        }
    }

    private float GetWeaponTypeValue(Weapon weapon)
    {
        if (weapon == null)
        {
            return 0f;
        }

        switch (weapon.slot)
        {
            case WeaponSlot.Primary:
                return 0f;
            case WeaponSlot.Secondary:
                return 1f;
            case WeaponSlot.Melee:
                return 2f;
            default:
                return 0f;
        }
    }

    private void OnShotFired()
    {
        SetTriggerIfExists(fireHash);
    }

    private void OnReloadStateChanged(bool reloading)
    {
        SetBoolIfExists(reloadHash, reloading);
    }

    private void OnMeleePerformed()
    {
        SetTriggerIfExists(meleeHash);
    }

    private void SetTriggerIfExists(int hash)
    {
        if (animator == null || !animatorParams.Contains(hash))
        {
            return;
        }

        animator.SetTrigger(hash);
    }

    private void SetBoolIfExists(int hash, bool value)
    {
        if (animator == null || !animatorParams.Contains(hash))
        {
            return;
        }

        animator.SetBool(hash, value);
    }

    private void SetFloatIfExists(int hash, float value)
    {
        if (animator == null || !animatorParams.Contains(hash))
        {
            return;
        }

        animator.SetFloat(hash, value);
    }
}
