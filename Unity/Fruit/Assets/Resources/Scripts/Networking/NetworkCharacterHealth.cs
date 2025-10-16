using EasyCharacterMovement;
using Mirror;
using System.Collections;
using UnityEngine;

public class NetworkCharacterHealth : NetworkHealth
{
    #region FIELDS

    [SyncVar] private bool _invulnerable;

    private FruitCharacter _character;
    private float _invulnTimer;

    #endregion

    #region PROPERTIES

    public bool isInvulnerable => _invulnerable;

    #endregion
    
    #region MONOBEHAVIOR

    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>

    private void Awake()
    {
        _character = GetComponent<FruitCharacter>();
        _currentHealth = maxHealth;
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to damage this character and check if their health depleted.
    /// </summary>

    [Command]
    public void CmdDamageTarget(NetworkIdentity targetIdentity, NetworkIdentity attackerIdentity, int amount)
    {
        NetworkCharacterHealth targetHealth = targetIdentity.GetComponent<NetworkCharacterHealth>();

        targetHealth.TakeDamage(attackerIdentity, amount);
    }

    /// <summary>
    /// Damage this character and check if their health depleted.
    /// </summary>

    public override void TakeDamage(NetworkIdentity attackerIdentity, int amount)
    {
        if (healthDepleted) return;

        _currentHealth -= amount;
        _currentHealth = Mathf.Max(_currentHealth, 0);

        if (healthDepleted)
        {
            GameManager.Instance.UpdatePlayerScore(attackerIdentity.connectionToClient, 1);

            OnHealthDepleted();
        }
    }

    /// <summary>
    /// Update the UI to reflect the current health of this character.
    /// </summary>

    protected override void OnHealthChanged(int oldHealth, int newHealth)
    {
        // TODO: Update UI (health bar, damage effect)
    }

    /// <summary>
    /// Setup and start the invulnerability timer.
    /// </summary>

    public void ApplyTemporaryInvulnerability(float duration)
    {
        _invulnerable = true;
        _invulnTimer = duration;
        StartCoroutine(InvulnerabilityTimer());
    }

    /// <summary>
    /// Reset the invulnerability state when the timer is up.
    /// </summary>

    private IEnumerator InvulnerabilityTimer()
    {
        yield return new WaitForSeconds(_invulnTimer);
        _invulnerable = false;
    }

    /// <summary>
    /// When character health has depleted, begin their respawn routine.
    /// </summary>

    protected override void OnHealthDepleted()
    {
        StartCoroutine(HandleRespawnRoutine());
    }

    /// <summary>
    /// Setup the character for a fake respawn (easier then detroying their body);
    /// Disable and hide the character, then teleport their body and reset their health.
    /// </summary>

    private IEnumerator HandleRespawnRoutine()
    {
        NetworkConnectionToClient client = GetComponent<NetworkIdentity>().connectionToClient;

        RpcDisableCharacter(client);
        RpcSetVisibility(false);

        yield return new WaitForSeconds(1f);

        //Transform spawn = GameManager.Instance.GetRandomSpawn(); // Implement this in GameManager
        //transform.SetPositionAndRotation(spawn.position, spawn.rotation);

        _currentHealth = maxHealth;

        RpcSetVisibility(true);
        RpcEnableCharacter(client);
    }

    /// <summary>
    /// Disable the character on the original client's version of this Character.
    /// </summary>

    [TargetRpc]
    public void RpcDisableCharacter(NetworkConnection conn)
    {
        _character.DisableCharacter();
    }

    /// <summary>
    /// Enable the character on the original client's version of this Character.
    /// </summary>

    [TargetRpc]
    public void RpcEnableCharacter(NetworkConnection conn)
    {
        _character.EnableCharacter();
    }

    /// <summary>
    /// Hide the character for every client while they "respawn".
    /// </summary>

    [ClientRpc]
    private void RpcSetVisibility(bool visible)
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = visible;
    }

    #endregion
}
