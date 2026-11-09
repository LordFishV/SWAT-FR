using System.Collections.Generic;
using UnityEngine;

// Holds the player's primary, secondary and gadgets, and switches between them.
// Put it on the Player. Assign the Holder (an empty object under the camera)
// and drag your weapon PREFABS into the slots (from the Project window).
public class WeaponManager : MonoBehaviour
{
    [SerializeField] private Transform _holder;
    [SerializeField] private Equippable _primaryPrefab;
    [SerializeField] private Equippable _secondaryPrefab;
    [SerializeField] private Equippable[] _gadgetPrefabs;

    public Equippable Current { get; private set; }

    // For the HUD
    public IReadOnlyList<Equippable> Gadgets => _gadgets;
    public int CurrentGadgetIndex => _gadgets.IndexOf(Current);   // -1 if not holding a gadget

    private Equippable _primary;
    private Equippable _secondary;
    private readonly List<Equippable> _gadgets = new List<Equippable>();
    private int _gadgetIndex = -1;

    public void Initialise()
    {
        _primary = Spawn(_primaryPrefab);
        _secondary = Spawn(_secondaryPrefab);

        foreach (Equippable prefab in _gadgetPrefabs)
        {
            Equippable gadget = Spawn(prefab);
            if (gadget != null)
                _gadgets.Add(gadget);
        }

        Equip(_primary);
    }

    public void UpdateWeapons(PlayerInput input, Transform aim)
    {
        if (Current != null)
            Current.UpdateItem(input, aim);
    }

    public void SelectPrimary() => Equip(_primary);
    public void SelectSecondary() => Equip(_secondary);

    public void CycleGadget()
    {
        if (_gadgets.Count == 0)
            return;

        _gadgetIndex = (_gadgetIndex + 1) % _gadgets.Count;
        Equip(_gadgets[_gadgetIndex]);
    }

    // Every item is spawned once and hidden. Ammo and counts are kept when you switch.
    private Equippable Spawn(Equippable prefab)
    {
        if (prefab == null)
            return null;

        Equippable item = Instantiate(prefab, _holder);
        item.gameObject.SetActive(false);
        return item;
    }

    private void Equip(Equippable item)
    {
        if (item == null || item == Current)
            return;

        if (Current != null)
            Current.gameObject.SetActive(false);

        Current = item;
        Current.gameObject.SetActive(true);
    }
}