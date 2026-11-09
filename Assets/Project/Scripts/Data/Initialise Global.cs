using UnityEngine;

// Global Enums
public enum  GunState
{
    Idle, Shooting, Reloading, ChangingFireMode, Inspecting, SwitchingWeapon, Aiming
}

public enum WeaponType
{
    None, Pistol, Rifle, Shotgun, Sniper, SMG, LMG, Melee, Grenade, Gadget
}

public enum WeaponSlot
{
    None, Primary, Secondary, Melee, Grenade
}

public enum WeaponFireMode
{
    SemiAuto, FullAuto, Burst
}
public enum RunInput
{
    None, Toggle, Hold
}
public enum CrouchInput
{
    None, Toggle, Hold
}
public enum ProneInput
{
    None, Toggle, Hold
}

public enum Stance
{
    Stand, Run, Crouch, Prone, Slide
}

public enum PlayerState
{
    Alive, Dead, Respawning
}

public enum PickupType
{
    Ammo, Health, Armor, Weapon, Grenade
}

public enum PickupMethod
{
    Instant, Interactable, Timed
}

public enum AIState
{
    Idle, Patrol, Chase, Attack, Flee, Dead
}

/// <summary>How something was breached. Flags, so a door can allow several methods.</summary>
[System.Flags]
public enum BreachMethod
{
    None = 0,
    Hammer = 1,
    Ram = 2,
    Explosive = 4
}



// Global Interfaces
public interface IHealable
{
    void ApplyHeal(float healAmount, GameObject source);
}

public interface IDamageable
{
    void ApplyDamage(float damageAmount, GameObject source);
}

/// <summary>How the player can Interact with objects: On click, hover enter/exit.</summary>
public interface IInteractable
{
    /// <summary>What happens when interactcting: Open door, Pickup item,</summary>
    void Interact(GameObject interactingObject);
    void OnHoverEnter(GameObject hoveringObject);
    void OnHoverExit(GameObject hoveringObject);
}

/// <summary>Anything a flashbang can affect: the player, enemies, civilians.</summary>
public interface IBlindable
{
    void ApplyBlind(in BlindInfo info);
}

/// <summary>Anything a hammer, ram or explosive can break: doors, barricades.</summary>
public interface IBreachable
{
    void ApplyBreach(in BreachInfo info);
}

// Global Structs
public struct CharacterState
{
    public bool Grounded;
    public Stance Stance;
    public Vector3 Velocity;
    public Vector3 Acceleration;
}

public struct PlayerInput
{
    public Quaternion LookRotation;
    public Vector2 Move;
    public Vector2 Look;
    public bool Jump;
    public bool JumpSustain;
    public RunInput Run;
    public CrouchInput Crouch;
    public ProneInput Prone;
    public bool Aim;
    public bool Interact;
    public bool Reload;
    public bool FirePressed;   // semi-auto and burst
    public bool FireHeld;      // full-auto
}

public struct BlindInfo
{
    public Vector3 Position;    // where the flash went off
    public float Intensity;     // 0 to 1, how strong the white-out is
    public float Duration;      // seconds
    public GameObject Source;   // who threw it
}

public struct BreachInfo
{
    public BreachMethod Method;
    public float Force;         // how hard the hit was
    public Vector3 Point;       // where it landed
    public Vector3 Direction;   // which way the force was pushing
    public GameObject Source;
}
