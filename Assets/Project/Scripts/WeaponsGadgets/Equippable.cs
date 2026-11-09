using UnityEngine;
using UnityEngine.InputSystem;

// Anything the player can hold in their hands: guns, grenades, tools.
// The WeaponManager only knows about this class, so it can switch between any of them.
public abstract class Equippable : MonoBehaviour
{
    public abstract void UpdateItem(PlayerInput input, Transform aim);
}