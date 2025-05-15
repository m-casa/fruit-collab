using EasyCharacterMovement;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    #region FIELDS

    public static GameManager Instance { get; private set; }

    [Header("Game States")]
    [SerializeField] private List<string> _allCharacters = new List<string> { "Apple", "Grape", "Lemon", "Peach" };
    private HashSet<string> _claimedCharacters = new HashSet<string>(); // Tracks taken characters
    private int[] _playerScores; // Scores for each player

    [SyncVar(hook = nameof(OnClaimedCharactersSyncUpdated))]
    private string _claimedCharactersSync = ""; // Sync'd string for character selection (CSV format)

    [Header("Timers")]
    [SerializeField] private float _matchDuration = 150f; // 2:30 minutes in seconds
    private float _remainingTime;

    [Header("Character Prefabs")]
    [SerializeField] private GameObject[] _characterPrefabs; // Reference to the character prefabs
    private GameObject[] _spawnedCharacters; // Reference to the spawned characters

    [Header("Fruit Prefabs")]
    [SerializeField] private GameObject[] _fruitPrefabs; // Reference to the fruit prefabs
    private GameObject[] _spawnedFruits; // Reference to the spawned fruits

    [Header("Arrow Prefab")]
    [SerializeField] private GameObject _arrowPrefab; // Reference to the arrow prefab
    private GameObject _spawnedArrow; // Reference to the spawned arrow

    #endregion

    #region MONOBEHAVIOR

    /// <summary>
    /// The initialization of this instance.
    /// </summary>

    void Awake()
    {
        // Check if we have an instance of the game manager
        if (Instance != null)
        {
            // If we already have a game manager, destroy this one
            Destroy(gameObject);
        }

        // Set this game manager as the primary instance since we don't have one
        Instance = this;

        // Set the array lengths early on to avoid null references
        _spawnedFruits = new GameObject[_fruitPrefabs.Length];
        _spawnedCharacters = new GameObject[_characterPrefabs.Length];

        // When our new scene loads, don't delete the game manager
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Spawns the necessary game objects in charge of character selection logic.
    /// </summary>
    
    public void StartSelection(NetworkConnectionToClient target)
    {
        RpcSpawnFruit(target);

        RpcSpawnArrow(target);
    }

    /// <summary>
    /// If available, the selected character will be spawned 
    ///  and the associated props will be destroyed.
    /// </summary>

    public void ClaimCharacter(string characterName, NetworkConnectionToClient target)
    {
        if (IsCharacterAvailable(characterName))
        {
            Debug.Log($"{characterName} character selected.");

            // Destroy the target client's character selection arrow
            RpcDestroyArrow(target);

            // Mark character as taken
            _claimedCharacters.Add(characterName);
            UpdateClaimedCharactersSync();

            // Spawn and assign the character to the target client
            SpawnCharacter(characterName, target);

            // Send a reference of all the active characters to the target client's camera
            NetworkIdentity[] characterIdentities = _spawnedCharacters
                .Where(c => c != null)
                .Select(c => c.GetComponent<NetworkIdentity>())
                .ToArray();

            RpcAddAllCharactersToCamera(target, characterIdentities);

            // Move the target client's camera to the lobby
            RpcMoveCameraToLobby(target);
        }
        else
        {
            Debug.LogWarning($"{characterName} is already taken.");
        }
    }

    /// <summary>
    /// Marks the appropriate character as available if they were previously taken.
    /// </summary>

    public void MarkAsAvailable(string characterName)
    {
        if (!IsCharacterAvailable(characterName))
        {
            _claimedCharacters.Remove(characterName); // Mark character as available

            Debug.Log($"{characterName} deselected.");
        }
    }

    /// <summary>
    /// Starts the match by initiating the timer and match logic.
    /// </summary>

    public void StartMatch()
    {
        _remainingTime = _matchDuration;
        StartCoroutine(MatchTimer());
    }

    /// <summary>
    /// Returns the scores for each player.
    /// </summary>

    public void GetPlayerScores()
    {
        // Return each player's score
    }

    /// <summary>
    /// Checks whether or not the specified character is available.
    /// </summary>

    private bool IsCharacterAvailable(string characterName)
    {
        // The "!" negates the result, meaning it checks if the character is not already taken
        return !_claimedCharacters.Contains(characterName);
    }

    /// <summary>
    /// Spawns the base fruit bodies to represent each character.
    /// </summary>

    [TargetRpc]
    private void RpcSpawnFruit(NetworkConnection conn)
    {
        if (_fruitPrefabs == null)
        {
            Debug.LogError("Fruit Prefabs not assigned in the GameManager.");

            return;
        }

        int fruitIndex = 0;

        foreach (GameObject fruit in _fruitPrefabs)
        {
            if (fruit == null)
            {
                Debug.LogWarning($"Fruit prefab at index {fruitIndex} is null. Skipping.");

                fruitIndex++;
                continue;
            }

            // Check if this fruit's corresponding character is already claimed
            string characterName = _allCharacters[fruitIndex]; // Get corresponding character name

            if (!IsCharacterAvailable(characterName))
            {
                Debug.Log($"{characterName} already claimed. Skipping.");
                fruitIndex++;
                continue;
            }

            // Spawn the fruit and store it in the array
            GameObject spawnedFruit = Instantiate(fruit);
            _spawnedFruits[fruitIndex] = spawnedFruit;
            fruitIndex++;
        }

        Debug.Log("Available fruit prefabs spawned for character selection.");
    }

    /// <summary>
    /// Spawns the arrow game object which has the logic needed to select a character.
    /// </summary>
    
    [TargetRpc]
    private void RpcSpawnArrow(NetworkConnection conn)
    {
        if (_arrowPrefab == null)
        {
            Debug.LogError("Arrow Prefab is not assigned in the GameManager.");

            return;
        }

        _spawnedArrow = Instantiate(_arrowPrefab);

        Debug.Log("Arrow spawned for character selection.");
    }

    /// <summary>
    /// Spawns the selected character along with any needed setup.
    /// </summary>

    private void SpawnCharacter(string characterName, NetworkConnectionToClient target)
    {
        // Find the index of the characterName in the _allCharacters list
        int characterIndex = _allCharacters.IndexOf(characterName);

        if (characterIndex == -1)
        {
            Debug.LogError($"Character {characterName} not found in the list of all characters.");

            return;
        }

        // Check if the prefab exists and spawn the character
        if (characterIndex < _characterPrefabs.Length && _characterPrefabs[characterIndex] != null)
        {
            // Spawn the character on the server and store it in the array
            GameObject spawnedCharacter = Instantiate(_characterPrefabs[characterIndex]);
            _spawnedCharacters[characterIndex] = spawnedCharacter;

            // Spawn the character on the network for the other clients
            NetworkServer.Spawn(spawnedCharacter, target);

            // Assign the spawned character to the client that requested it
            NetworkServer.ReplacePlayerForConnection(target, spawnedCharacter, ReplacePlayerOptions.KeepAuthority);

            // Reference to the spawned character's network identity
            NetworkIdentity characterIdentity = spawnedCharacter.GetComponent<NetworkIdentity>();

            // Setup the client's pause menu
            //RpcSetupPauseMenu(target, characterIdentity);

            // Assign the main camera to the target client's character so movement is not broken
            //RpcAssignMainCamera(target, characterIdentity);

            // Send a reference of the newly spawned character to each client's camera
            RpcAddNewCharacterToCamera(characterIdentity);

            Debug.Log($"{characterName} character spawned.");
        }
        else
        {
            Debug.LogError($"Prefab for character {characterName} not found.");
        }
    }

    /// <summary>
    /// Setup the client's pause menu depending on whether they're host.
    /// </summary>

    [TargetRpc]
    private void RpcSetupPauseMenu(NetworkConnection conn, NetworkIdentity characterIdentity)
    {
        UIManager.Instance.SetupPauseMenu(characterIdentity);
    }

    /// <summary>
    /// Setup the character's reference to the main camera.
    /// </summary>

    [TargetRpc]
    private void RpcAssignMainCamera(NetworkConnection conn, NetworkIdentity characterIdentity)
    {
        FruitCharacter fruitCharacter = characterIdentity.GetComponent<FruitCharacter>();
        fruitCharacter.camera = Camera.main;
    }

    /// <summary>
    /// Updates every client's dynamic camera with the newly added character.
    /// </summary>

    [ClientRpc]
    private void RpcAddNewCharacterToCamera(NetworkIdentity identity)
    {
        if (identity != null)
        {
            GameObject character = identity.gameObject; // Convert back to GameObject
            CameraManager.Instance.AddCharacterToCamera(character);
        }
        else
        {
            Debug.LogError("RpcAddNewCharacterToCamera received a null character identity!");
        }
    }

    /// <summary>
    /// Updates the target client's dynamic camera with every active character.
    /// </summary>

    [TargetRpc]
    private void RpcAddAllCharactersToCamera(NetworkConnection conn, NetworkIdentity[] characterIdentities)
    {
        foreach (NetworkIdentity identity in characterIdentities)
        {
            if (identity != null)
            {
                GameObject character = identity.gameObject; // Convert back to GameObject
                CameraManager.Instance.AddCharacterToCamera(character);
            }
            else
            {
                Debug.LogError("RpcAddAllCharactersToCamera received a null character identity!");
            }
        }
    }

    /// <summary>
    /// Moves the target client's camera to the lobby.
    /// </summary>

    [TargetRpc]
    private void RpcMoveCameraToLobby(NetworkConnection conn)
    {
        CameraManager.Instance.MoveCameraToLobby();
    }

    /// <summary>
    /// Destroys the target client's arrow game object.
    /// </summary>

    [TargetRpc]
    private void RpcDestroyArrow(NetworkConnection conn)
    {
        if (_spawnedArrow != null)
        {
            Destroy(_spawnedArrow);

            Debug.Log("Arrow destroyed.");
        }
    }

    /// <summary>
    /// Destroys the fruit associated with the chosen character.
    /// </summary>

    private void DestroyFruit(string characterName)
    {
        // Find the index of the characterName in the _allCharacters list
        int characterIndex = _allCharacters.IndexOf(characterName);

        if (characterIndex == -1)
        {
            Debug.LogError($"Character {characterName} not found in the list of all characters.");

            return;
        }

        // Check if the fruit exists in the array and destroy it
        if (_spawnedFruits[characterIndex] != null)
        {
            Destroy(_spawnedFruits[characterIndex]);
            _spawnedFruits[characterIndex] = null;

            Debug.Log($"Fruit {characterName} prefab destroyed.");
        }
        else
        {
            Debug.LogWarning($"No active fruit instance of {characterName} to destroy.");
        }
    }

    /// <summary>
    /// Destroys the specified character no longer needed.
    /// </summary>

    private void DestroyCharacter(string characterName)
    {
        // Find the index of the characterName in the _allCharacters list
        int characterIndex = _allCharacters.IndexOf(characterName);

        if (characterIndex == -1)
        {
            Debug.LogError($"Character {characterName} not found in the list of all characters.");

            return;
        }

        // Check if the character exists in the array and destroy it
        if (_spawnedCharacters[characterIndex] != null)
        {
            Destroy(_spawnedCharacters[characterIndex]);
            _spawnedCharacters[characterIndex] = null;

            Debug.Log($"{characterName} destroyed.");
        }
        else
        {
            Debug.LogWarning($"No active instance of {characterName} to destroy.");
        }
    }

    /// <summary>
    /// Logic for the match timer.
    /// </summary>

    private IEnumerator MatchTimer()
    {
        while (_remainingTime > 0)
        {
            yield return new WaitForSeconds(1f);
            _remainingTime--;
            UIManager.Instance.UpdateTimer(_remainingTime); // Implement this to update the timer display
        }

        EndMatch();
    }

    /// <summary>
    /// Sets the lobby back up and awards the highest scoring player.
    /// </summary>

    private void EndMatch()
    {
        // Determine winner and transition back to the lobby
        int highestScore = Mathf.Max(_playerScores);
        int winnerIndex = System.Array.IndexOf(_playerScores, highestScore);
        ShowWinner(winnerIndex);
    }

    /// <summary>
    /// The highest scoring player is marked as the winner.
    /// </summary>

    private void ShowWinner(int playerIndex)
    {
        // Display crown on the winning player
        Debug.Log($"Player {playerIndex + 1} wins!");
    }

    /// <summary>
    /// Update the Sync var that keeps track of claimed characters.
    /// </summary>

    private void UpdateClaimedCharactersSync()
    {
        _claimedCharactersSync = string.Join(",", _claimedCharacters);
    }

    /// <summary>
    /// Updates each client's list of claimed characters 
    ///  after the Syn var is updated.
    /// </summary>

    private void OnClaimedCharactersSyncUpdated(string oldValue, string newValue)
    {
        Debug.Log($"Syncing claimed characters: {newValue}");

        // Update local list based on new SyncVar value
        _claimedCharacters.Clear(); // Remove old data
        foreach (string character in newValue.Split(','))
        {
            if (!string.IsNullOrWhiteSpace(character)) // Avoid empty strings
            {
                _claimedCharacters.Add(character);

                DestroyFruit(character);
            }
        }
    }

    #endregion
}
