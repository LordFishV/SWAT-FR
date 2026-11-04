using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private float _sensitivity = 0.1f;
    private float _yaw, _pitch;
    public void Initilise(Transform anchor)
    {
        transform.position = anchor.position;
        Vector3 rawRotation = anchor.eulerAngles;
        _yaw = rawRotation.y;
        _pitch = rawRotation.x > 180f ? rawRotation.x - 360f : rawRotation.x;
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    public void UpdateRotation(PlayerInput input)
    {
        _yaw += input.Look.x * _sensitivity;
        _pitch -= input.Look.y * _sensitivity;
        _pitch = Mathf.Clamp(_pitch, -89f, 89f);
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    public void UpdatePosition(Transform anchor)
    {
        transform.position = anchor.position;
    }
}
