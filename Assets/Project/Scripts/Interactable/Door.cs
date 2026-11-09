using UnityEngine;

// A door that breaks when hit hard enough. Put it on the door next to its collider.
// Hammer hits, ram hits and explosions all call Breach().
public class Door : MonoBehaviour
{
    [SerializeField] private float _health = 100f;
    [SerializeField] private GameObject _brokenPrefab;   // optional: broken door or debris
    [SerializeField] private AudioClip _breakSound;

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
}