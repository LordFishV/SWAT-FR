using UnityEngine;
using KinematicCharacterController;

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
    [SerializeField] private float _proneSpeed = 5f;
    [SerializeField] private float _jumpSpeed = 20f;
    [SerializeField] private float _slideStartSpeed = 25f;
    [SerializeField] private float _slideEndSpeed = 15f;
    [Space]

    [Header("In-Air Movement")]
    [SerializeField] private float _inAirSpeed = 15f;
    [SerializeField] private float _inAirAcceleration = 70f;
    [SerializeField] private float _coyoteTime = 0.2f;
    [Space]

    [Header("Response")]
    [SerializeField] private float _walkHeightResponse = 25f;
    [SerializeField] private float _runHeightResponse = 30f;
    [SerializeField] private float _crouchHeightResponse = 15f;
    [SerializeField] private float _proneHeightResponse = 15f;
    [SerializeField] private float _walkResponse = 25f;
    [SerializeField] private float _runResponse = 30f;
    [SerializeField] private float _crouchResponse = 20f;
    [SerializeField] private float _proneResponse = 15f;
    [Space]

    [Header("Physics")]
    [SerializeField] private float _gravity = 98.1f;
    [SerializeField] private float _slideGravity = 98.1f;
    [Range(0f, 1f)]
    [SerializeField] private float _jumpSustainGravity = 0.4f;
    [Range(0f, 1f)]
    [SerializeField] private float _slideFriction = 0.8f;
    [SerializeField] private float _slideSteerAcceleration = 5f;
    [Space]

    [Header("Capsule Heights")]
    [SerializeField] private float _standHeight = 2f;
    [SerializeField] private float _runHeight = 2.2f;
    [SerializeField] private float _crouchHeight = 1f;
    [SerializeField] private float _proneHeight = 0.5f;
    [Space]

    [Header("Anchor Heights")]
    [Range(0f, 1f)]
    [SerializeField] private float _anchorStandHeight = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float _anchorRunHeight = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float _anchorCrouchHeight = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float _anchorProneHeight = 0.8f;

    // States
    private CharacterState _state;
    private CharacterState _lastState;
    private CharacterState _tempState;

    // Private fields requested by the player input
    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedJump;
    private bool _requestedJumpSustain;
    private bool _requestedCrouchInAir;
    private Stance _requestedStance = Stance.Stand;

    // Private fields not requested by the player input
    private Collider[] _overlapResults;
    private float _timeSinceUngrounded;
    private float _timeSinceJumpRequest;
    private bool _ungroundedDueToJump;

    public void Initilise()
    {
        _state.Stance = Stance.Stand;
        _lastState = _state;

        _overlapResults = new Collider[8];

        _motor.CharacterController = this;
    }

    /// <summary>Update the players incoming inputs (Mouse and Keyboard only for now)</summary>
    public void UpdateInput(PlayerInput input)
    {
        _requestedRotation = input.LookRotation;
        // Convert the input move vector into a worldspace direction relative to the character's rotation
        _requestedMovement = new Vector3(input.Move.x, 0f, input.Move.y);
        // Clamp input movement to a maximum magnitude of 1, so that diagonal movement isn't faster
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);
        // Rotate input movement to be relative to character rotation
        _requestedMovement = input.LookRotation * _requestedMovement;

        // Handle jump input
        bool wasRequestedJump = _requestedJump;
        _requestedJump = _requestedJump || input.Jump;
        //  If the jump was requested this frame and it wasn't requested in the last frame, reset the jump request timer
        if (_requestedJump && !wasRequestedJump)
            _timeSinceJumpRequest = 0f;
        // Handle jump sustain input
        _requestedJumpSustain = input.JumpSustain;

        Stance previousRequest = _requestedStance;

        // Handle stance input
        if (input.Run == RunInput.Toggle)
            _requestedStance = _requestedStance is Stance.Run ? Stance.Stand : Stance.Run;
        if (input.Crouch == CrouchInput.Toggle)
            _requestedStance = _requestedStance is Stance.Crouch ? Stance.Stand : Stance.Crouch;
        if (input.Prone == ProneInput.Toggle)
            _requestedStance = _requestedStance is Stance.Prone ? Stance.Stand : Stance.Prone;

        if (_requestedStance is Stance.Crouch && previousRequest is not Stance.Crouch)
            _requestedCrouchInAir = !_state.Grounded;
        else if (_requestedStance is not Stance.Crouch)
            _requestedCrouchInAir = false;
    }

    /// <summary>Updates player scale and responces depending on possable desired state</summary>
    public void UpdateBody(float deltaTime)
    {
        // Get the current height of the character's capsule collider
        float currentHeight = _motor.Capsule.height;

        // Calculate the normalized height of the character's capsule collider relative to the standing height
        float normalisedHeight = currentHeight / _standHeight;

        // Calculate the camera anchor height based on the current stance and the defined anchor heights
        float cameraAnchorHeight = currentHeight *
        (
            _state.Stance switch
            {
                Stance.Stand                    => _anchorStandHeight,  // Use the defined anchor height for standing
                Stance.Run                      => _anchorRunHeight,    // Use the defined anchor height for running
                Stance.Crouch or Stance.Slide   => _anchorCrouchHeight, // Use the defined anchor height for crouching
                Stance.Prone                    => _anchorProneHeight,  // Use the defined anchor height for prone
                _ => _anchorStandHeight                 // Default to the standing anchor height for any other stance
            }
        );

        float heightResponse =
        (
            _state.Stance switch
            {
                Stance.Stand                    => _walkHeightResponse,   // Use the defined response for standing
                Stance.Run                      => _runHeightResponse,    // Use the defined response for running
                Stance.Crouch or Stance.Slide   => _crouchHeightResponse, // Use the defined response for crouching
                Stance.Prone                    => _proneHeightResponse,  // Use the defined response for prone
                _ => _walkHeightResponse                  // Default to the standing response for any other stance
            }
        );

        Vector3 rootAnchorScale = new Vector3(1f, normalisedHeight, 1f);

        // Update the camera anchor position based on the current stance
        _cameraAnchor.localPosition = Vector3.Lerp
        (
            a: _cameraAnchor.localPosition,
            b: new Vector3(0f, cameraAnchorHeight, 0f),
            t: 1f - Mathf.Exp(-heightResponse * deltaTime)
        );
        _root.localScale = Vector3.Lerp
        (
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
            // Reset the time since ungrounded to zero when the character is grounded
            _timeSinceUngrounded = 0f;
            // Reset the ungrounded due to jump flag to false when the character is grounded
            _ungroundedDueToJump = false;

            // Project the movement direction onto the plane defined by the ground's normal
            Vector3 groundedMovement = _motor.GetDirectionTangentToSurface
            (
                direction: _requestedMovement,
                surfaceNormal: _motor.GroundingStatus.GroundNormal
            ) * _requestedMovement.magnitude;

            // Sliding logic
            {
                // Determine if the character is moving, crouching, and was either standing or in the air in the last frame
                bool isMoving = groundedMovement.sqrMagnitude > 0f;
                bool isCrouching = _state.Stance is Stance.Crouch;
                bool wasRunning = _lastState.Stance is Stance.Run;
                bool wasInAir = !_lastState.Grounded;
                // If the character is moving, crouching, and was either standing or in the air in the last frame, transition to a slide stance
                if (isMoving && isCrouching && (wasRunning || wasInAir))
                {
                    _state.Stance = Stance.Slide;
                    // If the character was in the air in the last frame, project the last frame's velocity onto the ground normal.
                    // Keeps the player to speed when landing from a jump or fall and entering a slide.
                    //Bypasses KCC's grounding velocity reset, which would otherwise reset the velocity to zero when landing.
                    if (wasInAir)
                    {
                        currentVelocity = Vector3.ProjectOnPlane
                        (
                            vector: _lastState.Velocity,
                            planeNormal: _motor.GroundingStatus.GroundNormal
                        );
                    }

                    float effectiveSlideStartSpeed = _slideStartSpeed;
                    // If the player was in the air and requested a crouch, we want to maintain the current velocity magnitude as the slide start speed
                    if (!_lastState.Grounded && !_requestedCrouchInAir)
                    {
                        effectiveSlideStartSpeed = 0f;
                        _requestedCrouchInAir = false;
                    }

                    // Set the slide speed to the maximum of the defined slide start speed and the current velocity magnitude
                    float slideSpeed = Mathf.Max(effectiveSlideStartSpeed, currentVelocity.magnitude);
                    currentVelocity = _motor.GetDirectionTangentToSurface
                    (
                        direction: currentVelocity,
                        surfaceNormal: _motor.GroundingStatus.GroundNormal
                    ) * slideSpeed;

                    Debug.DrawRay(transform.position, currentVelocity, Color.blue, 5f);
                }
            }

            // Movement logic
            if (_state.Stance is not Stance.Slide)
            {
                // Calculate the character's speed based on the current stance
                float speed = _state.Stance switch
                {
                    Stance.Stand    => _walkSpeed,      // Use walk speed when standing
                    Stance.Run      => _runSpeed,       // Use run speed when running
                    Stance.Crouch   => _crouchSpeed,    // Use crouch speed when crouching
                    Stance.Prone    => _proneSpeed,     // Use prone speed when prone
                    _ => _walkSpeed                     // Default to walk speed for any other stance
                };
                // Calculate the speed response based on the current stance
                float response = _state.Stance switch
                {
                    Stance.Stand    => _walkResponse,   // Use walk response when standing
                    Stance.Run      => _runResponse,    // Use run response when running
                    Stance.Crouch   => _crouchResponse, // Use crouch response when crouching
                    Stance.Prone    => _proneResponse,  // Use prone response when prone
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
            // Continue sliding logic if the character is in a slide stance
            else
            {
                //Friction logic for sliding
                currentVelocity -= currentVelocity * (_slideFriction * deltaTime);

                // Slide down slope logic
                {
                    Vector3 force = Vector3.ProjectOnPlane
                    (
                        vector: -_motor.CharacterUp,
                        planeNormal: _motor.GroundingStatus.GroundNormal
                    ) * -_slideGravity;

                    currentVelocity -= force * deltaTime;
                }

                // Slide steering logic
                {
                    float currentSpeed = Vector3.ProjectOnPlane
                    (
                        vector: currentVelocity,
                        planeNormal: _motor.GroundingStatus.GroundNormal
                    ).magnitude;
                    Vector3 targetVelocity = groundedMovement * currentSpeed;

                    Vector3 steerVelocity = currentVelocity;
                    Vector3 steerForce = (targetVelocity - steerVelocity) * _slideSteerAcceleration * deltaTime;
                    // Add the steer force to the current velocity and clamp it to the current speed to prevent excessive steering
                    steerVelocity += steerForce;
                    steerVelocity = Vector3.ClampMagnitude(steerVelocity, currentSpeed);

                    _state.Acceleration = (steerVelocity - currentVelocity) / deltaTime;

                    currentVelocity = steerVelocity;
                }

                // Stop sliding if the character's speed drops below the defined slide end speed
                if (currentVelocity.magnitude < _slideEndSpeed)
                    _state.Stance = Stance.Crouch;
            }

        }
        // If the character is not grounded, add air movement and gravity
        else
        {
            // Update the time since the character was last grounded
            _timeSinceUngrounded += deltaTime;

            if (_requestedMovement.sqrMagnitude > 0f)
            {
                // Project the requested movement onto the plane defined by the character's up direction to get the planar movement
                Vector3 planeMovement = Vector3.ProjectOnPlane
                    (
                        vector: _requestedMovement,
                        planeNormal: _motor.CharacterUp
                    ) * _requestedMovement.magnitude;

                // Project the current velocity onto the plane defined by the character's up direction to get the planar velocity
                Vector3 currentPlanarVelocity = Vector3.ProjectOnPlane
                    (
                        vector: currentVelocity,
                        planeNormal: _motor.CharacterUp
                    );

                // Calculate the movement force based on the in-air acceleration and delta time
                Vector3 movementForce = planeMovement * _inAirAcceleration * deltaTime;

                if (currentPlanarVelocity.magnitude < _inAirSpeed)
                {
                    // Calculate the target planar velocity by adding the movement force to the current planar velocity
                    Vector3 targetPlanarVelocity = currentPlanarVelocity + movementForce;

                    // Clamp the target planar velocity to the maximum in-air speed to prevent excessive speed
                    targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, _inAirSpeed);

                    // Calculate the movement force based on the difference between the target planar velocity and the current planar velocity
                    movementForce = targetPlanarVelocity - currentPlanarVelocity;
                }
                else if (Vector3.Dot(currentPlanarVelocity, movementForce) > 0f)
                {
                    Vector3 constrainedMovementForce = Vector3.ProjectOnPlane
                    (
                        vector: movementForce,
                        planeNormal: currentPlanarVelocity.normalized
                    );

                    movementForce = constrainedMovementForce;
                }

                // Prevent air-climbing steep slopes.
                if (_motor.GroundingStatus.FoundAnyGround)
                {
                    // if the movement force is in the same direction as the resultant velocity
                    if (Vector3.Dot(movementForce, currentVelocity + movementForce) > 0f)
                    {
                        // Calculate the obstruction normal
                        Vector3 obstructionNormal = Vector3.Cross
                        (
                            _motor.CharacterUp,
                            Vector3.Cross
                            (
                                _motor.CharacterUp,
                                _motor.GroundingStatus.GroundNormal
                            )
                        ).normalized;

                        // Project the movement force onto the plane defined by the obstruction normal to prevent climbing steep slopes
                        movementForce = Vector3.ProjectOnPlane
                        (
                            vector: movementForce,
                            planeNormal: obstructionNormal
                        );
                    }
                }

                // Steer towards current planar velocity by adding the difference between the target and current planar velocity to the current velocity
                currentVelocity += movementForce;
            }

            // gravity is multiplied by the jump sustain gravity factor if the player is holding the jump button and moving upwards
            float effectiveGravity = _gravity;
            float verticalSpeed = Vector3.Dot(currentVelocity, _motor.CharacterUp);
            if (_requestedJumpSustain && verticalSpeed > 0f)
                effectiveGravity *= _jumpSustainGravity;

            // Apply gravity
            currentVelocity -= _motor.CharacterUp * effectiveGravity * deltaTime;
        }

        if (_requestedJump)
        {
            bool grounded = _motor.GroundingStatus.IsStableOnGround;
            bool canCoyoteJump = _timeSinceUngrounded < _coyoteTime && !_ungroundedDueToJump;

            if (grounded || canCoyoteJump)
            {
                _requestedStance = _lastState.Stance;  // Reset the stance to stand when jumping
                _requestedCrouchInAir = false;  // Clear the requested crouch in air state to ensure the character is not crouching when jumping

                // Make sure to unground the character before applying the jump velocity
                _motor.ForceUnground(time: 0.1f);
                _ungroundedDueToJump = true;

                // Set minimum vertical speed to the jump speed.
                float currentVerticalSpeed = Vector3.Dot(currentVelocity, _motor.CharacterUp);
                float targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, _jumpSpeed);

                // Add the difference between the current and target vertical speed to the current velocity
                currentVelocity += _motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
            }
            else
            {
                _timeSinceJumpRequest += deltaTime;

                // Allow jump input to be buffered for a short duration after leaving the ground, enabling coyote time jumps
                bool canJumpLater = _timeSinceJumpRequest < _coyoteTime;
                _requestedJump = canJumpLater;
            }
        }
    }

    /// <summary>Updates player rotation based on the projecton plane</summary>
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        // Update the character's rotation based on the requested rotation depending on the character's up direction to the ground
        Vector3 forward = Vector3.ProjectOnPlane
            (
                vector: _requestedRotation * Vector3.forward,
                planeNormal: _motor.CharacterUp
            );

        if (forward != Vector3.zero)
            currentRotation = Quaternion.LookRotation(forward, _motor.CharacterUp);
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        _tempState = _state;

        // Determine if the character should keep sliding based on the current stance and the requested stance
        bool keepSliding = _state.Stance is Stance.Slide && _requestedStance is Stance.Crouch;

        // If the requested stance is different from the current stance and the character should not keep sliding, attempt to resize the capsule collider to match the requested stance
        if (_state.Stance != _requestedStance && !keepSliding)
        {
            if (TryResizeCapsule(GetHeight(_requestedStance)))
                _state.Stance = _requestedStance;
            // If the capsule resize fails, the stance will remain unchanged, and the requested stance will be retried in the next frame
        }
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        if (!_motor.GroundingStatus.IsStableOnGround && _state.Stance is Stance.Slide)
            _state.Stance = Stance.Crouch;
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
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

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
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

    public void SetPosition(Vector3 position, bool killVelocity = true)
    {
        _motor.SetPosition(position);
        if (killVelocity)
            _motor.BaseVelocity = Vector3.zero;
    }

    /// <summary>Get height depending on current stance</summary>
    private float GetHeight(Stance stance) => stance switch
    {
        Stance.Stand    => _standHeight,
        Stance.Run      => _runHeight,
        Stance.Crouch   => _crouchHeight,
        Stance.Prone    => _proneHeight,
        _ => _standHeight
    };

    /// <summary>Attempt to resize the motor capsule.</summary>
    private bool TryResizeCapsule(float height)
    {
        float oldHeight = _motor.Capsule.height;
        _motor.SetCapsuleDimensions(_motor.Capsule.radius, height, height * 0.5f);

        //Only increasing scale can cause a collision, so we only need to check for collisions when increasing the height
        if (height > oldHeight &&
            _motor.CharacterOverlap(_motor.TransientPosition, _motor.TransientRotation, _overlapResults, _motor.CollidableLayers, QueryTriggerInteraction.Ignore) > 0)
        {
            // If there is a collision when trying to resize the capsule, revert back to the old height and return false
            _motor.SetCapsuleDimensions(_motor.Capsule.radius, oldHeight, oldHeight * 0.5f);
            return false;
        }
        return true;
    }
}
