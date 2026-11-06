using UnityEngine;

/// <summary>
/// Pure DATA for a gun's visuals and audio. Create assets via
/// Right-click > Create > Weapons > Gun Effects, then drag one into the
/// "Effects" slot on a GunData asset. GunEffectsPlayer.cs plays them.
/// Several guns can share one GunEffects asset (e.g. all pistols).
/// </summary>
[CreateAssetMenu(fileName = "NewGunEffects", menuName = "Weapons/Gun Effects")]
public class GunEffects : ScriptableObject
{
    [Header("Muzzle")]
    [Tooltip("Particle system spawned once at the muzzle and replayed on every shot.")]
    public ParticleSystem muzzleFlashPrefab;
    [Tooltip("Optional. Spawned at the ejection port, replayed on every shot.")]
    public ParticleSystem shellEjectPrefab;

    [Header("Tracer")]
    [Tooltip("A prefab with a LineRenderer (2 points). Leave empty for no tracers.")]
    public LineRenderer tracerPrefab;
    [Tooltip("0 = never, 1 = every bullet, 3 = every third bullet.")]
    [Min(0)] public int tracerEveryNthBullet = 1;
    public float tracerDuration = 0.05f;

    [Header("Impact")]
    [Tooltip("Spawned where a bullet hits, facing away from the surface.")]
    public GameObject impactPrefab;
    public float impactLifetime = 2f;

    [Header("Audio")]
    [Tooltip("One is picked at random each shot, so repeated shots don't sound identical.")]
    public AudioClip[] fireClips;
    [Range(0f, 1f)] public float fireVolume = 1f;
    [Tooltip("Random pitch range applied per shot. (1, 1) = no variation.")]
    public Vector2 firePitchRange = new Vector2(0.95f, 1.05f);

    public AudioClip dryFireClip;
    public AudioClip reloadStartClip;
    public AudioClip reloadEndClip;
    [Range(0f, 1f)] public float reloadVolume = 0.8f;

    /// <summary>Get random fire clip AudioClip clip for gun</summary>
    public AudioClip GetRandomFireClip()
    {
        if (fireClips == null || fireClips.Length == 0)
            return null;
        return fireClips[Random.Range(0, fireClips.Length)];
    }

    private void OnValidate()
    {
        // Keep the pitch range ordered so Random.Range behaves
        if (firePitchRange.x > firePitchRange.y)
            firePitchRange = new Vector2(firePitchRange.y, firePitchRange.x);
    }
}