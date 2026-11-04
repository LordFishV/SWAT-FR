using UnityEngine;

// Global Enums
public enum  GunState
{
    Idle, Shooting, Reloading, ChangingFireMode, Inspecting, SwitchingWeapon, Aiming
}

public enum WeaponType
{
    None, Pistol, Rifle, Shotgun, Sniper, SMG, LMG
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



// Global Interfaces
public interface IHealable
{
    void ApplyHeal(float amount, GameObject affectingObject);
}

public interface IDamageable
{
    void ApplyDamage(float amount, GameObject affectingObject);
}

public interface IInteractable
{
    void Interact(GameObject interactingObject);
    void OnHoverEnter(GameObject hoveringObject);
    void OnHoverExit(GameObject hoveringObject);
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
}
