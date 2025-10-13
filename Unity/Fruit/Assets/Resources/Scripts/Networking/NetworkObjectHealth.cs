using Mirror;
using UnityEngine;

public class NetworkObjectHealth : NetworkHealth
{
    [SerializeField] private GameObject destructionEffectPrefab;

    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    protected override void OnHealthChanged(int oldHealth, int newHealth)
    {
        base.OnHealthChanged(oldHealth, newHealth);

        // Override here to replace texture with visible damage
    }

    protected override void OnHealthDepleted()
    {
        RpcPlayDestructionEffect();
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    private void RpcPlayDestructionEffect()
    {
        if (destructionEffectPrefab != null)
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
    }
}
