using EasyCharacterMovement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region FIELDS

    public static GameManager Instance { get; private set; }

    [Header("Game States")]
    [SerializeField] private List<string> _allCharacters = new List<string> { "Apple", "Grape", "Lemon", "Peach" };
    private List<string> _selectedCharacters = new List<string>(); // Tracks selected characters
    private int[] _playerScores; // Scores for each player
    private bool _inGame;

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

        // When our new scene loads, don't delete the game manager
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Initializes the array length of the amount of characters and their props.
    /// </summary>

    void Start()
    {
        _spawnedFruits = new GameObject[_fruitPrefabs.Length];
        _spawnedCharacters = new GameObject[_characterPrefabs.Length];
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Spawns the necessary game objects in charge of character selection logic.
    /// </summary>

    public void StartSelection()
    {
        SpawnFruit();

        SpawnArrow();

        // Additional setup for the arrow if needed
        Debug.Log("Fruits/arrow spawned for character selection.");
    }

    /// <summary>
    /// If available, the selected character will be spawned 
    ///  and the associated props will be destroyed.
    /// </summary>

    public void SelectCharacter(string characterName)
    {
        if (IsCharacterAvailable(characterName))
        {
            Debug.Log($"{characterName} selected.");

            DestroyArrow();

            DestroyFruit(characterName);

            _selectedCharacters.Add(characterName); // Mark character as taken
            SpawnCharacter(characterName);

            CameraManager.Instance.TransitionToLobby();

            _inGame = true;
        }
        else
        {
            Debug.LogWarning($"{characterName} is already taken.");
        }
    }

    /// <summary>
    /// Marks the appropriate character as available if they were taken.
    /// </summary>

    public void MarkAsAvailable(string characterName)
    {
        if (_selectedCharacters.Contains(characterName))
        {
            _selectedCharacters.Remove(characterName); // Mark character as available

            Debug.Log($"{characterName} deselected.");
        }
    }

    /// <summary>
    /// Returns a list of available characters.
    /// </summary>

    public List<string> GetAvailableCharacters()
    {
        // Return all characters that are not in the selected list
        return _allCharacters.FindAll(character => IsCharacterAvailable(character));
    }

    /// <summary>
    /// Returns true or false for whether or not the player is in game.
    /// </summary>

    public bool InGame()
    {
        return _inGame;
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
        return !_selectedCharacters.Contains(characterName);
    }

    /// <summary>
    /// Spawns the base fruit bodies to represent each character.
    /// </summary>

    private void SpawnFruit()
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

            // Spawn the fruit and store it in the array
            GameObject spawnedFruit = Instantiate(fruit);
            _spawnedFruits[fruitIndex] = spawnedFruit;
            fruitIndex++;
        }
    }

    /// <summary>
    /// Spawns the arrow game object which has the logic needed to select a character.
    /// </summary>

    private void SpawnArrow()
    {
        if (_arrowPrefab == null)
        {
            Debug.LogError("Arrow Prefab is not assigned in the GameManager.");

            return;
        }

        _spawnedArrow = Instantiate(_arrowPrefab);
    }

    /// <summary>
    /// Spawns the selected character along with any needed setup.
    /// </summary>

    private void SpawnCharacter(string characterName)
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
            // Spawn the character and store it in the array
            GameObject spawnedCharacter = Instantiate(_characterPrefabs[characterIndex]);
            _spawnedCharacters[characterIndex] = spawnedCharacter;

            // Setup the character's reference to the main camera
            FruitCharacter fruitCharacter = spawnedCharacter.GetComponent<FruitCharacter>();
            fruitCharacter.camera = Camera.main;

            CameraManager.Instance.AddPlayerToCamera(spawnedCharacter);

            Debug.Log($"{characterName} spawned.");
        }
        else
        {
            Debug.LogError($"Prefab for character {characterName} not found.");
        }
    }

    /// <summary>
    /// Destroys the arrow game object after a character has been selected.
    /// </summary>

    private void DestroyArrow()
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

            Debug.Log($"Fruit {characterName} destroyed.");
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

    #endregion
}
