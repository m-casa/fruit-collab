using EasyCharacterMovement;
using Mirror;
using System.Collections;
using UnityEngine;

public class CharacterHealth : NetworkHealth
{
    private FruitCharacter _fruitCharacter;

    private void Awake()
    {
        _fruitCharacter = GetComponent<FruitCharacter>();
        _currentHealth = maxHealth;
    }

    protected override void OnHealthChanged(int oldHealth, int newHealth)
    {
        base.OnHealthChanged(oldHealth, newHealth);
        // TODO: Update UI (health bar, damage effect)
    }

    protected override void OnHealthDepleted()
    {
        StartCoroutine(HandleRespawnRoutine());
    }

    private IEnumerator HandleRespawnRoutine()
    {
        _fruitCharacter.DisableCharacter();
        RpcSetVisibility(false);

        yield return new WaitForSeconds(1f);

        //Transform spawn = GameManager.Instance.GetRandomSpawn(); // Implement this in GameManager
        //transform.SetPositionAndRotation(spawn.position, spawn.rotation);

        _currentHealth = maxHealth;

        RpcSetVisibility(true);
        _fruitCharacter.EnableCharacter();
    }

    [ClientRpc]
    private void RpcSetVisibility(bool visible)
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = visible;
    }
}
