using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public string selectedCharacter; // Name or ID of the selected character

    private void Awake()
    {
        // Ensure there's only one GameManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetSelectedCharacter(string characterName)
    {
        selectedCharacter = characterName;
        Debug.Log($"Selected Character: {selectedCharacter}");
    }

    public string GetSelectedCharacter()
    {
        return selectedCharacter;
    }
}
