using EasyCharacterMovement;
using UnityEngine;

public class PowerUp : MonoBehaviour
{
    private enum PowerUpType { SpeedBoost, Launcher, Damage }

    [SerializeField] private PowerUpType powerUpType;
    [SerializeField] private float effectDuration = 5f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FruitCharacter player = other.GetComponent<FruitCharacter>();
            if (player != null)
            {
                ApplyPowerUp(player);
                //Destroy(gameObject); // Remove the power-up object after use
            }
        }
    }

    private void ApplyPowerUp(FruitCharacter player)
    {
        switch (powerUpType)
        {
            case PowerUpType.SpeedBoost:
                player.BoostSpeed();
                break;
            case PowerUpType.Launcher:
                player.QueueLaunch();
                break;
            case PowerUpType.Damage:
                player.TakeDamage(); // Example value
                break;
        }
    }
}
