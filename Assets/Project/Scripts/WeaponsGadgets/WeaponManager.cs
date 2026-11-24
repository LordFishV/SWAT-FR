using System.Collections.Generic;
using UnityEngine;

// Holds the player's primary, secondary and gadgets, and switches between them.
// Put it on the Player. Assign the Holder (an empty object under the camera)
// and drag your weapon prefabs into there slots.
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
    private int _validIndex;

    public void Initialise() {
        _primary = Spawn(_primaryPrefab);
        _secondary = Spawn(_secondaryPrefab);

        foreach (Equippable prefab in _gadgetPrefabs) {
            Equippable gadget = Spawn(prefab);
            if (gadget != null)
                _gadgets.Add(gadget);
        }

        Equip(_primary);
    }

    public void UpdateWeapons(PlayerInput input, Transform aim) {
        if (Current == null)
            return;

        Current.UpdateItem(input, aim);
    }

    public void SelectPrimary() {
        Equip(_primary);
    }
    public void SelectSecondary() {
        Equip(_secondary);
    }
    public void SelectGadget() {
        if (_gadgets.Count == 0 || _validIndex == -1) return;

        int startIndex = Mathf.Max(0, _gadgetIndex);
        int validIndex = FindNextValidGadget(startIndex);

        if (validIndex != -1) {
            _gadgetIndex = _validIndex;
            Equip(_gadgets[_gadgetIndex]);
        }
    }

    /// <summary>Cycle to next valid gadget</summary>
    public void CycleGadget() {
        int nextValidIndex = FindNextValidGadget(_gadgetIndex + 1);
        if (nextValidIndex == -1)
            return;

        _gadgetIndex = nextValidIndex;
        Equip(_gadgets[_gadgetIndex]);
    }

    /// <summary>Find the next valid gadget </summary>
    /// <param name="startIndex">The position to start at in the gadget list</param>
    /// <returns>returns a valid index if there is one, if not then -1</returns>
    private int FindNextValidGadget(int startIndex) {
        // To find the the next possable slot a minimum of 4 steps is needed.
        for (int i = 0; i < _gadgets.Count; i++) {
            int index = (startIndex + i) % _gadgets.Count;
            Equippable gadget = _gadgets[index];

            // Early exit. If the gadget is usable then the player can access it
            if (gadget != null && gadget.Charges != 0)
                return index;
        }
        return -1; // if not set to -1 as there is no valid Index
    }

    // Every item is spawned once and hidden. Ammo and counts are kept when you switch.
    private Equippable Spawn(Equippable prefab) {
        if (prefab == null)
            return null;

        Equippable item = Instantiate(prefab, _holder);
        item.gameObject.SetActive(false);
        return item;
    }

    private void Equip(Equippable item) {
        if (item == null || item == Current)
            return;

        if (Current != null)
            Current.gameObject.SetActive(false);

        Current = item;
        Current.gameObject.SetActive(true);
    }
}