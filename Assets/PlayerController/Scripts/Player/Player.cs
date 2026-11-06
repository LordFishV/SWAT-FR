using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;

    [Header("Weapons")]
    [SerializeField] private PlayerLoadout loadout;
    [Tooltip("The actual Camera component (Main Camera), used for aim-down-sights zoom.")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private float hipFov = 80f;
    [SerializeField] private float fovResponse = 15f;

    private PlayerInputActions _inputActions;
    private Gun _equippedGun;   // the gun currently in hand (null if holding a gadget)

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        _inputActions = new PlayerInputActions();
        _inputActions.Enable();

        playerCharacter.Initilise();
        playerCamera.Initilise(playerCharacter.GetCameraAnchor());

        cameraSpring.Initilise();
        cameraLean.Initilise();

        // Subscribe BEFORE Initialise, because Initialise equips the first item
        loadout.ItemEquipped += OnItemEquipped;
        loadout.Initialise();
    }

    private void OnDestroy()
    {
        if (loadout != null)
            loadout.ItemEquipped -= OnItemEquipped;
        if (_equippedGun != null)
            _equippedGun.Recoiled -= playerCamera.AddRecoil;

        _inputActions.Dispose();
    }

    // Moves the recoil subscription to whichever gun was just equipped
    private void OnItemEquipped(Equippable item)
    {
        if (_equippedGun != null)
            _equippedGun.Recoiled -= playerCamera.AddRecoil;

        _equippedGun = item as Gun;

        if (_equippedGun != null)
            _equippedGun.Recoiled += playerCamera.AddRecoil;
    }

    // Update is called once per frame
    void Update()
    {
        PlayerInputActions.GameplayActions input = _inputActions.Gameplay;
        float deltaTime = Time.deltaTime;

        // Get the camera anchor from the player character and pass it to the camera controller
        Transform cameraAnchor = playerCharacter.GetCameraAnchor();

        CharacterState state = playerCharacter.GetState();

        // Get player input and pass it to the character controller
        PlayerInput playerInput = new PlayerInput
        {
            LookRotation = playerCamera.transform.rotation,
            Move = input.Move.ReadValue<Vector2>(),
            Look = input.Look.ReadValue<Vector2>(),
            Jump = input.Jump.WasPressedThisFrame(),
            JumpSustain = input.Jump.IsPressed(),
            Run = input.Run.WasPressedThisFrame()
                                ? RunInput.Toggle
                                : RunInput.None,

            Crouch = input.Crouch.WasPressedThisFrame()
                                ? CrouchInput.Toggle
                                : CrouchInput.None,

            Prone = input.Prone.WasPressedThisFrame()
                                ? ProneInput.Toggle
                                : ProneInput.None,

            // Weapon input
            FirePressed = input.Fire.WasPressedThisFrame(),
            FireHeld = input.Fire.IsPressed(),
            Aim = input.Aim.IsPressed(),
            Reload = input.Reload.WasPressedThisFrame()
        };

        // Update the camera's rotation and position based on the player's input, the camera anchor and the spring physics
        playerCamera.UpdateRotation(playerInput);
        playerCamera.UpdatePosition(cameraAnchor);
        cameraSpring.UpdateCameraSpring(deltaTime, cameraAnchor.up);
        cameraLean.UpdateLean
        (
            deltaTime,
            state.Stance is Stance.Slide,
            state.Acceleration,
            cameraAnchor.up
        );

        playerCharacter.UpdateInput(playerInput);
        playerCharacter.UpdateBody(deltaTime);

        // ---- Loadout ----
        if (input.EquipPrimary.WasPressedThisFrame())
            loadout.SelectPrimary();
        if (input.EquipSecondary.WasPressedThisFrame())
            loadout.SelectSecondary();
        if (input.CycleGadget.WasPressedThisFrame())
            loadout.CycleGadget();
        if (input.QuickSwap.WasPressedThisFrame())
            loadout.QuickSwap();

        // Runs AFTER the camera update so shots use this frame's look direction.
        // playerCamera.transform is the parent object, so spring and lean don't affect aim.
        loadout.UpdateLoadout(playerInput, playerCamera.transform);

        if (_equippedGun != null && !loadout.IsSwitching && input.ChangeFireMode.WasPressedThisFrame())
            _equippedGun.CycleFireMode();

        UpdateFov(playerInput.Aim && _equippedGun != null && !loadout.IsSwitching, deltaTime);

#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Teleport(hit.point);
            }
        }
#endif
    }

    /// <summary>Smoothly zoom to the equipped gun's ADS field of view while aiming</summary>
    private void UpdateFov(bool aiming, float deltaTime)
    {
        if (viewCamera == null)
            return;

        float targetFov = aiming ? _equippedGun.Data.adsFov : hipFov;
        viewCamera.fieldOfView = Mathf.Lerp
        (
            viewCamera.fieldOfView,
            targetFov,
            1f - Mathf.Exp(-fovResponse * deltaTime)
        );
    }

    /// <summary>Teleport player to spacific location they are looking at</summary>
    public void Teleport(Vector3 position)
    {
        playerCharacter.SetPosition(position);
    }
}