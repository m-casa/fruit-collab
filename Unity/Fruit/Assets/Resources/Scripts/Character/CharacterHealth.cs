using EasyCharacterMovement;
using Mirror;
using System.Collections;
using UnityEngine;

public class CharacterHealth : NetworkHealth
{
    private FruitCharacter _character;

    // NEW CODE -------------------------------------------------------------------------------
    private Combat _combat;
    private bool _invulnerable;
    private float _invulnTimer;

    public bool isInvulnerable => _invulnerable;
    // NEW CODE -------------------------------------------------------------------------------

    private void Awake()
    {
        _character = GetComponent<FruitCharacter>();

        // NEW CODE -------------------------------------------------------------------------------
        _combat = GetComponent<Combat>();
        // NEW CODE -------------------------------------------------------------------------------

        _currentHealth = maxHealth;
    }

    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);
    }

    protected override void OnHealthChanged(int oldHealth, int newHealth)
    {
        base.OnHealthChanged(oldHealth, newHealth);
        // TODO: Update UI (health bar, damage effect)
    }

    // NEW CODE -------------------------------------------------------------------------------
    public void ApplyTemporaryInvulnerability(float duration)
    {
        _invulnerable = true;
        _invulnTimer = duration;
        StartCoroutine(InvulnerabilityTimer());
    }

    private IEnumerator InvulnerabilityTimer()
    {
        yield return new WaitForSeconds(_invulnTimer);
        _invulnerable = false;
    }
    // NEW CODE -------------------------------------------------------------------------------

    protected override void OnHealthDepleted()
    {
        StartCoroutine(HandleRespawnRoutine());
    }

    private IEnumerator HandleRespawnRoutine()
    {
        _character.DisableCharacter();
        RpcSetVisibility(false);

        yield return new WaitForSeconds(1f);

        //Transform spawn = GameManager.Instance.GetRandomSpawn(); // Implement this in GameManager
        //transform.SetPositionAndRotation(spawn.position, spawn.rotation);

        _currentHealth = maxHealth;

        RpcSetVisibility(true);
        _character.EnableCharacter();
    }

    [ClientRpc]
    private void RpcSetVisibility(bool visible)
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = visible;
    }
}
