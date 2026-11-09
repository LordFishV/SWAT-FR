using UnityEngine;

// A door that breaks when hit hard enough. Put it on the door next to its collider.
// Hammer hits, ram hits and explosions all call Breach().
public class Door : MonoBehaviour, IInteractable, IDamageable
{

    public enum RotationAxis
    { X, Y, Z }

    public enum RotationDirection
    {Clockwise = -1, CounterClockwise = 1 }

    [Header("Settings")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;
    [SerializeField] private RotationDirection rotationDirection = RotationDirection.Clockwise;
    [Range(-90,90)]
    [SerializeField] private float rotationAmount = 90f;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private bool startOpened = false;
    [SerializeField] private float _health = 100f;
    [SerializeField] private GameObject _brokenPrefab;   // optional: broken door or debris
    [SerializeField] private AudioClip _breakSound;

    private bool _isOpen = false;
    private bool _isRotating = false;

    private Quaternion _closedRotation;
    private Quaternion _targetRotation;

    private GameObject _interactingObject;

    private void Start()
    {
        // Store initial rotation as CLOSED state
        _closedRotation = transform.rotation;

        if (startOpened)
            Interact(_interactingObject);
    }

    public void Interact(GameObject interactingObject)
    {
        if (_isRotating) return; // prevent spam interaction

        Vector3 axis = GetAxisVector();

        if (!_isOpen)
        {
            // Open door
            _targetRotation = _closedRotation *
            Quaternion.AngleAxis(rotationAmount * (int)rotationDirection, axis);
        }
        else
        {
            // Close door
            _targetRotation = _closedRotation;
        }

        _isOpen = !_isOpen;
        _isRotating = true;
    }

    private void Update()
    {
        if (!_isRotating) return;

        transform.rotation =
        Quaternion.RotateTowards
        (
            from: transform.rotation,
            to: _targetRotation,
            maxDegreesDelta: rotationSpeed * Time.deltaTime
        );

        // Stop rotating when finished
        if (Quaternion.Angle(transform.rotation, _targetRotation) < 0.1f)
        {
            transform.rotation = _targetRotation;
            _isRotating = false;
        }
    }

    public void Breach(float force)
    {
        _health -= force;

        if (_health > 0f)
            return;

        if (_brokenPrefab != null)
            Instantiate(_brokenPrefab, transform.position, transform.rotation);

        if (_breakSound != null)
            AudioSource.PlayClipAtPoint(_breakSound, transform.position);

        Destroy(gameObject);
    }

    private Vector3 GetAxisVector()
    {
        switch (rotationAxis)
        {
            case RotationAxis.X: return Vector3.right;
            case RotationAxis.Y: return Vector3.up;
            case RotationAxis.Z: return Vector3.forward;
        }

        return Vector3.up;
    }

    public void OnHoverEnter(GameObject hoveringObject)
    {
    }

    public void OnHoverExit(GameObject hoveringObject)
    {
    }

    public void ApplyDamage(float damageAmount, GameObject source)
    {
        Breach(damageAmount / 4);
    }
}