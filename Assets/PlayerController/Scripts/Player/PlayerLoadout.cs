using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the player's equipment: one primary, one secondary and a list of gadgets.
/// Spawns each item from its prefab, keeps them all alive (so ammo and counts persist),
/// and switches which one is active and in the player's hands.
/// Put this on the Player (or the weapon holder) and assign a Holder transform.
/// </summary>
public class PlayerLoadout : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Empty transform (usually under the camera) that items are spawned into.")]
    [SerializeField] private Transform _holder;
    [SerializeField] private float _holsterTime = 0.15f;

    [Header("Loadout")]
    [SerializeField] private GunData _primary;
    [SerializeField] private GunData _secondary;
    [SerializeField] private List<GadgetData> _gadgets = new List<GadgetData>();
    [Tooltip("Total slot cost of all gadgets must fit inside this.")]
    [SerializeField] private int _gadgetCapacity = 4;

    /// <summary>Raised after a new item becomes the active one.</summary>
    public event System.Action<Equippable> ItemEquipped;

    public Equippable Current { get; private set; }
    public Gun CurrentGun => Current as Gun;
    public bool IsSwitching { get; private set; }

    private Equippable _primaryItem;
    private Equippable _secondaryItem;
    private readonly List<Equippable> _gadgetItems = new List<Equippable>();
    private Equippable _previous;
    private int _gadgetIndex;

    /// <summary>Call once from Player.Start. Subscribe to ItemEquipped BEFORE calling.</summary>
    public void Initialise()
    {
        _primaryItem = _primary != null ? Spawn(_primary.weaponPrefab, _primary.displayName) : null;
        _secondaryItem = _secondary != null ? Spawn(_secondary.weaponPrefab, _secondary.displayName) : null;

        int usedSlots = 0;
        foreach (GadgetData data in _gadgets)
        {
            if (data == null)
                continue;

            if (usedSlots + data.slotCost > _gadgetCapacity)
            {
                Debug.LogWarning($"Loadout: no room for '{data.displayName}' ({data.slotCost} slots), skipped.");
                continue;
            }

            Equippable gadget = Spawn(data.heldPrefab, data.displayName);
            if (gadget != null)
            {
                _gadgetItems.Add(gadget);
                usedSlots += data.slotCost;
            }
        }

        // Start with the primary in hand, with no draw delay at spawn
        Equippable start = _primaryItem != null ? _primaryItem : _secondaryItem;
        Equip(start, instant: true);
    }

    /// <summary>Call every frame from Player.Update with the camera as the aim transform.</summary>
    public void UpdateLoadout(PlayerInput input, Transform aim)
    {
        // Items can't be used while one is being put away or brought up
        if (IsSwitching || Current == null)
            return;

        Current.UpdateItem(input, aim);
    }
    /// <summary>Quickly swap to primary weapon in loadout.</summary>
    public void SelectPrimary() => Equip(_primaryItem);
    /// <summary>Quickly swap to secondary weapon in loadout.</summary>
    public void SelectSecondary() => Equip(_secondaryItem);
    /// <summary>Quickly swap to previous equiped item in loadout.</summary>
    public void QuickSwap() => Equip(_previous);

    /// <summary>Cycle through gadgets in loadout.</summary>
    public void CycleGadget()
    {
        if (_gadgetItems.Count == 0)
            return;

        int heldIndex = _gadgetItems.IndexOf(Current);
        _gadgetIndex = heldIndex >= 0 ? (heldIndex + 1) % _gadgetItems.Count : _gadgetIndex;
        Equip(_gadgetItems[_gadgetIndex]);
    }

    /// <summary>Create instances of a prefab to be used during runtime.</summary>
    private Equippable Spawn(GameObject prefab, string label)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"Loadout: '{label}' has no prefab assigned.");
            return null;
        }

        GameObject instance = Instantiate(prefab, _holder);
        Equippable item = instance.GetComponent<Equippable>();
        if (item == null)
        {
            Debug.LogWarning($"Loadout: prefab for '{label}' has no Gun or Gadget on its root.");
            Destroy(instance);
            return null;
        }

        // Everything starts holstered. Awake has already run, so ammo/counts are initialised.
        instance.SetActive(false);
        return item;
    }

    /// <summary>Equip desired item only if item is not null or current item or isnt already switching.</summary>
    private void Equip(Equippable item, bool instant = false)
    {
        if (item == null || item == Current || IsSwitching)
            return;

        StartCoroutine(SwitchRoutine(item, instant));
    }

    /// <summary>Traverses through a two item list like structure to save current as previews item and then next item to current over a set time.</summary>
    private IEnumerator SwitchRoutine(Equippable next, bool instant)
    {
        IsSwitching = true;

        Equippable old = Current;
        if (old != null)
        {
            old.OnUnequipped();
            if (!instant)
                yield return new WaitForSeconds(_holsterTime);

            // Deactivating also cancels any reload in progress on that gun
            old.gameObject.SetActive(false);
            _previous = old;
        }

        Current = next;
        next.gameObject.SetActive(true);
        next.OnEquipped();
        ItemEquipped?.Invoke(next);

        if (!instant)
            yield return new WaitForSeconds(next.EquipTime);

        IsSwitching = false;
    }
}