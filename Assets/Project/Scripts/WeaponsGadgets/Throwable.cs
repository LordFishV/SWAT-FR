using UnityEngine;

// A gadget the player throws: frag grenade, flashbang.
// Put it on a prefab root with a model. Assign the grenade projectile prefab.
public class Throwable : Equippable
{
    [SerializeField] private GameObject _projectilePrefab;   // needs a Rigidbody and a Grenade script
    [SerializeField] private int _count = 2;
    [SerializeField] private float _throwSpeed = 12f;
    [SerializeField] private float _cooldown = 0.8f;
    [SerializeField] private AudioClip _throwSound;
    [SerializeField] private float _spawnDistance = 5f;

    public int Count => _count;

    private float _nextThrowTime;

    public override void UpdateItem(PlayerInput input, Transform aim)
    {
        if (!input.FirePressed || _count <= 0 || Time.time < _nextThrowTime)
            return;

        _nextThrowTime = Time.time + _cooldown;
        _count--;

        // Spawn in front of the camera, clear of the player's capsule
        GameObject grenade = Instantiate(_projectilePrefab, aim.position + aim.forward * _spawnDistance, aim.rotation);
        grenade.GetComponent<Rigidbody>().linearVelocity = aim.forward * _throwSpeed;

        if (_throwSound != null)
            AudioSource.PlayClipAtPoint(_throwSound, aim.position);
    }
}