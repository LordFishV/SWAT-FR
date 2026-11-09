using System.Collections.Generic;
using UnityEngine;

// The thrown projectile. Put it on a prefab with a Rigidbody and a collider.
// After the fuse it hurts, blinds and/or breaks everything in range.
// Set only the numbers you want:
//   Frag grenade:      damage 100
//   Flashbang:         blind seconds 6
//   Breaching charge:  breach force 100
public class Grenade : MonoBehaviour
{
    [Header("Fuse and area")]
    [SerializeField] private float _fuseTime = 3f;
    [SerializeField] private float _radius = 8f;
    [SerializeField] private LayerMask _hitMask = ~0;

    [Header("What it does")]
    [SerializeField] private float _damage = 0f;
    [SerializeField] private float _blindSeconds = 0f;
    [SerializeField] private float _breachForce = 0f;

    [Header("Effects")]
    [SerializeField] private GameObject _explosionPrefab;
    [SerializeField] private AudioClip _explosionSound;

    private void Start()
    {
        Invoke(nameof(Explode), _fuseTime);
    }

    private void Explode()
    {
        // A character can have several colliders, so remember who we already affected
        var damaged = new HashSet<IDamageable>();
        var blinded = new HashSet<Blindable>();
        var breached = new HashSet<Door>();

        Collider[] hits = Physics.OverlapSphere(transform.position, _radius, _hitMask, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            // 1 at the centre of the blast, 0 at the edge
            float distance = Vector3.Distance(transform.position, hit.ClosestPointOnBounds(transform.position));
            float strength = 1f - Mathf.Clamp01(distance / _radius);

            if (_damage > 0f)
            {
                IDamageable target = hit.GetComponentInParent<IDamageable>();
                if (target != null && damaged.Add(target))
                    target.ApplyDamage(_damage * strength, gameObject);
            }

            if (_blindSeconds > 0f)
            {
                Blindable blindable = hit.GetComponentInParent<Blindable>();
                if (blindable != null && blinded.Add(blindable))
                    blindable.Blind(_blindSeconds * Mathf.Max(strength, 0.25f));
            }

            if (_breachForce > 0f)
            {
                Door door = hit.GetComponentInParent<Door>();
                if (door != null && breached.Add(door))
                    door.Breach(_breachForce * strength);
            }
        }

        if (_explosionPrefab != null)
            Destroy(Instantiate(_explosionPrefab, transform.position, Quaternion.identity), 4f);

        if (_explosionSound != null)
            AudioSource.PlayClipAtPoint(_explosionSound, transform.position);

        Destroy(gameObject);
    }

    // Shows the blast radius in the Scene view when selected
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}