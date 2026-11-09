using UnityEngine;
using UnityEngine.InputSystem;

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

    private Equippable _primary;
    private Equippable _secondary;
    private Equippable[] _gadgets;
    private int _gadgetIndex = -1;

    public void Initialise()
    {
        _primary = Spawn(_primaryPrefab);
        _secondary = Spawn(_secondaryPrefab);

        _gadgets = new Equippable[_gadgetPrefabs.Length];
        for (int i = 0; i < _gadgets.Length; i++)
            _gadgets[i] = Spawn(_gadgetPrefabs[i]);

        Equip(_primary);
    }

    public void UpdateWeapons(PlayerInput input, Transform aim)
    {
        if (Current == null) return;

        Current.UpdateItem(input, aim);

        if (input.Aim)
        {
            _holder.transform.localPosition = new Vector3(0, -0.4f, 1f);
            _holder.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            _holder.transform.localPosition = new Vector3(0.6f, -0.4f, 1f);
            _holder.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
    }

    public void SelectPrimary() => Equip(_primary);
    public void SelectSecondary() => Equip(_secondary);

    public void CycleGadget()
    {
        if (_gadgets.Length == 0)
            return;

        _gadgetIndex = (_gadgetIndex + 1) % _gadgets.Length;
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