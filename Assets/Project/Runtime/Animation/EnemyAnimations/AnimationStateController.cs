using UnityEngine;

public class AnimationStateController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private Enemy hostileAI;

    private int _isWalkingHash;
    private int _isRunningHash;
    private int _isShootingHash;

    private void Awake()
    {
        hostileAI = GetComponent<Enemy>();
        animator = GetComponentInChildren<Animator>();

        _isWalkingHash = Animator.StringToHash("isWalking");
        _isRunningHash = Animator.StringToHash("isRunning");
        _isShootingHash = Animator.StringToHash("isShooting");

        hostileAI.OnStateChange += UpdateAnimation;
    }

    private void OnDestroy()
    {
        if (hostileAI != null)
            hostileAI.OnStateChange -= UpdateAnimation;
    }

    private void UpdateAnimation(AIState aIState)
    {
        Debug.Log(aIState);

        animator.SetBool(_isWalkingHash, aIState == AIState.Patrol);
        animator.SetBool(_isRunningHash, aIState == AIState.Chase);
        animator.SetBool(_isShootingHash, aIState == AIState.Attack);
    }
}
