using UnityEngine;

// All the numbers for one gun. Make one asset per gun:
// Right-click > Create > Weapons > Gun Data
// A rifle, pistol, shotgun, LMG and sniper are the same script with different numbers.
[CreateAssetMenu(fileName = "NewGun", menuName = "Weapons/Gun Data")]
public class GunData : ScriptableObject
{
    public string displayName = "New Gun";

    [Header("Shooting")]
    [Tooltip("The max damage the weapon will deal with one pelet")]
    public float damage = 25f;
    [Tooltip("How many rounds per second can be shot")]
    public float shotsPerSecond = 8f;
    [Tooltip("Is the weapon fully automatic or not. true = hold to keep firing")]
    public bool automatic = false;
    [Tooltip("The max range till")]
    public float range = 100f;
    [Tooltip("Rays per shot. 1 for most guns, 8 for a shotgun.")]
    public int pellets = 1;
    [Tooltip("Random angle in degrees. Aiming makes it smaller.")]
    public float spread = 1f;
    [Tooltip("The hit mask for all weapons. Include player and enemy layers for all weapons, also include all hard surfaces")]
    public LayerMask hitMask = ~0;

    [Header("Recoil")]
    [Tooltip("Barrel tilt in per shot in degrees")]
    public float recoil = 2f;
    [Tooltip("How much the weapon kicks back per shot in meters")]
    public float kickback = 0.03f;
    [Tooltip("How fast the gun settles back per shot")]
    public float recoilRecovery = 10f;
    [Tooltip("The amount to reduce the recoil when aming (0.5 = 50% base recoil)")]
    [Range(0f, 1f)] public float aimRecoilMultiplier = 0.5f;

    [Header("Ammo")]
    [Tooltip("The max mag size of the weapon")]
    public int magazineSize = 30;
    [Tooltip("The reserve ammo held by the character")]
    public int reserveAmmo = 90;
    [Tooltip("How lonf it takes to complete the reload of the weapon")]
    public float reloadTime = 2f;
    [Tooltip("The ammo type to show on screen")]
    public string ammoType = "5.56 × 45 mm";

    [Header("Aiming")]
    [Tooltip("Camera field of view when fully aimed. Lower zooms in more (sniper: 20).")]
    public float aimFov = 55f;
    [Tooltip("Seconds to go from hip to fully aimed.")]
    public float aimTime = 0.15f;
    [Tooltip("Spread is multiplied by this when fully aimed. 0.25 = four times tighter.")]
    [Range(0f, 1f)] public float aimSpreadMultiplier = 0.25f;

    [Header("Effects")]
    [Tooltip("The prefab to use for the muzzle flash")]
    public GameObject muzzleFlashPrefab;
    [Tooltip("The sound effect played when shooting the weapon")]
    public AudioClip fireSound;
    [Tooltip("The sound effect played when reloading the weapon")]
    public AudioClip reloadSound;
}
