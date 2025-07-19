using EasyCharacterMovement;
using Mirror;
using System.Collections;
using UnityEngine;

public class CharacterHealth : NetworkHealth
{
    #region FIELDS

    [SyncVar]
    private bool _invulnerable;

    private FruitCharacter _character;
    private Combat _combat;
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
        _combat = GetComponent<Combat>();
        _currentHealth = maxHealth;
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Damage this character and check if their health depleted.
    /// </summary>

    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);
    }

    /// <summary>
    /// Update the UI to reflect the current health of this character.
    /// </summary>

    protected override void OnHealthChanged(int oldHealth, int newHealth)
    {
        base.OnHealthChanged(oldHealth, newHealth);
        // TODO: Update UI (health bar, damage effect)
    }

    /// <summary>
    /// When character health has depleted, begin their respawn routine.
    /// </summary>

    protected override void OnHealthDepleted()
    {
        StartCoroutine(HandleRespawnRoutine());
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
    /// Setup the character for a fake respawn (easier then detroying their body);
    /// Disable and hide the character, then teleport their body and reset their health.
    /// </summary>

    private IEnumerator HandleRespawnRoutine()
    {
        _character.RpcDisableCharacter(GetComponent<NetworkIdentity>().connectionToClient);
        RpcSetVisibility(false);

        yield return new WaitForSeconds(1f);

        //Transform spawn = GameManager.Instance.GetRandomSpawn(); // Implement this in GameManager
        //transform.SetPositionAndRotation(spawn.position, spawn.rotation);

        _currentHealth = maxHealth;

        RpcSetVisibility(true);
        _character.RpcEnableCharacter(GetComponent<NetworkIdentity>().connectionToClient);
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
