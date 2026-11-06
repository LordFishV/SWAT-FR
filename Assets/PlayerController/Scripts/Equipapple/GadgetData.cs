using UnityEngine;

/// <summary>
/// Pure DATA for one kind of gadget (frag grenade, flashbang, ...).
/// Create assets via Right-click > Create > Weapons > Gadget Data.
/// </summary>
[CreateAssetMenu(fileName = "NewGadget", menuName = "Weapons/Gadget Data")]
public class GadgetData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "New Gadget";
    [Tooltip("The held-gadget prefab (model + Gadget script) the loadout spawns.")]
    public GameObject heldPrefab;

    [Header("Loadout")]
    [Tooltip("How much of the loadout's gadget capacity this takes up.")]
    [Min(1)] public int slotCost = 1;
    [Min(0)] public int startingCount = 2;
    public float equipTime = 0.4f;

    [Header("Throwing")]
    [Tooltip("Spawned and thrown when used. Needs a Rigidbody. Fuse/explosion logic lives on this prefab.")]
    public GameObject projectilePrefab;
    public float throwSpeed = 12f;
    public float useCooldown = 0.8f;
}