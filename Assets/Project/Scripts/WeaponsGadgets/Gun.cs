using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;

// Put this on the root of a weapon prefab and assign a GunData.
public class Gun : Equippable
{
    [SerializeField] private GunData _data;
    [SerializeField] private Transform _muzzle;      // empty object at the end of the barrel
    [SerializeField] private AudioSource _audio;
    [SerializeField] HUDController _hudUI;
    [SerializeField] private Texture2D _hudIcon;
    [SerializeField] private string _hudName;

    public GunData Data => _data;
    public int AmmoInMag { get; private set; }
    public int ReserveAmmo { get; private set; }
    public bool IsReloading { get; private set; }

    private float _nextShotTime;
    private PlayerCamera _camera;    // found automatically, used for recoil

    private void Awake()
    {
        AmmoInMag = _data.magazineSize;
        ReserveAmmo = _data.reserveAmmo;
    }

    // Putting the gun away cancels a reload
    private void OnDisable()
    {
        IsReloading = false;
    }

    public override void UpdateItem(PlayerInput input, Transform aim)
    {
        if (input.Reload)
            StartReload();

        bool wantsToShoot = _data.automatic ? input.FireHeld : input.FirePressed;
        if (wantsToShoot)
            TryShoot(aim, input.Aim);
    }

    private void TryShoot(Transform aim, bool aiming)
    {
        if (IsReloading || Time.time < _nextShotTime)
            return;

        if (AmmoInMag <= 0)
        {
            StartReload();
            return;
        }

        _nextShotTime = Time.time + 1f / _data.shotsPerSecond;
        AmmoInMag--;

        float spread = aiming ? _data.spread * 0.25f : _data.spread;

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
                    target.ApplyDamage(_data.damage, gameObject);
                Debug.DrawRay(aim.position, direction * _data.range, Color.red, 2f);
            }
            else
            {
                Debug.DrawRay(aim.position, direction * _data.range, Color.yellow, 2f);
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

        if (_camera == null)
            _camera = GetComponentInParent<PlayerCamera>();
        if (_camera != null)
            _camera.AddRecoil(new Vector2(_data.recoil, Random.Range(-0.3f, 0.3f) * _data.recoil));
    }

    private void StartReload()
    {
        if (IsReloading || AmmoInMag == _data.magazineSize || ReserveAmmo <= 0)
            return;

        _hudUI.ShowReloadUI
        (
            AmmoInMag,
            _data.magazineSize
        );

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

        _hudUI.UpdateReloadAmmo
        (
            AmmoInMag,
            _data.magazineSize
        );
        _hudUI.HideReloadUI();
    }
}