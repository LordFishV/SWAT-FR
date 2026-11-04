using UnityEngine;

public class CameraLean : MonoBehaviour
{
    [SerializeField] private float _attackDamping = 0.5f;
    [SerializeField] private float _decayDamping = 0.3f;
    [SerializeField] private float _leanWalkStrength = 0.075f;
    [SerializeField] private float _leanSlideStrength = 0.2f;
    [SerializeField] private float _strengthResponce = 5f;

    private Vector3 _dampedAcceleration;
    private Vector3 _dampedAccelerationVelocity;
    private float _smoothLeanStrength;
    public void Initilise()
    {
        _smoothLeanStrength = _leanWalkStrength;
    }

    public void UpdateLean(float deltaTime, bool isSliding, Vector3 acceleration, Vector3 up)
    {
        // Project the acceleration onto the plane defined by the up vector to get the planar acceleration
        Vector3 planarAcceleration = Vector3.ProjectOnPlane(acceleration, up);
        float damping = planarAcceleration.magnitude > _dampedAcceleration.magnitude
            ? _attackDamping
            : _decayDamping;

        // Smoothly damp the acceleration to create a more natural leaning effect
        _dampedAcceleration = Vector3.SmoothDamp
        (
            current: _dampedAcceleration,
            target: planarAcceleration,
            currentVelocity: ref _dampedAccelerationVelocity,
            smoothTime: damping,
            maxSpeed: Mathf.Infinity,
            deltaTime: deltaTime
        );

        // Calculate the lean axis based on the damped acceleration and the up vector
        Vector3 leanAxis = Vector3.Cross(_dampedAcceleration.normalized, up).normalized;

        // Reset the local rotation to that of the parent transform to ensure the lean is applied correctly
        transform.localRotation = Quaternion.identity;

        // Apply the lean based on the damped acceleration and the appropriate lean strength for walking or sliding
        float targetLeanStrength = isSliding
            ? _leanSlideStrength
            : _leanWalkStrength;

        _smoothLeanStrength = Mathf.Lerp
            (
                _smoothLeanStrength,
                targetLeanStrength,
                1f - Mathf.Exp(-_strengthResponce * deltaTime)
            );

        transform.rotation = Quaternion.AngleAxis(_dampedAcceleration.magnitude * _smoothLeanStrength, leanAxis) * transform.rotation;
    }
}
