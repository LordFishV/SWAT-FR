using UnityEngine;

// Anything the player can hold in their hands: guns, grenades, tools.
// The WeaponManager only knows about this class, so it can switch between any of them.
public abstract class Equippable : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private string _displayName;
    [SerializeField] private Texture2D _icon;

    // Falls back to the object's name if no display name is typed in
    public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
    public Texture2D Icon => _icon;

    // Uses left, shown as "x 2" on the HUD. -1 means "not counted" (hammers, ram).
    public virtual int Charges => -1;

    public abstract void UpdateItem(PlayerInput input, Transform aim);
}