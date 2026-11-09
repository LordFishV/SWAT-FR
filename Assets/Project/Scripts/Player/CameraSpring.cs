using UnityEngine;

public class CameraSpring : MonoBehaviour
{
    [Header("Spring Parameters")]
    [Min(0.01f)]
    [SerializeField] private float halfLife = 0.075f;
    [Space]
    [SerializeField] private float frequency = 18f;
    [Space]
    [SerializeField] private float anglarDisplacement = 2f;
    [SerializeField] private float linearDisplacement = 0.05f;

    private Vector3 _springPosition;
    private Vector3 _springVelocity;

    public void Initilise()
    {
        _springPosition = transform.position;
        _springVelocity = Vector3.zero;
    }

    /// <summary>Allows the camera to lag behind the acceleration direction vector while still being responcive to the players intentions</summary>
    public void UpdateCameraSpring(float deltaTime, Vector3 up)
    {
        transform.localPosition = Vector3.zero;

        Spring(ref _springPosition, ref _springVelocity, transform.position, halfLife, frequency, deltaTime);

        Vector3 relativeSpringPosition = _springPosition - transform.position;
        float springHeight = Vector3.Dot(relativeSpringPosition, up);

        transform.localEulerAngles = new Vector3(-springHeight * anglarDisplacement, 0f, 0f);
        transform.localPosition += relativeSpringPosition * linearDisplacement;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, _springPosition);
        Gizmos.DrawWireSphere(_springPosition, 0.1f);

    }

    // https://allenchou.net/2015/04/game-math-more-on-numeric-springing/
    private static void Spring(ref Vector3 current, ref Vector3 velocity, Vector3 target, float halfLife, float frequency, float timeStep)
    {
        float dampingRatio = -Mathf.Log(0.5f) / (halfLife * frequency);
        float f = 1.0f + 2.0f * timeStep * dampingRatio * frequency;
        float oo = frequency * frequency;
        float hoo = timeStep * oo;
        float hhoo = timeStep * hoo;
        float detInv = 1.0f / (f + hhoo);

        Vector3 detX = f * current + timeStep * velocity + hhoo * target;
        Vector3 detV = velocity + hoo * (target - current);

        current = detX * detInv;
        velocity = detV * detInv;
    }
}
