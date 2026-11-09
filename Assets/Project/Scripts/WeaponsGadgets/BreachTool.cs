using UnityEngine;
using UnityEngine.InputSystem;

// A hammer or a ram. Same script, different numbers:
//   Hammer: force 34  (about 3 hits for a 100 health door), cooldown 0.9
//   Ram:    force 100 (1 hit), cooldown 1.6
public class BreachTool : Equippable
{
    [SerializeField] private float _force = 34f;
    [SerializeField] private float _reach = 2f;
    [SerializeField] private float _cooldown = 0.9f;
    [SerializeField] private AudioClip _swingSound;
    [SerializeField] private AudioClip _hitSound;

    private float _nextSwingTime;

    public override void UpdateItem(PlayerInput input, Transform aim)
    {
        if (!input.FirePressed || Time.time < _nextSwingTime)
            return;

        _nextSwingTime = Time.time + _cooldown;

        if (_swingSound != null)
            AudioSource.PlayClipAtPoint(_swingSound, aim.position);

        if (!Physics.Raycast(aim.position, aim.forward, out RaycastHit hit, _reach))
            return;

        if (_hitSound != null)
            AudioSource.PlayClipAtPoint(_hitSound, hit.point);

        Door door = hit.collider.GetComponentInParent<Door>();
        if (door != null)
            door.Breach(_force);
    }
}