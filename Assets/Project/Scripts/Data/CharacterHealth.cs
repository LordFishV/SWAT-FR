using UnityEngine;

public class CharacterHealth : MonoBehaviour, IDamageable
{
    /// <summary>Public health for player UI, default 100.</summary>
    public float Health = 100f;
    private float _maxHealth => Health;

    public void ApplyDamage(float damageAmount)
    {
        Health -= damageAmount;
        Health = Mathf.Clamp(Health, 0, _maxHealth);

        if (Health <= 0f)
            gameObject.SetActive(false);
    }
}
