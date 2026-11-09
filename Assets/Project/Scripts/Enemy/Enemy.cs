using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEditor.Search;
using UnityEngine.XR;
using UnityEngine.EventSystems;

public class Enemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private GameObject CharacterObj;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Layers")]
    [SerializeField] private LayerMask terrainLasyer;
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Vision Settings")]
    [SerializeField] private float fieldofView;
    [SerializeField] private float loseSightTime;

    [Header("Patrol Settings")]
    [SerializeField] private Transform patrolRoute;
    [SerializeField] private float patrolPointTolerence = 1.5f;
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float walkSpeed = 8f;
    [SerializeField] private float runSpeed = 14f;
    [SerializeField] private float pathUpdateRate = 0.2f;
    private Transform[] patrolPoints;
    private int _currentPatrolIndex;
    private bool _isWaiting;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float forwardShotForce;
    [SerializeField] private float verticalShotForce;
    [SerializeField] private float turnSmoothTime = 0.1f;
    [SerializeField] private AudioClip shootSFX;
    private bool _isOnAttackCooldown;

    [Header("Detection Ranges")]
    [SerializeField] private float visionRange = 20f;
    [SerializeField] private float engagementRange = 10f;
    [SerializeField] private float crouchEngagementMult = 0.5f;

    private bool _isPlayerVisable;
    private bool _isPlayerInRange;
    private bool _hasPatrolDestination;
    private bool _stateLocked;
    private float _lastTimeAttacked;
    private float _yawVelocity;
    private float _pitchVelocity;
    private float _lastTimePlayerSeen;
    private float _currentEngagementRagnge;
    private float _nextPathUpdate;
    private float _pitchAngle;
    private float _yawAngle;
    private float _yawTarget;
    private PlayerCharacter _playerCharacter;
    private AudioSource _audioSource;
    private CharacterController _enemyHealth;

    public System.Action<AIState> OnStateChange;
    public AIState currentState;
    public GunState gunState;

    private void Awake()
    {
        if (CharacterObj == null)
            CharacterObj = GameObject.Find("Player");

        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();

        navAgent.updateRotation = false;
        navAgent.updatePosition = true;

        _audioSource = GetComponentInChildren<AudioSource>();

        _enemyHealth = GetComponent<CharacterController>();

        _playerCharacter = CharacterObj.GetComponent<PlayerCharacter>();
        _currentEngagementRagnge = engagementRange;
    }

    private void Start()
    {
        navAgent.speed = walkSpeed;

        ChangeState(AIState.Patrol);
        gunState = GunState.Idle;

        // find and set patrol points from parenting patrol route
        if (patrolRoute != null)
        {
            patrolPoints = new Transform[patrolRoute.childCount];
            if (patrolRoute != null)
                for (int i = 0; i < patrolRoute.childCount; i++)
                {
                    patrolPoints[i] = patrolRoute.GetChild(i);
                }
        }
        else
        {
            // If no patrol points then stay where you are
            patrolPoints = new Transform[1];
            patrolPoints[0] = transform;
        }
    }

    private void Update()
    {
        DetectPlayer();

        if (!_stateLocked)
            UpdateBehaviourState();

        if (currentState == AIState.Patrol || currentState == AIState.Chase)
        {
            Vector3 moveDir = navAgent.desiredVelocity;

            if (moveDir.sqrMagnitude > 0.1f)
                LookInDirection(navAgent.desiredVelocity);
        }

        if (currentState == AIState.Attack)
        {
            Vector3 dirToPlayer =
                CharacterObj.transform.position - transform.position;

            LookInDirection(dirToPlayer);
        }

    }

    private void UpdateBehaviourState()
    {
        // Chase player if recently attacked for a duration
        bool wasRecentlyAttacked =
            Time.time - _lastTimeAttacked <= loseSightTime;

        // Chase player even when out of sight for a duration 
        bool rememberPlayer =
            Time.time - _lastTimePlayerSeen <= loseSightTime;

        AIState newState;

        if (!rememberPlayer && !wasRecentlyAttacked)
            newState = AIState.Patrol;
        else if (!_isPlayerInRange)
            newState = AIState.Chase;
        else
            newState = AIState.Attack;

        if (newState != currentState)
            ChangeState(newState);

        ExecuteState();
    }

    private void LookInDirection(Vector3 direction)
    {
        if (direction.magnitude < 0.01f)
            return;

        direction.Normalize();

        // Calculate y rotation to look at the player (Right/Left Rotation)
        _yawTarget = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        _yawAngle = Mathf.SmoothDampAngle
        (
            transform.eulerAngles.y,
            _yawTarget,
            ref _yawVelocity,
            turnSmoothTime
        );

        // Calculate y rotation to look at the player (Up/Down Rotation)
        float pitchTarget = Mathf.Atan2(direction.y, new Vector3(direction.x, 0, direction.z).magnitude) * Mathf.Rad2Deg;
        pitchTarget = Mathf.Clamp(pitchTarget, -90, 90);
        _pitchAngle = Mathf.SmoothDampAngle(transform.eulerAngles.x, -pitchTarget, ref _pitchVelocity, turnSmoothTime);

        // Apply current angle to enemy rotation
        transform.rotation = Quaternion.Euler(_pitchAngle, _yawAngle, 0f);

        /*
            I do understand that _pitch should be done on the weapon
            but I needed something quick to implement the ability to
            shoot at the player even when the player is above or
            bellow
        */
    }

    private void ChangeState(AIState newState)
    {
        currentState = newState;
        OnStateChange?.Invoke(currentState);

        switch (currentState)
        {
            case (AIState.Idle):
                gunState = GunState.Idle;
                navAgent.isStopped = true;
                break;

            case (AIState.Patrol):
                gunState = GunState.Idle;
                navAgent.speed = walkSpeed;
                navAgent.isStopped = false;
                _hasPatrolDestination = false;
                break;

            case (AIState.Chase):
                gunState = GunState.Idle;
                navAgent.speed = runSpeed;
                navAgent.isStopped = false;
                break;

            case (AIState.Attack):
                gunState = GunState.Shooting;
                navAgent.isStopped = true;
                break;
        }
    }

    private void ExecuteState()
    {
        switch (currentState)
        {
            case (AIState.Patrol):
                PerformPatrol();
                break;

            case (AIState.Chase):
                PerformChase();
                break;

            case (AIState.Attack):
                PerformAttack();
                break;
        }
    }

    private void PerformPatrol() // Patrol around the map
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        if (_isWaiting)
            return;

        navAgent.isStopped = false;
        Transform targetPoint = patrolPoints[_currentPatrolIndex];

        // Only set patrol destiniation once when needed
        if (!_hasPatrolDestination)
        {
            navAgent.SetDestination(targetPoint.position);
            _hasPatrolDestination = true;
        }

        if (!navAgent.pathPending && navAgent.remainingDistance <= patrolPointTolerence)
        {
            StartCoroutine(waitAtPatrolPoint());
        }
    }

    private void PerformChase() // Chase player
    {
        if (CharacterObj == null)
            return;

        if (Time.time >= _nextPathUpdate)
        {
            navAgent.SetDestination(CharacterObj.transform.position);
            _nextPathUpdate = Time.time + pathUpdateRate;
        }
    }

    private void PerformAttack() // Attack player
    {
        navAgent.isStopped = true;

        if (CharacterObj == null)
            return;

        const float aimTolerance = 10f;

        // Checks if the enemy is looking at target within 1% discrepionce
        if (Mathf.Abs(Mathf.DeltaAngle(_yawAngle, _yawTarget)) <= aimTolerance && !_isOnAttackCooldown)
        {
            // Create a coroutine to create the fire delay and fire projectile code
            FireProjectile();
            _isOnAttackCooldown = true;
            StartCoroutine(AttackCooldown());
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 leftLimit = Quaternion.Euler(0, -fieldofView / 2, 0) * transform.forward;
        Vector3 rightLimit = Quaternion.Euler(0, fieldofView / 2, 0) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, leftLimit * visionRange);
        Gizmos.DrawRay(transform.position, rightLimit * visionRange);

        // Show engagement range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _currentEngagementRagnge);

        // Show vision range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }

    private void DetectPlayer()
    {
        if (CharacterObj == null)
            return;

        Vector3 directionToPlayer =
            (CharacterObj.transform.position - transform.position).normalized;

        float distanceToPlayer =
            Vector3.Distance(transform.position, CharacterObj.transform.position);

        // Outside vision range
        if (distanceToPlayer > visionRange)
        { loseSightCheck(); return; }

        // Field of View check (Stops AI from seing behind themselves)
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (angle > fieldofView * 0.5f)
        { loseSightCheck(); return; }

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 1.5f, directionToPlayer, out hit, visionRange))
        {
            if (((1 << hit.collider.gameObject.layer) & playerLayerMask) != 0)
            {
                _isPlayerVisable = true;
                _lastTimePlayerSeen = Time.time;

                bool isCrouching =
                _playerCharacter.State.Stance == Stance.Crouch;

                // Adjust engagement range depending on player stance
                _currentEngagementRagnge = isCrouching
                    ? engagementRange * crouchEngagementMult
                    : engagementRange;

                // Check attack range
                _isPlayerInRange = distanceToPlayer <= _currentEngagementRagnge;

                return;
            }
        }
        loseSightCheck();
    }

    private void loseSightCheck()
    {
        if (Time.time - _lastTimePlayerSeen > loseSightTime)
        {
            _isPlayerVisable = false;
            _isPlayerInRange = false;
        }
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;

        _audioSource.PlayOneShot(shootSFX);

        Rigidbody projectileRB = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity).GetComponent<Rigidbody>();
        projectileRB.AddForce(transform.forward * forwardShotForce, ForceMode.Impulse);
        projectileRB.AddForce(transform.up * verticalShotForce, ForceMode.Impulse);

        Destroy(projectileRB.gameObject, 3f);
    }

    private IEnumerator AttackCooldown()
    {
        _stateLocked = true;
        _isOnAttackCooldown = true;

        yield return new WaitForSeconds(attackCooldown);

        _isOnAttackCooldown = false;
        _stateLocked = false;
    }

    IEnumerator waitAtPatrolPoint()
    {
        _stateLocked = true;
        _isWaiting = true;

        ChangeState(AIState.Idle);
        navAgent.ResetPath();
        yield return new WaitForSeconds(patrolWaitTime);

        _currentPatrolIndex++;

        if (_currentPatrolIndex >= patrolPoints.Length)
            _currentPatrolIndex = 0; // Loop patrol

        ChangeState(AIState.Patrol);

        _hasPatrolDestination = false;
        _isWaiting = false;
        _stateLocked = false;
    }
}

