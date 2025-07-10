using Mirror;
using UnityEngine;

public abstract class NetworkHealth : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    protected int _currentHealth;

    public int maxHealth { get; protected set; } = 9;

    public int currentHealth => _currentHealth;

    public bool healthDepleted => _currentHealth <= 0;

    public virtual void TakeDamage(int amount)
    {
        if (!isServer || healthDepleted) return;

        _currentHealth -= amount;
        _currentHealth = Mathf.Max(_currentHealth, 0);

        if (healthDepleted)
            OnHealthDepleted();
    }

    protected virtual void OnHealthChanged(int oldHealth, int newHealth)
    {
        // Override here for UI
    }

    protected abstract void OnHealthDepleted();
}
