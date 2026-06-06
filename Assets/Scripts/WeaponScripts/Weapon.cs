using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponSlot slot;
    public GameObject weaponModel;
    public Vector3 equippedLocalPosition = new Vector3(0.18f, -0.18f, 0.35f);
    public Vector3 equippedLocalEuler = Vector3.zero;
    public Vector3 equippedLocalScale = Vector3.one;

    [Header("Combat")]
    public float bodyDamage = 10f;
    public float headshotDamage = 30f;
    [Range(0f, 1f)] public float headshotStartNormalizedHeight = 0.7f;
    [Range(0f, 1f)] public float headshotFullNormalizedHeight = 0.9f;
    public bool scaleDamageByHitHeight = true;
    public float meleeDamage = 50f;

    [HideInInspector] public bool isEquipped = false;
    [HideInInspector] public bool isPickup = false;

    private void Awake()
    {
        ApplyDefaultCombatProfile();
    }

    private void OnValidate()
    {
        ApplyDefaultCombatProfile();
    }

    void Start()
    {
        if (weaponModel == null)
        {
            Debug.LogWarning("Weapon: weaponModel is missing.", this);
            return;
        }

        // Don't hide the model if it's a world pickup — the spawner already made it visible.
        if (!isEquipped && !isPickup)
        {
            weaponModel.SetActive(false);
        }
    }

    public void ApplyDefaultCombatProfile()
    {
        string weaponName = name != null ? name.ToLowerInvariant() : string.Empty;

        if (slot == WeaponSlot.Melee || weaponName.Contains("katana") || weaponName.Contains("sword"))
        {
            meleeDamage = 50f;
            return;
        }

        if (weaponName.Contains("pistol") || weaponName.Contains("handgun") || slot == WeaponSlot.Secondary)
        {
            bodyDamage = 20f;
            headshotDamage = 30f;
            headshotStartNormalizedHeight = 0.68f;
            headshotFullNormalizedHeight = 0.88f;
            scaleDamageByHitHeight = true;
            return;
        }

        bodyDamage = 10f;
        headshotDamage = 30f;
        headshotStartNormalizedHeight = 0.72f;
        headshotFullNormalizedHeight = 0.92f;
        scaleDamageByHitHeight = true;
    }
}
