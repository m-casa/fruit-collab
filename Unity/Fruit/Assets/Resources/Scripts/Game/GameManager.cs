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

    #endregion

    #region MONOBEHAVIOR

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

    #endregion

    #region METHODS

    public void SelectCharacter(string characterName)
    {
        if (IsCharacterAvailable(characterName))
        {
            _selectedCharacters.Add(characterName); // Mark character as taken

            CameraManager.Instance.TransitionToLobby();
            SpawnCharacter(characterName);
            _inGame = true;

            Debug.Log($"{characterName} selected.");
        }
        else
        {
            Debug.LogWarning($"{characterName} is already taken.");
        }
    }

    public void DeselectCharacter(string characterName)
    {
        if (_selectedCharacters.Contains(characterName))
        {
            _selectedCharacters.Remove(characterName); // Mark character as available
            Debug.Log($"{characterName} deselected.");
        }
    }

    public List<string> GetAvailableCharacters()
    {
        // Return all characters that are not in the selected list
        return _allCharacters.FindAll(character => IsCharacterAvailable(character));
    }

    public void SpawnCharacter(string characterName)
    {

    }

    public bool InGame()
    {
        return _inGame;
    }

    public void StartMatch()
    {
        _remainingTime = _matchDuration;
        StartCoroutine(MatchTimer());
    }

    public void GetPlayerScores()
    {
        // Return each player's score
    }

    private bool IsCharacterAvailable(string characterName)
    {
        // The "!" negates the result, meaning it checks if the character is not already taken
        return !_selectedCharacters.Contains(characterName);
    }

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

    private void EndMatch()
    {
        // Determine winner and transition back to the lobby
        int highestScore = Mathf.Max(_playerScores);
        int winnerIndex = System.Array.IndexOf(_playerScores, highestScore);
        ShowWinner(winnerIndex);
    }

    private void ShowWinner(int playerIndex)
    {
        // Display crown on the winning player
        Debug.Log($"Player {playerIndex + 1} wins!");
    }

    #endregion
}
