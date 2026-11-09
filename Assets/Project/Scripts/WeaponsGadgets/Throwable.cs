using UnityEngine;

// A gadget the player throws: frag grenade, flashbang.
// Put it on a prefab root with a model. Assign the grenade projectile prefab.
public class Throwable : Equippable
{
    [SerializeField] private GameObject _projectilePrefab;   // needs a Rigidbody and a Grenade script
    [SerializeField] private int _count = 2;
    [SerializeField] private float _throwSpeed = 12f;
    [Tooltip("How far in front of the camera the grenade appears. Keep it larger than the player's capsule radius.")]
    [SerializeField] private float _spawnDistance = 1f;
    [SerializeField] private float _cooldown = 0.8f;
    [SerializeField] private AudioClip _throwSound;

    public int Count => _count;
    public override int Charges => _count;   // shown on the HUD as "x 2"

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