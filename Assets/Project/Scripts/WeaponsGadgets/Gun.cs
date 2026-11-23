using System.Collections;
using UnityEngine;

// Put this on the root of a weapon prefab and assign a GunData.
public class Gun : Equippable
{
    [Header("References")]
    [SerializeField] private GunData _data;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private AudioSource _audio;

    [Header("Aiming")]
    [Tooltip("The child object holding the weapon mesh. It slides to the aim position when aiming.")]
    [SerializeField] private Transform _model;
    [Tooltip("Local position of the Model while aiming, with the sights lined up with the screen centre.")]
    [SerializeField] private Vector3 _aimPosition;

    public GunData Data => _data;
    public int AmmoInMag { get; private set; }
    public int ReserveAmmo { get; private set; }
    public bool IsReloading { get; private set; }
    public bool IsAiming { get; private set; }

    // True for a moment after each shot. Useful for animation.
    public bool IsShooting => Time.time < _lastShotTime + 0.15f;

    // 0 = hip fire, 1 = fully aimed. Smoothed over aimTime seconds.
    public float AimAmount { get; private set; }

    private const float MaxRecoilPitch = 20f;   // stops the gun tilting too far when firing fast

    private float _nextShotTime;
    private float _lastShotTime = -10f;
    private Vector3 _hipPosition;

    // Recoil is stored as offsets that fade to zero, applied on top of the resting pose
    private float _recoilPitch;      // degrees the barrel is tilted up
    private float _recoilKickback;   // metres the gun is pushed toward the player
    private Quaternion _defaultRotation;
    private Vector3 _defaultPosition;

    private void Awake()
    {
        AmmoInMag = _data.magazineSize;
        ReserveAmmo = _data.reserveAmmo;
        _defaultRotation = transform.localRotation;
        _defaultPosition = transform.localPosition;

        if (_model != null)
            _hipPosition = _model.localPosition;
    }

    private void Update()
    {
        // Recoil fades back to zero every frame, so it recovers even while firing
        float recover = 1f - Mathf.Exp(-_data.recoilRecovery * Time.deltaTime);
        _recoilPitch = Mathf.Lerp(_recoilPitch, 0f, recover);
        _recoilKickback = Mathf.Lerp(_recoilKickback, 0f, recover);

        // Applied on top of the prefab's resting pose, so its x and y are never lost.
        // A negative X rotation tilts the barrel up. Negative Z pulls the gun back.
        transform.localRotation = _defaultRotation * Quaternion.Euler(-_recoilPitch, 0f, 0f);
        transform.localPosition = _defaultPosition + new Vector3(0f, 0f, -_recoilKickback);
    }

    // Putting the gun away cancels a reload, drops out of aim and clears recoil
    private void OnDisable()
    {
        IsReloading = false;
        IsAiming = false;
        AimAmount = 0f;
        _recoilPitch = 0f;
        _recoilKickback = 0f;

        transform.localRotation = _defaultRotation;
        transform.localPosition = _defaultPosition;

        if (_model != null)
            _model.localPosition = _hipPosition;
    }

    public override void UpdateItem(PlayerInput input, Transform aim)
    {
        TryAim(input);

        if (input.Reload)
            StartReload();

        bool wantsToShoot = _data.automatic ? input.FireHeld : input.FirePressed;
        if (wantsToShoot)
            TryShoot(aim);
    }

    private void TryAim(PlayerInput input)
    {
        // You can't stay aimed while reloading
        IsAiming = input.Aim && !IsReloading;

        float target = IsAiming ? 1f : 0f;
        AimAmount = Mathf.MoveTowards(AimAmount, target, Time.deltaTime / _data.aimTime);

        // Slide the weapon between the hip position and the aim position
        if (_model != null)
        {
            float eased = Mathf.SmoothStep(0f, 1f, AimAmount);
            _model.localPosition = Vector3.Lerp(_hipPosition, _aimPosition, eased);
        }
    }

    private void TryShoot(Transform aim)
    {
        if (IsReloading || Time.time < _nextShotTime)
            return;

        if (AmmoInMag <= 0)
        {
            StartReload();
            return;
        }

        _nextShotTime = Time.time + 1f / _data.shotsPerSecond;
        _lastShotTime = Time.time;
        AmmoInMag--;

        // Spread tightens as you aim
        float spread = Mathf.Lerp(_data.spread, _data.spread * _data.aimSpreadMultiplier, AimAmount);

        for (int i = 0; i < _data.pellets; i++)
        {
            // A random direction inside a small cone around where you are looking
            Vector2 offset = Random.insideUnitCircle * Mathf.Tan(spread * Mathf.Deg2Rad);
            Vector3 direction = (aim.rotation * new Vector3(offset.x, offset.y, 1f)).normalized;

            if (Physics.Raycast(aim.position, direction, out RaycastHit hit, _data.range,
                    _data.hitMask, QueryTriggerInteraction.Ignore))
            {
                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null)
                    target.ApplyDamage(_data.damage);
            }
        }

        PlayShotEffects();
    }

    private void PlayShotEffects()
    {
        if (_data.muzzleFlashPrefab != null)
        {
            Transform spawnPoint = _muzzle != null ? _muzzle : transform;
            GameObject flash = Instantiate(_data.muzzleFlashPrefab, spawnPoint);
            Destroy(flash, 1f);
        }

        if (_data.fireSound != null && _audio != null)
            _audio.PlayOneShot(_data.fireSound);

        // A kick between 60% and 100% of the gun's recoil, so shots aren't identical
        AddRecoil(Random.Range(_data.recoil * 0.6f, _data.recoil));
    }

    public void AddRecoil(float kick)
    {
        // Aiming steadies the gun
        float scale = Mathf.Lerp(1f, _data.aimRecoilMultiplier, AimAmount);

        _recoilPitch = Mathf.Min(_recoilPitch + kick * scale, MaxRecoilPitch);
        _recoilKickback += _data.kickback * scale;
    }

    private void StartReload()
    {
        if (IsReloading || AmmoInMag == _data.magazineSize || ReserveAmmo <= 0)
            return;

        StartCoroutine(Reload());
    }

    private IEnumerator Reload()
    {
        IsReloading = true;

        if (_data.reloadSound != null && _audio != null)
            _audio.PlayOneShot(_data.reloadSound);

        yield return new WaitForSeconds(_data.reloadTime);

        int loaded = Mathf.Min(_data.magazineSize - AmmoInMag, ReserveAmmo);
        AmmoInMag += loaded;
        ReserveAmmo -= loaded;
        IsReloading = false;
    }
}