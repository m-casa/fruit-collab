using EasyCharacterMovement;
using Mirror;
using UnityEngine;

public class PowerUp : NetworkBehaviour
{
    private enum PowerUpType { SpeedBoost, Launcher, Damage }
    private Vector3 direction;

    [SerializeField] private PowerUpType powerUpType;

    void Awake()
    {
        Invoke(nameof(DespawnPowerUp), 6f);
    }

    private void DespawnPowerUp()
    {
        Destroy(transform.parent.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out NetworkIdentity identity))
        {
            // Check if this player is the host/authoritative server                                                                                                                                     
            if (identity.connectionToClient.connectionId == 0)
            {
                FruitCharacter hostCharacter = identity.GetComponentInParent<FruitCharacter>();
                if (hostCharacter != null)
                {
                    // Host applies effect directly on server                                                                                                                                                                       
                    switch (powerUpType)
                    {
                        case PowerUpType.SpeedBoost:
                            hostCharacter.BoostSpeed(5f);
                            break;
                        case PowerUpType.Launcher:
                            hostCharacter.QueueLaunch();
                            break;
                        //case PowerUpType.Damage:
                        //    player.combat.RpcStartKnockback(0.2f, direction); // Example value
                        //    break;
                    }
                }
            }
            else
            {
                // Aapply effect to client                                                                                                                                                                   
                RpcApplyPowerUp(identity);
            }

            Destroy(transform.parent.gameObject);
        }
    }

    [ClientRpc]
    private void RpcApplyPowerUp(NetworkIdentity identity)
    {
        if (identity.isLocalPlayer)
        {
            FruitCharacter fruitChar = identity.GetComponentInParent<FruitCharacter>();
            
            switch (powerUpType)
            {
                case PowerUpType.SpeedBoost:
                    fruitChar.BoostSpeed(5f);
                    break;
                case PowerUpType.Launcher:
                    fruitChar.QueueLaunch();
                    break;
                //case PowerUpType.Damage:
                //    player.combat.RpcStartKnockback(0.2f, direction); // Example value
                //    break;
            }
        }
    }
}
