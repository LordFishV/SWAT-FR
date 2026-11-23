using KinematicCharacterController;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    // Serialize fields
    [Header("References")]
    [SerializeField] private KinematicCharacterMotor _motor;
    [SerializeField] private Transform _root;
    [SerializeField] private Transform _cameraAnchor;
    [Space]

    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 20f;
    [SerializeField] private float _runSpeed = 30f;
    [SerializeField] private float _crouchSpeed = 10f;
    [Space]

    [Header("Response")]
    [SerializeField] private float _walkHeightResponse = 25f;
    [SerializeField] private float _runHeightResponse = 30f;
    [SerializeField] private float _crouchHeightResponse = 15f;
    [SerializeField] private float _walkResponse = 25f;
    [SerializeField] private float _runResponse = 30f;
    [SerializeField] private float _crouchResponse = 20f;
    [Space]

    [Header("Physics")]
    [SerializeField] private float _gravity = 98.1f;

    [Header("Capsule Heights")]
    [SerializeField] private float _standHeight = 2f;
    [SerializeField] private float _runHeight = 2.2f;
    [SerializeField] private float _crouchHeight = 1f;
    [Space]

    [Header("Anchor Heights")]
    [Range(0f, 1f)]
    [SerializeField] private float _anchorStandHeight = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float _anchorRunHeight = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float _anchorCrouchHeight = 0.8f;
    [Space]

    [Header("Interaction")]
    [SerializeField] private float _interactionDistance = 3f;


    // States
    private CharacterState _state;
    private CharacterState _lastState;
    private CharacterState _tempState;

    // Private fields requested by the player input
    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private Stance _requestedStance = Stance.Stand;
    private bool _requestedInteraction;

    // Private fields not requested by the player input
    private Collider[] _overlapResults;

    public CharacterState State => _state;

    public void Initilise() {
        _state.Stance = Stance.Stand;
        _lastState = _state;

        _overlapResults = new Collider[8];

        _motor.CharacterController = this;
    }

    /// <summary>Update the players incoming inputs (Mouse and Keyboard only for now)</summary>
    public void UpdateInput(PlayerInput input) {
        _requestedRotation = input.LookRotation;
        // Convert the input move vector into a worldspace direction relative to the character's rotation
        _requestedMovement = new Vector3(input.Move.x, 0f, input.Move.y);
        // Clamp input movement to a maximum magnitude of 1, so that diagonal movement isn't faster
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);
        // Rotate input movement to be relative to character rotation
        _requestedMovement = input.LookRotation * _requestedMovement;

        Stance previousRequest = _requestedStance;

        // Handle stance input
        if (input.Run == RunInput.Toggle)
            _requestedStance = _requestedStance is Stance.Run ? Stance.Stand : Stance.Run;
        if (input.Crouch == CrouchInput.Toggle)
            _requestedStance = _requestedStance is Stance.Crouch ? Stance.Stand : Stance.Crouch;

        _requestedInteraction = input.Interact;
    }

    /// <summary>Updates player scale and responces depending on possable desired state</summary>
    public void UpdateBody(float deltaTime) {
        // Get the current height of the character's capsule collider
        float currentHeight = _motor.Capsule.height;

        // Calculate the normalized height of the character's capsule collider relative to the standing height
        float normalisedHeight = currentHeight / _standHeight;

        // Calculate the camera anchor height based on the current stance and the defined anchor heights
        float cameraAnchorHeight = currentHeight * (
            _state.Stance switch {
                Stance.Stand    => _anchorStandHeight,  // Use the defined anchor height for standing
                Stance.Run      => _anchorRunHeight,    // Use the defined anchor height for running
                Stance.Crouch   => _anchorCrouchHeight, // Use the defined anchor height for crouching
                _ => _anchorStandHeight                 // Default to the standing anchor height for any other stance
            }
        );

        float heightResponse = (
            _state.Stance switch {
                Stance.Stand        => _walkHeightResponse,   // Use the defined response for standing
                Stance.Run      => _runHeightResponse,    // Use the defined response for running
                Stance.Crouch   => _crouchHeightResponse, // Use the defined response for crouching
                _ => _walkHeightResponse                  // Default to the standing response for any other stance
            }
        );

        Vector3 rootAnchorScale = new Vector3(1f, normalisedHeight, 1f);

        // Update the camera anchor position based on the current stance
        _cameraAnchor.localPosition = Vector3.Lerp(
            a: _cameraAnchor.localPosition,
            b: new Vector3(0f, cameraAnchorHeight, 0f),
            t: 1f - Mathf.Exp(-heightResponse * deltaTime)
        );
        _root.localScale = Vector3.Lerp(
            a: _root.localScale,
            b: rootAnchorScale,
            t: 1f - Mathf.Exp(-heightResponse * deltaTime)
        );
    }

    /// <summary>Updates velocity for all movement states, idle, crouch, run, slide, jump</summary>
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _state.Acceleration = Vector3.zero;

        // If the character is grounded, set the velocity based on our movement input and speed
        if (_motor.GroundingStatus.IsStableOnGround)
        {
            // Project the movement direction onto the plane defined by the ground's normal
            Vector3 groundedMovement = _motor.GetDirectionTangentToSurface(
                direction: _requestedMovement,
                surfaceNormal: _motor.GroundingStatus.GroundNormal
            ) * _requestedMovement.magnitude;

            // Calculate the character's speed based on the current stance
            float speed = _state.Stance switch
            {
                Stance.Stand => _walkSpeed,      // Use walk speed when standing
                Stance.Run => _runSpeed,       // Use run speed when running
                Stance.Crouch => _crouchSpeed,    // Use crouch speed when crouching
                _ => _walkSpeed                     // Default to walk speed for any other stance
            };
            // Calculate the speed response based on the current stance
            float response = _state.Stance switch
            {
                Stance.Stand => _walkResponse,   // Use walk response when standing
                Stance.Run => _runResponse,    // Use run response when running
                Stance.Crouch => _crouchResponse, // Use crouch response when crouching
                _ => _walkResponse                  // Default to walk response for any other stance
            };

            // Set the current velocity to the calculated grounded movement
            Vector3 targetVelocity = groundedMovement * speed;
            Vector3 moveVelocity = Vector3.Lerp
            (
                a: currentVelocity,
                b: targetVelocity,
                t: 1f - Mathf.Exp(-response * deltaTime)
            );
            _state.Acceleration = moveVelocity - currentVelocity;

            currentVelocity = moveVelocity;
        }
        else {
            // gravity is multiplied by the jump sustain gravity factor if the player is holding the jump button and moving upwards
            float effectiveGravity = _gravity;
            float verticalSpeed = Vector3.Dot(currentVelocity, _motor.CharacterUp);
            // Apply gravity
            currentVelocity -= _motor.CharacterUp * effectiveGravity * deltaTime;
        }
    }

    /// <summary>Updates player rotation based on the projecton plane</summary>
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
        // Update the character's rotation based on the requested rotation depending on the character's up direction to the ground
        Vector3 forward = Vector3.ProjectOnPlane(
                vector: _requestedRotation * Vector3.forward,
                planeNormal: _motor.CharacterUp
            );

        if (forward != Vector3.zero)
            currentRotation = Quaternion.LookRotation(forward, _motor.CharacterUp);
    }

    public void UpdateInteract(PlayerCamera playerCamera)
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, _interactionDistance) && hit.collider.TryGetComponent(out IInteractable interactObj)) {
            IInteractable target = hit.collider.GetComponentInParent<IInteractable>();
            if (target != null) {
                ClearInteraction(target);
                target = interactObj;
                target.OnHoverEnter(gameObject);
            }

            if (_requestedInteraction) target.Interact(gameObject);
        }
    }
    private void ClearInteraction(IInteractable target) {
        if (target != null) target.OnHoverExit(gameObject);
        target = null;
    }

    public void BeforeCharacterUpdate(float deltaTime) {
        _tempState = _state;

        if (TryResizeCapsule(GetHeight(_requestedStance)))
            _state.Stance = _requestedStance;
        // If the capsule resize fails, the stance will remain unchanged, and the requested stance will be retried in the next frame
    }

    public void PostGroundingUpdate(float deltaTime) {
        if (!_motor.GroundingStatus.IsStableOnGround)
            _state.Stance = Stance.Crouch;
    }

    public void AfterCharacterUpdate(float deltaTime) {
        // Update the grounded state based on the motor's grounding status
        _state.Grounded = _motor.GroundingStatus.IsStableOnGround;
        // Update the velocity state based on the motor's current velocity [Debug]
        _state.Velocity = _motor.Velocity;

        Vector3 totalAcceleration = (_state.Velocity - _lastState.Velocity) / deltaTime;
        _state.Acceleration = Vector3.ClampMagnitude(_state.Acceleration, totalAcceleration.magnitude);
        // Update the last state to the current state for the next frame
        _lastState = _tempState;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
        _state.Acceleration = Vector3.ProjectOnPlane
        (
            vector: _state.Acceleration,
            planeNormal: hitNormal
        );
    }

    public bool IsColliderValidForCollisions(Collider coll) => true;

    public void OnDiscreteCollisionDetected(Collider hitCollider) { }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }

    /// <summary>Camera anchor game objects transform</summary>
    public Transform GetCameraAnchor() => _cameraAnchor;
    /// <summary>Current state</summary>
    public CharacterState GetState() => _state;
    /// <summary>Last state</summary> 
    public CharacterState GetLastState() => _lastState;

    public void SetPosition(Vector3 position, bool killVelocity = true) {
        _motor.SetPosition(position);
        if (killVelocity)
            _motor.BaseVelocity = Vector3.zero;
    }

    /// <summary>Get height depending on current stance</summary>
    private float GetHeight(Stance stance) => stance switch {
        Stance.Stand    => _standHeight,
        Stance.Run      => _runHeight,
        Stance.Crouch   => _crouchHeight,
        _ => _standHeight
    };

    /// <summary>Attempt to resize the motor capsule.</summary>
    private bool TryResizeCapsule(float height) {
        float oldHeight = _motor.Capsule.height;
        _motor.SetCapsuleDimensions(_motor.Capsule.radius, height, height * 0.5f);

        //Only increasing scale can cause a collision, so we only need to check for collisions when increasing the height
        if (height > oldHeight && _motor.CharacterOverlap(
            _motor.TransientPosition, _motor.TransientRotation,
            _overlapResults, _motor.CollidableLayers, QueryTriggerInteraction.Ignore
        ) > 0) {
            // If there is a collision when trying to resize the capsule, revert back to the old height and return false
            _motor.SetCapsuleDimensions(_motor.Capsule.radius, oldHeight, oldHeight * 0.5f);
            return false;
        }
        return true;
    }
}
