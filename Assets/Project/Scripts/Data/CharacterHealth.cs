using UnityEngine;

public class CharacterHealth : MonoBehaviour, IDamageable, IHealable
{
    /// <summary>Public health for player UI, default 100.</summary>
    public float Health = 100f;
    private float _maxHealth => Health;

    public void ApplyDamage(float damageAmount, GameObject source)
    {
        Health -= damageAmount;
        Health = Mathf.Clamp(Health, 0, _maxHealth);

        if (Health <= 0f)
            gameObject.SetActive(false);
    }

    public void ApplyHeal(float healAmount, GameObject source)
    {
        if (Health < _maxHealth)
        {
            Health += healAmount;
            Health = Mathf.Clamp(Health, 0, _maxHealth);
        }
    }
}
