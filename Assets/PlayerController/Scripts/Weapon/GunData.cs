using UnityEngine;

/// <summary>
/// Pure DATA for one kind of gun. Create assets via
/// Right-click > Create > Weapons > Gun Data.
/// Never change these values at runtime: the asset is shared by every
/// instance of the gun. Runtime state (ammo, cooldowns) lives on Gun.cs.
/// </summary>
[CreateAssetMenu(fileName = "NewGun", menuName = "Weapons/Gun Data")]
public class GunData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "New Gun";
    public WeaponType weaponType = WeaponType.Rifle;
    public WeaponSlot slot = WeaponSlot.Primary;
    [UnityEngine.Serialization.FormerlySerializedAs("viewModelPrefab")]
    [Tooltip("The full weapon prefab (model + Gun + effects scripts) the loadout spawns.")]
    public GameObject weaponPrefab;
    [Tooltip("Seconds to bring the weapon up after switching to it.")]
    public float equipTime = 0.4f;

    [Header("Firing")]
    [Tooltip("First entry is the default. Add more to allow switching modes.")]
    public WeaponFireMode[] fireModes = { WeaponFireMode.SemiAuto };
    [Min(1f)] public float roundsPerMinute = 600f;
    [Min(1)] public int burstCount = 3;
    [Tooltip("Pause after a burst finishes, in seconds.")]
    public float burstCooldown = 0.25f;

    [Header("Damage")]
    public float damage = 25f;
    [Tooltip("Rays per shot. 1 for most guns, 8+ for shotguns. Damage applies PER pellet.")]
    [Min(1)] public int pellets = 1;
    public float range = 100f;
    [Tooltip("X = distance / range (0-1), Y = damage multiplier.")]
    public AnimationCurve damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    public LayerMask hitMask = ~0;

    [Header("Accuracy (cone half-angle, degrees)")]
    public float hipSpread = 2f;
    public float adsSpread = 0.3f;

    [Header("Recoil (degrees per shot)")]
    public float recoilUp = 0.8f;
    public float recoilSide = 0.2f;

    [Header("Ammo")]
    [Min(1)] public int magazineSize = 30;
    [Min(0)] public int startingReserve = 90;
    public float reloadTime = 2f;
    [Tooltip("Shotgun-style: load one round at a time, can be interrupted by firing.")]
    public bool reloadOneRoundAtATime = false;
    public float roundReloadTime = 0.5f;

    [Header("Effects")]
    public GunEffects effects;

    [Header("Aiming")]
    [Tooltip("Camera FOV while aiming. Lower = more zoom (snipers).")]
    public float adsFov = 60f;

    // Seconds between shots, derived from rounds per minute.
    public float FireInterval => 60f / roundsPerMinute;

    private void OnValidate()
    {
        if (fireModes == null || fireModes.Length == 0)
            fireModes = new[] { WeaponFireMode.SemiAuto };
        roundsPerMinute = Mathf.Max(1f, roundsPerMinute);
    }
}