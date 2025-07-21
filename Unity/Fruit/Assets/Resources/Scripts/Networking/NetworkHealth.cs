using Mirror;
using UnityEngine;

public abstract class NetworkHealth : NetworkBehaviour
{
    #region FIELDS

    [SyncVar(hook = nameof(OnHealthChanged))]
    [SerializeField] protected int _currentHealth;

    [SerializeField] private int _maxHealth = 9;

    #endregion

    #region PROPERTIES

    public int maxHealth => _maxHealth;

    public bool healthDepleted => _currentHealth <= 0;

    #endregion

    #region METHODS

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to damage this character and check if their health depleted.
    /// </summary>

    [Command]
    public virtual void CmdTakeDamage(int amount)
    {
        if (healthDepleted) return;

        _currentHealth -= amount;
        _currentHealth = Mathf.Max(_currentHealth, 0);

        if (healthDepleted)
            OnHealthDepleted();
    }

    /// <summary>
    /// Update the UI to reflect the current health of this character.
    /// </summary>

    protected virtual void OnHealthChanged(int oldHealth, int newHealth)
    {
        // Override here for UI
    }

    /// <summary>
    /// Override this method to handle character/object health depletion seperately.
    /// </summary>

    protected abstract void OnHealthDepleted();

    #endregion
}
