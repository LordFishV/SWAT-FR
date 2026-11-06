using UnityEngine;

/// <summary>
/// Plays a gun's GunEffects in response to Gun events.
/// Put this on the same GameObject as Gun.cs.
/// The Gun never references this script, so you can delete it (or swap it for
/// a network or pooled version) without touching the gun logic.
/// </summary>
[RequireComponent(typeof(Gun))]
public class GunEffectsPlayer : MonoBehaviour
{
    [Tooltip("Where the muzzle flash and tracers start. Falls back to this transform.")]
    [SerializeField] private Transform _muzzle;
    [Tooltip("Optional. Where spent shells eject from. Falls back to the muzzle.")]
    [SerializeField] private Transform _ejectionPort;
    [Tooltip("Optional. Audio source for sound effects. Falls back to the Audio source.")]
    [SerializeField] private AudioSource _audioSource;

    private Gun _gun;
    private GunEffects _effects;
    private ParticleSystem _muzzleFlash;
    private ParticleSystem _shellEject;
    private int _bulletCount;

    private void Awake()
    {
        _gun = GetComponent<Gun>();

        if (_muzzle == null)
            _muzzle = transform;
        if (_ejectionPort == null)
            _ejectionPort = _muzzle;
        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        _effects = _gun.Data != null ? _gun.Data.effects : null;
        if (_effects == null)
            return;

        // Spawn the looping effects once and replay them, instead of
        // instantiating a new flash for every shot.
        if (_effects.muzzleFlashPrefab != null)
            _muzzleFlash = Instantiate(_effects.muzzleFlashPrefab, _muzzle);
        if (_effects.shellEjectPrefab != null)
            _shellEject = Instantiate(_effects.shellEjectPrefab, _ejectionPort);
    }

    private void OnEnable()
    {
        _gun.Fired += OnFired;
        _gun.BulletTraced += OnBulletTraced;
        _gun.DryFired += OnDryFired;
        _gun.ReloadStarted += OnReloadStarted;
        _gun.ReloadFinished += OnReloadFinished;
    }

    private void OnDisable()
    {
        _gun.Fired -= OnFired;
        _gun.BulletTraced -= OnBulletTraced;
        _gun.DryFired -= OnDryFired;
        _gun.ReloadStarted -= OnReloadStarted;
        _gun.ReloadFinished -= OnReloadFinished;
    }

    /// <summary>Once per trigger pull (a shotgun blast is ONE Fired, many BulletTraces)</summary>
    private void OnFired()
    {
        if (_effects == null)
            return;

        if (_muzzleFlash != null)
            _muzzleFlash.Play();
        if (_shellEject != null)
            _shellEject.Play();

        AudioClip clip = _effects.GetRandomFireClip();
        if (clip != null && _audioSource != null)
        {
            // Pitch is shared by everything playing on this AudioSource
            _audioSource.pitch = Random.Range(_effects.firePitchRange.x, _effects.firePitchRange.y);
            _audioSource.PlayOneShot(clip, _effects.fireVolume);
        }
    }

    /// <summary>One trace per pellet / bullet. And draw tracer lines from muzzle to endPoint, once impact spawn impact prefab</summary>
    private void OnBulletTraced(Vector3 endPoint, Vector3 hitNormal, bool didHit)
    {
        if (_effects == null)
            return;

        _bulletCount++;

        // Tracer line from the muzzle to where the bullet ended
        if (_effects.tracerPrefab != null
            && _effects.tracerEveryNthBullet > 0
            && _bulletCount % _effects.tracerEveryNthBullet == 0)
        {
            LineRenderer line = Instantiate(_effects.tracerPrefab);
            line.positionCount = 2;
            line.SetPosition(0, _muzzle.position);
            line.SetPosition(1, endPoint);
            Destroy(line.gameObject, _effects.tracerDuration);
        }

        // Impact effect, only if the bullet actually hit something
        if (didHit && _effects.impactPrefab != null)
        {
            GameObject impact = Instantiate(
                _effects.impactPrefab,
                endPoint,
                Quaternion.LookRotation(hitNormal));
            Destroy(impact, _effects.impactLifetime);
        }
    }
    /// <summary>If no ammo play dry fire audio clip</summary>
    private void OnDryFired() => PlayClip(_effects != null ? _effects.dryFireClip : null);
    /// <summary>Started reloading play reload started audio clip</summary>
    private void OnReloadStarted() => PlayClip(_effects != null ? _effects.reloadStartClip : null);
    /// <summary>Finished reloading play reload finished audio clip</summary>
    private void OnReloadFinished() => PlayClip(_effects != null ? _effects.reloadEndClip : null);

    /// <summary>Play AudioClip clip for gun</summary>
    private void PlayClip(AudioClip clip)
    {
        if (clip == null || _audioSource == null)
            return;

        _audioSource.pitch = 1f;
        _audioSource.PlayOneShot(clip, _effects.reloadVolume);
    }
}