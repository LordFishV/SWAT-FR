using UnityEngine;

// All the numbers for one gun. Make one asset per gun:
// Right-click > Create > Weapons > Gun Data
// A rifle, pistol, shotgun, LMG and sniper are the same script with different numbers.
[CreateAssetMenu(fileName = "NewGun", menuName = "Weapons/Gun Data")]
public class GunData : ScriptableObject
{
    public string displayName = "New Gun";

    [Header("Shooting")]
    public float damage = 25f;
    public float shotsPerSecond = 8f;
    public bool automatic = false;          // true = hold to keep firing
    public float range = 100f;
    [Tooltip("Rays per shot. 1 for most guns, 8 for a shotgun.")]
    public int pellets = 1;
    [Tooltip("Random angle in degrees. Aiming makes it smaller.")]
    public float spread = 1f;
    public float recoil = 1f;               // degrees of camera kick per shot
    public LayerMask hitMask = ~0;          // leave out the Player layer

    [Header("Ammo")]
    public int magazineSize = 30;
    public int reserveAmmo = 90;
    public float reloadTime = 2f;
    public string ammoType = "5.56 × 45 mm";    // shown on the reload HUD

    [Header("Effects")]
    public GameObject muzzleFlashPrefab;
    public AudioClip fireSound;
    public AudioClip reloadSound;
}