using Mirror;
using System.Collections;
using System.Linq;
using UnityEngine;

public class PowerUpSpawner : NetworkBehaviour
{
    [Header("Power Up Prefabs")]
    [SerializeField] private GameObject[] _powerUpPrefabs; // Drag all power up prefabs here

    [Header("Settings")]
    [SerializeField] private float _minSpawnTime = 5f;
    [SerializeField] private float _maxSpawnTime = 10f;

    [Header("State")]
    private bool _isSpawning = false;
    private Coroutine _spawnCoroutine;

    #region MONOBEHAVIOR

    void Awake()
    {
        // Deactivate by default until match starts
        _isSpawning = false;
    }

    #endregion

    #region MATCH CONTROL

    /// <summary>
    /// Called when the match starts to activate spawning.
    /// </summary>
    
    public void StartMatch()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }

        StartSpawningRoutine();
        _isSpawning = true;
        Debug.Log("PowerUpSpawner activated!");
    }

    /// <summary>
    /// Called when the match ends to deactivate spawning.
    /// </summary>
    
    public void EndMatch()
    {
        _isSpawning = false;

        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }

        Debug.Log("PowerUpSpawner deactivated!");
    }

    #endregion

    #region SPAWN LOGIC

    /// <summary>
    /// Starts the coroutine that randomly spawns power ups during the match.
    /// </summary>
    
    private void StartSpawningRoutine()
    {
        _spawnCoroutine = StartCoroutine(SpawnPowerUpLoop());
    }

    /// <summary>
    /// Coroutine that waits a random amount of time and then spawns a power up.
    /// </summary>
    
    private IEnumerator SpawnPowerUpLoop()
    {
        while (_isSpawning)
        {
            // Wait a random time between minSpawnTime and maxSpawnTime
            float randomDelay = Random.Range(_minSpawnTime, _maxSpawnTime);
            yield return new WaitForSeconds(randomDelay);

            // Spawn the power up
            SpawnRandomPowerUp();
        }
    }

    /// <summary>
    /// Spawns a random power up above a random player's head.
    /// </summary>
    
    private void SpawnRandomPowerUp()
    {
        if (!_isSpawning) return;

        if (_powerUpPrefabs == null || _powerUpPrefabs.Length == 0)
        {
            Debug.LogWarning("No power up prefabs assigned in PowerUpSpawner.");
            return;
        }

        // Get all networked players
        NetworkPlayer[] networkPlayers = GameManager.Instance.GetAllNetworkedPlayers();

        if (networkPlayers == null || networkPlayers.Length == 0)
        {
            Debug.LogWarning("No active players found. Skipping power up spawn.");
            return;
        }

        // Select a random player
        int randomPlayerIndex = Random.Range(0, networkPlayers.Length);
        NetworkPlayer randomPlayer = networkPlayers[randomPlayerIndex];

        // Get the player's transform and position above their head
        Transform playerTransform = randomPlayer.GetCharacter().transform;
        Vector3 spawnPosition = playerTransform.position + new Vector3(0, 2f, 0);
        Quaternion spawnRotation = playerTransform.rotation;

        // Select a random power up prefab
        int randomPrefabIndex = Random.Range(0, _powerUpPrefabs.Length);
        GameObject selectedPrefab = _powerUpPrefabs[randomPrefabIndex];

        // Spawn the power up
        if (selectedPrefab != null)
        {
            GameObject spawnedPowerUp = Instantiate(selectedPrefab, spawnPosition, spawnRotation);
            Debug.Log($"Spawned power up above player {randomPlayer.connectionToClient.connectionId}");
        }
        else
        {
            Debug.LogWarning($"Selected power up prefab at index {randomPrefabIndex} is null.");
        }
    }

    #endregion
}
