using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;

    private PlayerInputActions _inputActions;

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
    }

    private void OnDestroy()
    {
        _inputActions.Dispose();
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
            LookRotation    = playerCamera.transform.rotation,
            Move            = input.Move.ReadValue<Vector2>(),
            Look            = input.Look.ReadValue<Vector2>(),
            Jump            = input.Jump.WasPressedThisFrame(),
            JumpSustain     = input.Jump.IsPressed(),
            Run             = input.Run.WasPressedThisFrame()
                                ? RunInput.Toggle
                                : RunInput.None,

            Crouch          = input.Crouch.WasPressedThisFrame()
                                ? CrouchInput.Toggle
                                : CrouchInput.None,

            Prone           = input.Prone.WasPressedThisFrame()
                                ? ProneInput.Toggle
                                : ProneInput.None
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

        #if UNITY_EDITOR
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Teleport(hit.point);
            }
        }
        #endif
    }

    public void Teleport(Vector3 position)
    {
        playerCharacter.SetPosition(position);
    }
}