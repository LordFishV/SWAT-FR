using System.Collections;
using UnityEngine;

/// <summary>
/// Runtime behaviour for ONE gun. Reads its stats from a GunData asset and
/// keeps its own mutable state (ammo, cooldown, fire mode).
/// Put this on the gun's GameObject (e.g. under the camera / view model).
/// Note: no "using System;" here, so Random means UnityEngine.Random.
/// </summary>
public class Gun : Equippable
{
    [SerializeField] private GunData _data;

    // Read-only state for HUD, animation and the camera to use
    public GunData Data => _data;
    public int AmmoInMag { get; private set; }
    public int ReserveAmmo { get; private set; }
    public WeaponFireMode FireMode { get; private set; }
    public GunState State { get; private set; } = GunState.Idle;

    // Events keep the gun decoupled from camera, audio, VFX and UI
    public event System.Action<Vector2> Recoiled;   // x = pitch up, y = yaw (degrees)
    public event System.Action Fired;
    public event System.Action ReloadStarted;
    public event System.Action AmmoChanged;
    public event System.Action ReloadFinished;
    public event System.Action DryFired;            // trigger pulled on an empty magazine
    public event System.Action<Vector3, Vector3, bool> BulletTraced;   // end point, hit normal, did it hit

    private int _fireModeIndex;
    private float _nextFireTime;
    private Coroutine _routine;   // the current burst or reload

    private void Awake()
    {
        if (_data != null)
            Initialise(_data);
    }

    // Stuff is reset when the weapon is holstered (SetActive(false)).
    // Coroutines stop automatically then, so clear the stale state.
    private void OnDisable()
    {
        _routine = null;
        State = GunState.Idle;
    }

    /// <summary>Call this to assign or swap a GunData at runtime (e.g. from a loadout).</summary>
    public void Initialise(GunData data)
    {
        _data = data;
        AmmoInMag = data.magazineSize;
        ReserveAmmo = data.startingReserve;
        _fireModeIndex = 0;
        FireMode = data.fireModes[0];
        State = GunState.Idle;
        _nextFireTime = 0f;
        AmmoChanged?.Invoke();
    }

    // ---------- Equippable (called by PlayerLoadout) ----------

    public override float EquipTime => _data != null ? _data.equipTime : 0.3f;

    public override void UpdateItem(PlayerInput input, Transform aim) => UpdateGun(input, aim);

    /// <summary>Call once per frame while this gun is equipped. The loadout already does this.</summary>
    /// <param name="aim">Transform whose position/forward the shots use (the camera).</param>
    public void UpdateGun(PlayerInput input, Transform aim)
    {
        if (input.Reload)
            TryReload();

        // Full-auto fires while held. Semi and burst fire on the press.
        bool wantsFire = FireMode == WeaponFireMode.FullAuto
            ? input.FireHeld
            : input.FirePressed;

        if (!wantsFire)
            return;

        // A shotgun that is loading shell by shell can be interrupted to shoot
        if (State == GunState.Reloading && _data.reloadOneRoundAtATime && AmmoInMag > 0)
            CancelReload();

        if (FireMode == WeaponFireMode.Burst)
            TryStartBurst(aim, input.Aim);
        else
            TryFire(aim, input.Aim);
    }

    /// <summary>Cycle through semi, burst and full audio fire modes. This dependent on what type of fire modes the weapon accualy has.</summary>
    public void CycleFireMode()
    {
        if (_data.fireModes.Length < 2 || State != GunState.Idle)
            return;

        _fireModeIndex = (_fireModeIndex + 1) % _data.fireModes.Length;
        FireMode = _data.fireModes[_fireModeIndex];
    }

    /// <summary>Add ammo in gun to reserve ammo so ammo in mag doesnt get lost.</summary>
    public void AddReserveAmmo(int amount)
    {
        ReserveAmmo += amount;
        AmmoChanged?.Invoke();
    }

    // ---------- Firing ----------

    /// <summary>Attempt to fire weapon if not reloading and enough time has passed since last shot fired.</summary>
    private bool TryFire(Transform aim, bool aiming)
    {
        if (State == GunState.Reloading || Time.time < _nextFireTime)
            return false;

        if (AmmoInMag <= 0)
        {
            // Rate-limit the click so holding full-auto doesn't spam it
            _nextFireTime = Time.time + _data.FireInterval;
            DryFired?.Invoke();
            TryReload();   // click on empty starts a reload
            return false;
        }

        _nextFireTime = Time.time + _data.FireInterval;
        AmmoInMag--;
        Shoot(aim, aiming);
        return true;
    }

    /// <summary>Attempt to start the birst fire coroutine if not idle and enough time has passed since last shot fired.</summary>
    private void TryStartBurst(Transform aim, bool aiming)
    {
        if (State != GunState.Idle || Time.time < _nextFireTime)
            return;

        _routine = StartCoroutine(BurstRoutine(aim, aiming));
    }

    /// <summary>Shoots repetitivly for burst count.</summary>
    private IEnumerator BurstRoutine(Transform aim, bool aiming)
    {
        State = GunState.Shooting;

        for (int i = 0; i < _data.burstCount; i++)
        {
            if (!TryFire(aim, aiming))
                break;   // ran dry mid-burst
            yield return new WaitForSeconds(_data.FireInterval);
        }

        _nextFireTime = Mathf.Max(_nextFireTime, Time.time + _data.burstCooldown);
        State = GunState.Idle;
        _routine = null;
    }

    /// <summary>Fire shot and deal damage to hit target if applicable.</summary>
    private void Shoot(Transform aim, bool aiming)
    {
        float spread = aiming ? _data.adsSpread : _data.hipSpread;

        // One ray per pellet: pistols/rifles/LMGs/snipers use 1, shotguns use many
        for (int i = 0; i < _data.pellets; i++)
        {
            Vector3 direction = GetSpreadDirection(aim, spread);

            if (Physics.Raycast(aim.position, direction, out RaycastHit hit,
                    _data.range, _data.hitMask, QueryTriggerInteraction.Ignore))
            {
                float falloff = _data.damageFalloff.Evaluate(hit.distance / _data.range);

                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null)
                    target.ApplyDamage(_data.damage * falloff, gameObject);

                Debug.DrawLine(aim.position, hit.point, Color.red, 0.5f);
                BulletTraced?.Invoke(hit.point, hit.normal, true);
            }
            else
            {
                Vector3 end = aim.position + direction * _data.range;
                Debug.DrawLine(aim.position, end, Color.yellow, 0.5f);
                BulletTraced?.Invoke(end, -direction, false);
            }
        }

        Recoiled?.Invoke(new Vector2(_data.recoilUp,
            Random.Range(-_data.recoilSide, _data.recoilSide)));
        Fired?.Invoke();
        AmmoChanged?.Invoke();
    }

    /// <summary>Random direction inside the boundary of a cone around the aim forward direction.</summary>
    private static Vector3 GetSpreadDirection(Transform aim, float spreadDegrees)
    {
        Vector2 offset = Random.insideUnitCircle * Mathf.Tan(spreadDegrees * Mathf.Deg2Rad);
        Vector3 local = new Vector3(offset.x, offset.y, 1f);
        return (aim.rotation * local).normalized;
    }

    // ---------- Reloading ----------

    /// <summary>Attempt to reload if ammo in mag is greater then or equal to mag size or reserve ammo.</summary>
    public bool TryReload()
    {
        if (State != GunState.Idle)
            return false;
        if (AmmoInMag >= _data.magazineSize || ReserveAmmo <= 0)
            return false;

        _routine = StartCoroutine(ReloadRoutine());
        return true;
    }

    /// <summary>Reload current gun and check if it reloads one bullet at a time or the whole mag.</summary>
    private IEnumerator ReloadRoutine()
    {
        State = GunState.Reloading;
        ReloadStarted?.Invoke();

        if (_data.reloadOneRoundAtATime)
        {
            // Shotgun: one shell per step
            while (AmmoInMag < _data.magazineSize && ReserveAmmo > 0)
            {
                yield return new WaitForSeconds(_data.roundReloadTime);
                AmmoInMag++;
                ReserveAmmo--;
                AmmoChanged?.Invoke();
            }
        }
        else
        {
            // Everything else: wait, then swap in a magazine's worth
            yield return new WaitForSeconds(_data.reloadTime);
            int loaded = Mathf.Min(_data.magazineSize - AmmoInMag, ReserveAmmo);
            AmmoInMag += loaded;
            ReserveAmmo -= loaded;
            AmmoChanged?.Invoke();
        }

        State = GunState.Idle;
        _routine = null;
        ReloadFinished?.Invoke();
    }

    /// <summary>Stop the reload coroutine and set gun state to Idle.</summary>
    private void CancelReload()
    {
        if (_routine != null)
            StopCoroutine(_routine);
        _routine = null;
        State = GunState.Idle;
    }
}