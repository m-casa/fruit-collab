using EasyCharacterMovement;
using UnityEngine;

public class PowerUp : MonoBehaviour
{
    private enum PowerUpType { SpeedBoost, Launcher, Damage }
    private Vector3 direction;

    [SerializeField] private PowerUpType powerUpType;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FruitCharacter player = other.GetComponent<FruitCharacter>();
            if (player != null)
            {
                direction = (other.transform.position - transform.position).normalized;

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
                player.BoostSpeed(5f);
                break;
            case PowerUpType.Launcher:
                player.QueueLaunch();
                break;
            case PowerUpType.Damage:
                player.TakeDamage(0.2f, direction); // Example value
                break;
        }
    }
}
