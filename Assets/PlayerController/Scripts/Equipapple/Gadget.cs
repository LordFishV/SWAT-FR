using UnityEngine;

/// <summary>
/// Runtime behaviour for a throwable gadget. Like Gun.cs, it reads its stats
/// from a data asset (GadgetData) and keeps its own state (how many are left).
/// Put this on the root of the held-gadget prefab and assign its GadgetData.
/// </summary>
public class Gadget : Equippable
{
    [SerializeField] private GadgetData _data;

    public GadgetData Data => _data;
    public int Count { get; private set; }

    public event System.Action CountChanged;

    private float _nextUseTime;

    public override float EquipTime => _data != null ? _data.equipTime : 0.3f;

    private void Awake()
    {
        if (_data != null)
            Count = _data.startingCount;
    }

    /// <summary>Update item currently in hand.</summary>
    public override void UpdateItem(PlayerInput input, Transform aim)
    {
        // Left mouse (the fire button) uses the gadget
        if (input.FirePressed)
            TryUse(aim);
    }

    /// <summary>.</summary>
    public void AddCount(int amount)
    {
        Count += amount;
        CountChanged?.Invoke();
    }

    /// <summary>Try use gadget if player has in inventory or enough time has passed from last use.</summary>
    private void TryUse(Transform aim)
    {
        if (Count <= 0 || Time.time < _nextUseTime)
            return;

        _nextUseTime = Time.time + _data.useCooldown;
        Count--;

        if (_data.projectilePrefab != null)
        {
            // Spawn slightly in front of the camera so it doesn't start inside the player
            Vector3 spawnPosition = aim.position + aim.forward * 0.5f;
            GameObject projectile = Instantiate(_data.projectilePrefab, spawnPosition, aim.rotation);

            if (projectile.TryGetComponent(out Rigidbody body))
                body.linearVelocity = aim.forward * _data.throwSpeed;
        }

        CountChanged?.Invoke();
    }
}