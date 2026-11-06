using UnityEngine;

/// <summary>
/// Anything the player can hold in their hands: guns, grenades, tools.
/// The loadout only knows about this class, so it can swap between a rifle
/// and a flashbang with exactly the same code.
/// </summary>
public abstract class Equippable : MonoBehaviour
{
    /// <summary>Seconds spent bringing this item up before it can be used.</summary>
    public virtual float EquipTime => 0.3f;

    /// <summary>Called every frame while this item is in hand and not mid-switch.</summary>
    public abstract void UpdateItem(PlayerInput input, Transform aim);

    /// <summary>Called when the item becomes the active one.</summary>
    public virtual void OnEquipped() { }

    /// <summary>Called when the player starts putting the item away.</summary>
    public virtual void OnUnequipped() { }
}