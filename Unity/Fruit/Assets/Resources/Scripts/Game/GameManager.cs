using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    #region FIELDS

    public static GameManager Instance { get; private set; }

    [SyncVar] private ArenaState _arenaState = ArenaState.Static;
    [SyncVar] private int _staticArenaIndex = 0;
    [SyncVar] private int _transitionArenaIndex = 0;
    [SyncVar] private bool _matchActive;

    private enum ArenaState
    {
        Static,
        Transition
    }

    [Header("Game States")]
    [SerializeField] private List<string> _allCharacters = new List<string> { "Apple", "Grape", "Lemon", "Peach" };
    [SerializeField] private SyncDictionary<int, int> _playerScores = new SyncDictionary<int, int>(); // Scores for each player
    private HashSet<string> _claimedCharacters = new HashSet<string>(); // Tracks taken characters
    private HashSet<NetworkConnectionToClient> _readyPlayers = new HashSet<NetworkConnectionToClient>();
    private List<Transform> _lobbySpawns, _matchSpawns;

    [SyncVar(hook = nameof(OnClaimedCharactersSyncUpdated))]
    private string _claimedCharactersSync = ""; // Sync'd string for character selection (CSV format)

    [Header("Timers")]
    private Coroutine _startCountdownRoutine;
    [SerializeField] private float _matchDuration = 150f; // 150 seconds is 2:30 minutes
    private float _remainingTime;

    [Header("Fruit Prefabs")]
    [SerializeField] private GameObject[] _fruitPrefabs; // Reference to the fruit prefabs
    private GameObject[] _spawnedFruits; // Reference to the spawned fruits

    [Header("Character Prefabs")]
    [SerializeField] private GameObject[] _characterPrefabs; // Reference to the character prefabs
    private GameObject[] _spawnedCharacters; // Reference to the spawned characters

    [Header("Arrow Prefab")]
    [SerializeField] private GameObject _arrowPrefab; // Reference to the arrow prefab
    private GameObject _spawnedArrow; // Reference to the spawned arrow

    [Header("Arena Timing")]
    [SerializeField] private float _timeBeforeArenaMove = 30f;
    [SerializeField] private float _arenaMoveDuration = 15f;
    
    private Coroutine _arenaRoutine;

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

        // When our new scene loads, don't delete the game manager
        DontDestroyOnLoad(gameObject);

        _matchActive = false;

        // Set the array lengths early on to avoid null references
        _spawnedFruits = new GameObject[_fruitPrefabs.Length];
        _spawnedCharacters = new GameObject[_characterPrefabs.Length];

        _lobbySpawns = FindObjectsByType<LobbySpawn>(FindObjectsSortMode.None)
                        .OrderBy(sp => sp.lobbySpawnIndex)
                        .Select(sp => sp.transform)
                        .ToList();

        _matchSpawns = FindObjectsByType<MatchSpawn>(FindObjectsSortMode.None)
                        .OrderBy(sp => sp.matchSpawnIndex)
                        .Select(sp => sp.transform)
                        .ToList();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Initialize score tracking when a player joins.
    /// </summary>
   
    public void RegisterPlayer(NetworkConnectionToClient target)
    {
        if (!_playerScores.ContainsKey(target.connectionId))
        {
            _playerScores[target.connectionId] = 0;
            Debug.Log($"Registered player {target.connectionId} with score 0");
        }
    }

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
    /// Check if a match is currently active.
    /// </summary>

    public bool MatchActive()
    {
        return _matchActive;
    }

    /// <summary>
    /// Will ready/unready the client that sent the request;
    /// If all clients are ready, begin the countdown to start the match.
    /// If not all client are ready and the countdown is active, stop it.
    /// </summary>

    public void ToggleReadyStatus(NetworkConnectionToClient target)
    {
        if (_readyPlayers.Contains(target))
        {
            _readyPlayers.Remove(target);
            Debug.Log($"Player {target.connectionId} is now UNREADY");

            if (_startCountdownRoutine != null)
            {
                StopCoroutine(_startCountdownRoutine);
                _startCountdownRoutine = null;

                Debug.Log("Countdown canceled, someone unreadied.");
            }
        }
        else
        {
            _readyPlayers.Add(target);
            Debug.Log($"Player {target.connectionId} is now READY");

            CheckAllPlayersReady();
        }

        RpcUpdateReadyStatus(target.connectionId, _readyPlayers.Contains(target));
    }

    /// <summary>
    /// Notify all clients that the specified player is ready.
    /// </summary>

    [ClientRpc]
    public void RpcUpdateReadyStatus(int connectionId, bool state)
    {
        // TODO: Just use the new state to update the UI for the specified player.
    }

    /// <summary>
    /// Returns the current arena.
    /// </summary>

    public ArenaArea GetCurrentArena()
    {
        if (_arenaState == ArenaState.Transition)
            return CameraManager.Instance.GetTransitionArena(_transitionArenaIndex);

        return CameraManager.Instance.GetStaticArena(_staticArenaIndex);
    }

    /// <summary>
    /// Update the specified player's score.
    /// </summary>

    public void UpdatePlayerScore(NetworkConnectionToClient target, int amount)
    {
        if (_playerScores.ContainsKey(target.connectionId))
        {
            _playerScores[target.connectionId] += amount;
            Debug.Log($"Player {target.connectionId} score updated to {_playerScores[target.connectionId]}");

            // Update all clients with the new score
            RpcUpdateScoreDisplay();
        }
    }

    /// <summary>
    /// Get a player's current score
    /// </summary>

    public int GetPlayerScore(NetworkConnectionToClient target)
    {
        return _playerScores.ContainsKey(target.connectionId) ? _playerScores[target.connectionId] : 0;
    }

    /// <summary>
    /// Get all player scores for display
    /// </summary>
    
    public Dictionary<int, int> GetAllPlayerScores()
    {
        var scores = new Dictionary<int, int>();
        foreach (var kvp in _playerScores)
        {
            scores[kvp.Key] = kvp.Value;
        }
        return scores;
    }

    /// <summary>
    /// Destroys every fruit prefab; Used when leaving a lobby.
    /// </summary>

    public void DestroyAllFruit()
    {
        // Check if the array has been initialized
        if (_spawnedFruits != null)
        {
            foreach (GameObject fruit in _spawnedFruits)
            {
                // Only attempt to destroy prefabs that aren't null
                if (fruit == null)
                    continue;

                Destroy(fruit);

                Debug.Log($"Fruit {fruit} prefab destroyed.");
            }
        }
        else
        {
            Debug.LogWarning($"No active fruit instances to destroy.");
        }
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
            // Spawn the character on the server and store it in an array
            GameObject spawnedCharacter = Instantiate(_characterPrefabs[characterIndex]);
            _spawnedCharacters[characterIndex] = spawnedCharacter;

            // Spawn the character on the network for the other clients
            NetworkServer.Spawn(spawnedCharacter, target);

            // Reference to the spawned character's network identity
            NetworkIdentity characterIdentity = spawnedCharacter.GetComponent<NetworkIdentity>();

            // Set a reference to the client's character in a syncvar
            target.identity.GetComponent<NetworkPlayer>().SetCharacter(characterIdentity);

            // Assign the spawned character to the client that requested it
            NetworkServer.ReplacePlayerForConnection(target, spawnedCharacter, ReplacePlayerOptions.KeepAuthority);

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
        CameraManager.Instance.MoveCameraToPlayerFollow();
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
    /// Destroys the fruit prefab associated with the chosen character.
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
    /// Check if all players in the server are ready.
    /// </summary>

    private void CheckAllPlayersReady()
    {
        if (_readyPlayers.Count == NetworkServer.connections.Count)
        {
            if (_startCountdownRoutine == null)
            {
                Debug.Log("All players ready. Starting countdown...");
                _startCountdownRoutine = StartCoroutine(CountdownTimer(3f));
            }
        }
    }

    /// <summary>
    /// Begins a contdown to start the match.
    /// </summary>

    private IEnumerator CountdownTimer(float timer)
    {
        while (timer > 0)
        {
            Debug.Log("Game starts in " + timer);
            yield return new WaitForSeconds(1f);
            timer--;
        }

        StartMatch();
    }

    /// <summary>
    /// Starts the match by initiating the timer and match logic.
    /// </summary>

    private void StartMatch()
    {
        NetworkPlayer[] networkPlayers = GetAllNetworkedPlayers();

        if (_arenaRoutine != null)
        {
            StopCoroutine(_arenaRoutine);
            _arenaRoutine = null;
        }

        _arenaRoutine = StartCoroutine(ArenaFlowRoutine());
        _matchActive = true;
        _startCountdownRoutine = null;
        _readyPlayers.Clear();


        MovePlayersToMatch(networkPlayers);
        StartCoroutine(MatchTimer());

        Debug.Log("Match Started!");
    }

    /// <summary>
    /// Returns each networked player.
    /// </summary>

    public NetworkPlayer[] GetAllNetworkedPlayers()
    {
        return FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
    }

    /// <summary>
    /// Waits before attempting to move the camera to the next arena.
    /// </summary>

    private IEnumerator ArenaFlowRoutine()
    {
        // Always start on the first arena
        _staticArenaIndex = 0;
        _transitionArenaIndex = 0;
        _arenaState = ArenaState.Static;

        while (_transitionArenaIndex < CameraManager.Instance.TransitionCount)
        {
            // Wait before moving the camera
            yield return new WaitForSeconds(_timeBeforeArenaMove);

            // Enter transition state so constraints use transition arena
            _arenaState = ArenaState.Transition;

            // Tell the camera to move to the next arena (broadcasts transition to all clients)
            RpcMoveCameraToNextArena(
                _transitionArenaIndex,
                _arenaMoveDuration
            );

            // Wait for camera movement to finish
            yield return new WaitForSeconds(_arenaMoveDuration);

            // Advance arena index
            _staticArenaIndex++;
            _transitionArenaIndex++;

            // Lock into the new static arena
            _arenaState = ArenaState.Static;
        }

        if (_arenaRoutine != null)
        {
            StopCoroutine(_arenaRoutine);
            _arenaRoutine = null;
        }
    }

    [ClientRpc]
    private void RpcMoveCameraToNextArena(int transitionIndex, float duration)
    {
        CameraManager.Instance.MoveCameraToNextArena(
                transitionIndex,
                duration
        );
    }

    /// <summary>
    /// Moves players to random spawn points in the map.
    /// </summary>

    private void MovePlayersToMatch(NetworkPlayer[] networkPlayers)
    {
        int i = 0;
        List<Transform> shuffledSpawns = _matchSpawns.OrderBy(x => Random.value).ToList();

        foreach (NetworkPlayer networkPlayer in networkPlayers)
        {
            if (i >= shuffledSpawns.Count) break;

            Transform spawn = shuffledSpawns[i];
            NetworkConnectionToClient conn = networkPlayer.connectionToClient;

            networkPlayer.RpcTeleportCharacter(conn, spawn.position, spawn.rotation);

            Debug.Log($"Teleporting player with conn {conn.connectionId} to spawn {i}");
            i++;
        }
    }

    /// <summary>
    /// Logic for the match timer.
    /// </summary>

    private IEnumerator MatchTimer()
    {
        _remainingTime = _matchDuration;

        while (_remainingTime > 0)
        {
            if (_remainingTime <= 3f)
            {
                Debug.Log("Game ends in " + _remainingTime);
            }

            UIManager.Instance.UpdateTimer(_remainingTime); // Implement this to update the timer display

            yield return new WaitForSeconds(1f);

            _remainingTime--;
        }

        EndMatch();
    }

    /// <summary>
    /// Update all clients with current scores
    /// </summary>

    [ClientRpc]
    private void RpcUpdateScoreDisplay()
    {
        // Update UI with current scores
        UIManager.Instance.UpdateScoreDisplay(GetAllPlayerScores());
    }

    /// <summary>
    /// Sets the lobby back up and awards the highest scoring player.
    /// </summary>

    private void EndMatch()
    {
        Debug.Log("Match Ended!");

        NetworkPlayer[] networkPlayers = GetAllNetworkedPlayers();
        string winner = DetermineWinner();
        Vector3 originalRotation = new Vector3(30, 180, 0);

        if (_arenaRoutine != null)
        {
            StopCoroutine(_arenaRoutine);
            _arenaRoutine = null;
        }

        RpcResetArenasAndCameras(originalRotation);

        ResetAllScores(); // Reset scores between matches
        ShowWinner(winner);
        ResetHealthForAllPlayers(networkPlayers);
        MovePlayersToLobby(networkPlayers);

        _matchActive = false;
    }

    [ClientRpc]
    private void RpcResetArenasAndCameras(Vector3 originalRotation)
    {
        CameraManager.Instance.ResetTransitionArenas();
        StartCoroutine(CameraManager.Instance.RotateTransitionCameras(originalRotation));
    }

    /// <summary>
    /// Determine the winner based on highest score.
    /// </summary>
    
    private string DetermineWinner()
    {
        string winner = null;
        int highestScore = -1;

        foreach (var kvp in _playerScores)
        {
            if (kvp.Value == highestScore)
            {
                winner = "tie";
            }
            else if (kvp.Value > highestScore)
            {
                highestScore = kvp.Value;
                winner = kvp.Key.ToString();
            }
        }

        return winner;
    }

    /// <summary>
    /// The highest scoring player is marked as the winner.
    /// </summary>

    private void ShowWinner(string winner)
    {
        if (winner != null)
        {
            if (winner == "tie")
            {
                Debug.Log("Tie!");
            }
            else
            {
                int winnerConnectionId = int.Parse(winner);

                if (NetworkServer.connections.TryGetValue(winnerConnectionId, out NetworkConnectionToClient winnerConn))
                {
                    // Display crown on the winning player
                    Debug.Log($"Player {winnerConn.connectionId + 1} wins!");
                }
            }
        }
    }

    /// <summary>
    /// Reset each player's health to default.
    /// </summary>

    private void ResetHealthForAllPlayers(NetworkPlayer[] networkPlayers)
    {
        foreach (NetworkPlayer networkPlayer in networkPlayers)
        {
            if (networkPlayer != null)
            {
                var character = networkPlayer.GetCharacter();
                if (character != null)
                {
                    var health = character.GetComponent<NetworkCharacterHealth>();
                    if (health != null)
                    {
                        health.ResetHealth();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Reset all scores for a new match.
    /// </summary>

    private void ResetAllScores()
    {
        var keys = _playerScores.Keys.ToList();
        foreach (var conn in keys)
        {
            _playerScores[conn] = 0;
        }
        RpcUpdateScoreDisplay();
    }

    /// <summary>
    /// Moves players to random spawn points in the lobby.
    /// </summary>

    private void MovePlayersToLobby(NetworkPlayer[] networkPlayers)
    {
        int i = 0;
        List<Transform> shuffledSpawns = _lobbySpawns.OrderBy(x => Random.value).ToList();

        foreach (NetworkPlayer networkPlayer in networkPlayers)
        {
            if (i >= shuffledSpawns.Count) break;

            Transform spawn = shuffledSpawns[i];
            NetworkConnectionToClient conn = networkPlayer.connectionToClient;

            networkPlayer.RpcTeleportCharacter(conn, spawn.position, spawn.rotation);

            Debug.Log($"Teleporting player with conn {conn.connectionId} to spawn {i}");
            i++;
        }
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
