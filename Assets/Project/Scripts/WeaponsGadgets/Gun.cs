using System.Collections;
using UnityEngine;
using UnityEngine.Windows;

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
    public bool IsShooting { get; private set; }
    public bool IsAiming { get; private set; }


    // 0 = hip fire, 1 = fully aimed. Smoothed over aimTime seconds.
    public float AimAmount { get; private set; }

    private float _nextShotTime;
    private Vector3 _hipPosition;
    private PlayerCamera _camera;    // found automatically, used for recoil
    private float _recoilRotation, _recoilPosition;
    private Quaternion _defaultRotation;
    private Vector3 _defaultPosition;

    private void Awake() {
        AmmoInMag = _data.magazineSize;
        ReserveAmmo = _data.reserveAmmo;
        _defaultRotation = transform.localRotation;
        _defaultPosition = transform.localPosition;

        if (_model != null)
            _hipPosition = _model.localPosition;
    }

    private void Update() {
        if (!IsShooting)
        transform.localRotation = Quaternion.Slerp(transform.localRotation, _defaultRotation, 10f * Time.deltaTime);
        transform.localPosition = Vector3.Lerp(transform.localPosition, _defaultPosition, 10f * Time.deltaTime);
    }

    // Putting the gun away cancels a reload and drops out of aim
    private void OnDisable() {
        IsReloading = false;
        AimAmount = 0f;

        if (_model != null)
            _model.localPosition = _hipPosition;
    }

    public override void UpdateItem(PlayerInput input, Transform aim) {
        TryAim(input);

        if (input.Reload)
            StartReload();

        bool wantsToShoot = _data.automatic ? input.FireHeld : input.FirePressed;
        if (wantsToShoot)
            TryShoot(aim);
    }

    private void TryAim(PlayerInput input) {
        // You can't stay aimed while reloading
        IsAiming = input.Aim && !IsReloading;

        if (IsAiming) {
            AimAmount = Mathf.MoveTowards(AimAmount, 1f, Time.deltaTime / _data.aimTime);
        } else {
            AimAmount = Mathf.MoveTowards(AimAmount, 0f, Time.deltaTime / _data.aimTime);
        }

        // Slide the weapon between the hip position and the aim position
        if (_model != null) {
            float eased = Mathf.SmoothStep(0f, 1f, AimAmount);
            _model.localPosition = Vector3.Lerp(_hipPosition, _aimPosition, eased);
        }
    }

    private void TryShoot(Transform aim) {
        if (IsReloading || Time.time < _nextShotTime) {
            IsShooting = false;
            return;
        }

        if (AmmoInMag <= 0) {
            StartReload();
            return;
        }

        _nextShotTime = Time.time + 1f / _data.shotsPerSecond;
        AmmoInMag--;

        // Spread tightens as you aim
        float spread = Mathf.Lerp(_data.spread, _data.spread * _data.aimSpreadMultiplier, AimAmount);

        for (int i = 0; i < _data.pellets; i++) {
            // A random direction inside a small cone around where you are looking
            Vector2 offset = Random.insideUnitCircle * Mathf.Tan(spread * Mathf.Deg2Rad);
            Vector3 direction = (aim.rotation * new Vector3(offset.x, offset.y, 1f)).normalized;

            if (Physics.Raycast(aim.position, direction, out RaycastHit hit, _data.range,
                    _data.hitMask, QueryTriggerInteraction.Ignore)) {
                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null)
                    target.ApplyDamage(_data.damage);
            }
        }

        PlayShotEffects();
    }

    private void PlayShotEffects() {
        IsShooting = true;
        if (_data.muzzleFlashPrefab != null) {
            Transform spawnPoint = _muzzle != null ? _muzzle : transform;
            GameObject flash = Instantiate(_data.muzzleFlashPrefab, spawnPoint);
            Destroy(flash, 1f);
        }

        if (_data.fireSound != null && _audio != null)
            _audio.PlayOneShot(_data.fireSound);

        AddRecoila(Random.Range(0, _data.recoil));
    }

    public void AddRecoila(float kick) {
        if (IsAiming) {
            _recoilPosition -= kick * 0.01f;
            _recoilRotation -= kick;

            _recoilRotation = Mathf.Clamp(_recoilRotation, -89f, 89f);
            transform.localRotation = Quaternion.Euler(_recoilRotation, 0f, 0f);
            transform.localPosition = new Vector3(0f, 0f, _recoilPosition);

            _recoilRotation = transform.localRotation.x;
            _recoilPosition = transform.localPosition.x;
        } else {
            _recoilRotation -= kick;
            _recoilRotation = Mathf.Clamp(_recoilRotation, -89f, 89f);
            transform.localRotation = Quaternion.Euler(_recoilRotation, 0f, 0f);
            _recoilRotation = transform.localRotation.x;
        }
    }

    private void StartReload() {
        if (IsReloading || AmmoInMag == _data.magazineSize || ReserveAmmo <= 0)
            return;

        StartCoroutine(Reload());
    }

    private IEnumerator Reload() {
        IsReloading = true;
        IsShooting = false;
        IsAiming = false;

        if (_data.reloadSound != null && _audio != null)
            _audio.PlayOneShot(_data.reloadSound);

        yield return new WaitForSeconds(_data.reloadTime);

        int loaded = Mathf.Min(_data.magazineSize - AmmoInMag, ReserveAmmo);
        AmmoInMag += loaded;
        ReserveAmmo -= loaded;
        IsReloading = false;
    }
}