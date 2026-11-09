using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;

    [Header("Weapons")]
    [SerializeField] private WeaponManager weapons;
    [SerializeField] private Camera viewCamera;     // the Main Camera, for aim zoom
    [SerializeField] private float hipFov = 80f;
    [SerializeField] private float aimFov = 55f;
    [SerializeField] private float fovResponse = 15f;

    private PlayerInputActions _inputActions;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        _inputActions = new PlayerInputActions();
        _inputActions.Enable();

        playerCharacter.Initilise();
        playerCamera.Initilise(playerCharacter.GetCameraAnchor());

        cameraSpring.Initilise();
        cameraLean.Initilise();

        weapons.Initialise();
    }

    private void OnDestroy()
    {
        _inputActions.Dispose();
    }

    void Update()
    {
        PlayerInputActions.GameplayActions input = _inputActions.Gameplay;
        float deltaTime = Time.deltaTime;

        Transform cameraAnchor = playerCharacter.GetCameraAnchor();
        CharacterState state = playerCharacter.GetState();

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
            Interact = input.Interact.WasPressedThisFrame(),

            FirePressed = input.Fire.WasPressedThisFrame(),
            FireHeld = input.Fire.IsPressed(),
            Aim = input.Aim.IsPressed(),
            Reload = input.Reload.WasPressedThisFrame()
        };

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

        // Weapons run after the camera so shots use this frame's look direction
        if (input.EquipPrimary.WasPressedThisFrame())
            weapons.SelectPrimary();
        if (input.EquipSecondary.WasPressedThisFrame())
            weapons.SelectSecondary();
        if (input.CycleGadget.WasPressedThisFrame())
            weapons.CycleGadget();

        weapons.UpdateWeapons(playerInput, playerCamera.transform);

        playerCharacter.UpdateInteract(playerCamera);

        // Zoom in while aiming a gun
        bool aiming = playerInput.Aim && weapons.Current is Gun;
        UpdateFov(aiming, deltaTime);

#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit))
                Teleport(hit.point);
        }
#endif
    }

    private void UpdateFov(bool aiming, float deltaTime)
    {
        if (viewCamera == null)
            return;

        float targetFov = aiming ? aimFov : hipFov;
        viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, targetFov, 1f - Mathf.Exp(-fovResponse * deltaTime));
    }

    public void Teleport(Vector3 position)
    {
        playerCharacter.SetPosition(position);
    }
}