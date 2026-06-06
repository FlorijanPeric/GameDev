# Weapon System Setup Guide

## Quick Start: Make All Guns Work

This guide will help you get all weapons (Rifle, Pistol, Shotgun, SMG, AssaultRifle, Sniper, LMG, RPG) working in your survival FPS.

## One-Click Setup (Recommended)

1. **In the Unity Editor**, go to: **Tools → Weapons → Setup All Weapons For Gameplay**
   - This will:
     - Create prefabs for all 8 weapons
     - Add GunShootTracer to each weapon
     - Configure damage, ammo, fire rate, etc.

2. **Verify the weapons are created**: 
   - Go to **Tools → Weapons → List All Configured Weapons**
   - This shows all weapons with their setup status

3. **In your Player object**:
   - Find the WeaponManager component
   - Assign weapons to the slots:
     - **Primary Weapon**: Drag `Assets/Prefabs/Weapons/Rifle.prefab`
     - **Secondary Weapon**: Drag `Assets/Prefabs/Weapons/Pistol.prefab`
     - **Melee Weapon**: Drag any melee weapon (or leave empty for now)

4. **Play and test**:
   - Press `1` to switch to primary (Rifle)
   - Press `2` to switch to secondary (Pistol)
   - Click mouse to shoot
   - `R` to reload

## What's Available

All 8 weapons are configured:

| Weapon | Type | Slot | Model |
|--------|------|------|-------|
| **Rifle** | Assault Rifle | Primary | M4_8 |
| **Pistol** | Handgun | Secondary | Pistol 2 |
| **Shotgun** | Combat Shotgun | Primary | Bennelli M4 |
| **SMG** | Submachine Gun | Primary | Uzi |
| **AssaultRifle** | Russian AK | Primary | AK74 |
| **Sniper** | Sniper Rifle | Primary | M107 |
| **LMG** | Light Machine Gun | Primary | M249 |
| **RPG** | Rocket Launcher | Primary | RPG7 |

## Manual Setup (If Needed)

If the one-click setup doesn't work:

1. **Create Weapon Prefabs**:
   - Go to: **Tools → Weapons → Create All Weapon Prefabs**

2. **Auto-configure in Scene**:
   - Go to: **Tools → Weapons → Auto-Configure All Weapons In Scene**

3. **Assign to WeaponManager**:
   - In your Player object's WeaponManager component
   - Drag the created prefabs to the weapon slot fields

## Troubleshooting

### Weapons don't shoot
- Check that GunShootTracer is on the weapon model (not the container)
- Verify firePoint is found or configured in GunShootTracer inspector
- Make sure WeaponManager.autoSetupWeapons is enabled

### Ammo shows 0
- Check GunShootTracer.magazineSize and reserveAmmo values
- Try pressing `R` to reload

### Can't switch weapons
- Verify all weapon slots in WeaponManager are assigned
- Check that weapons have the Weapon component

### Pistol specifically not working
- Ensure Pistol.prefab was created in `Assets/Prefabs/Weapons/`
- Check that it has GunShootTracer on the model child
- Verify Secondary weapon slot is assigned in WeaponManager

## Weapon Stats (Configurable in GunShootTracer)

- **Damage**: 10 per shot
- **Range**: 100 units
- **Fire Rate**: 0.1 seconds (10 shots per second)
- **Magazine Size**: 30 rounds
- **Reserve Ammo**: 120 rounds
- **Reload Time**: 1.6 seconds

These can be customized per weapon by selecting each weapon prefab and editing the GunShootTracer component.
