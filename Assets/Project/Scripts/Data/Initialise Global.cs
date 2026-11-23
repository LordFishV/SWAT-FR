using UnityEngine;

// Global Enums
/// <summary>Holds the current gun state (Shooting/Aiming/SwitchingWeapoon)</summary>
public enum  GunState {
    Idle, Shooting, Reloading, ChangingFireMode, Inspecting, SwitchingWeapon, Aiming
}
/// <summary>The weapon type (Rifles/Shotguns/Pistols)</summary>
public enum WeaponType {
    None, Pistol, Rifle, Shotgun, Sniper, SMG, LMG, Melee, Grenade, Gadget
}

/// <summary>The weapon slot the weapon manager can put the weapon inside</summary>
public enum WeaponSlot {
    None, Primary, Secondary, Melee, Grenade
}

/// <summary>Fire type (SemiAuto/FullAuto/Burst). Might not be implemented as just a simple bool is good enough for semiauto and fullauto.</summary>
public enum WeaponFireMode {
    SemiAuto, FullAuto, Burst
}

/// <summary>Current player stance</summary>
public enum Stance {
    Stand, Run, Crouch
}

/// <summary>Player dead or alive state</summary>
public enum PlayerState {
    Alive, Dead
}

/// <summary>The current AI state</summary>
public enum AIState {
    Idle, Patrol, Chase, Attack, Flee, Dead
}

/// <summary>How something was breached. Flags, so a door can allow several methods</summary>
[System.Flags]
public enum BreachMethod {
    None = 0,
    Hammer = 1,
    Ram = 2,
    Explosive = 4
}



// Global Interfaces
/// <summary>If object has this interface then the object can take damage")]</summary>
public interface IDamageable {
    /// <summary>Apply damage to a target containing this interface</summary>
    /// <param name="damageAmount">The damage that will be applied</param>
    void ApplyDamage(float damageAmount);
}

/// <summary>If an script contains this it allows the player to interact with it</summary>
public interface IInteractable {
    /// <summary>The interaction action that will happen (Open doors, or arrest suspects)</summary>
    /// <param name="interactingObject">The object thats interacting with this object (Player)</param>
    void Interact(GameObject interactingObject);
    /// <summary>When the player starts to hover over an object</summary>
    /// <param name="interactingObject">The object thats hovering over with this object (Player)</param>
    void OnHoverEnter(GameObject interactingObject);
    /// <summary>When the player stops hovering over an object</summary>
    /// <param name="interactingObject">The object thats stopped hovering over with this object (Player)</param>
    void OnHoverExit(GameObject interactingObject);
}

public interface IBlindable {
    /// <summary>Apply a blidning effect to all affected targets</summary>
    /// <param name="info">The information from the blind struct for this flashbang</param>
    void ApplyBlind(in BlindInfo info);
}

/// <summary>Anything a hammer, ram or explosive can break: doors, barricades</summary>
public interface IBreachable {
    void ApplyBreach(in BreachInfo info);
}

// Global Structs
public struct CharacterState {
    /// <summary>Holds if the Character on the ground this frame (For Gravity)</summary>
    public bool Grounded;
    /// <summary>Holds the Characters current state</summary>
    public Stance Stance;
    /// <summary>Holds the Characters velocity</summary>
    public Vector3 Velocity;
    /// <summary>Holds the current acceleration this frame</summary>
    public Vector3 Acceleration;
}

public struct PlayerInput {
    /// <summary>Holds the Look Rotation of the camera</summary>
    public Quaternion LookRotation;
    /// <summary>Holds the Move input from the keyboard</summary>
    public Vector2 Move;
    /// <summary>Holds the Look input from the mouse</summary>
    public Vector2 Look;
    /// <summary>Holds the Run type (Hold/Toggle)</summary>
    public ButtonState Run;
    /// <summary>Holds the Crouch type (Hold/Toggle)</summary>
    public ButtonState Crouch;
    /// <summary>Holds the Aim input from the Mouse</summary>
    public bool Aim;
    /// <summary>Holds the Interact input from the keyboard</summary>
    public bool Interact;
    /// <summary>Holds the Reload input from the keyboard</summary>
    public bool Reload;
    /// <summary>Holds the Fire Pressed input from the Mouse (SemiAuto)</summary>
    public bool FirePressed;
    /// <summary>FireHeld holds the value if Fire Pressed has be down for longer then a frame (FullAuto)</summary>
    public bool FireHeld;
}

public struct BlindInfo {
    /// <summary>where the flash went off</summary>
    public Vector3 Position;
    /// <summary>How strong the flashbang flash is. 0 to 1, how strong the white-out is</summary>
    public float Intensity;
    /// <summary>The duration blinded for in seconds</summary>
    public float Duration;
    /// <summary>Who threw the flashbang</summary>
    public GameObject Source;
}

public struct BreachInfo {
    /// <summary>The method to breach through the surface (Hammer, Ram, Explosive)</summary>
    public BreachMethod Method;
    /// <summary>The amount of force applied to the breachable surface</summary>
    public float Force;
    /// <summary>The point where the breach landed</summary>
    public Vector3 Point;
    /// <summary>which way the force was pushing</summary>
    public Vector3 Direction;
    /// <summary>Who threw the flashbang</summary>
    public GameObject Source;
}
