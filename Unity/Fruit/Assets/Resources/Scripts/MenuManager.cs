using UnityEngine;
using UnityEngine.InputSystem;

public class MenuManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject startScreenUI;      // UI for "Press Any Button to Start"
    public GameObject mainMenuUI;        // Main menu UI
    public GameObject pauseMenuUI;       // Pause menu UI

    [Header("Game States")]
    private bool isGamePaused = false;

    private void Start()
    {
        // Show the start screen initially
        //startScreenUI.SetActive(true);
        //mainMenuUI.SetActive(false);
        //pauseMenuUI.SetActive(false);
    }

    private void Update()
    {
        // Detect any button press for the start screen
        //if (startScreenUI.activeSelf && 
        //    (Keyboard.current.anyKey.wasPressedThisFrame || Gamepad.current != null && Gamepad.current.allControls.Any(c => c.wasPressedThisFrame)))
        //{
        //    OnStartGame();
        //}

        // Detect pause input (e.g., ESC or Start button on a controller)
        //if (!startScreenUI.activeSelf && Keyboard.current.escapeKey.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame)
        //{
        //    TogglePauseMenu();
        //}
    }

    public void OnStartGame()
    {
        startScreenUI.SetActive(false);
        mainMenuUI.SetActive(true);
        // Additional logic for starting the game (e.g., enabling player controls).
    }

    public void OnHostGame()
    {
        mainMenuUI.SetActive(false);
        // Logic for hosting a game...
    }

    public void TogglePauseMenu()
    {
        isGamePaused = !isGamePaused;

        if (isGamePaused)
        {
            Time.timeScale = 0f; // Pause game time
            pauseMenuUI.SetActive(true);
            // Disable player controls if needed
        }
        else
        {
            Time.timeScale = 1f; // Resume game time
            pauseMenuUI.SetActive(false);
            // Enable player controls
        }
    }

    public void OnResumeGame()
    {
        // Unpause the game explicitly (used by a Resume button)
        isGamePaused = false;
        Time.timeScale = 1f;
        pauseMenuUI.SetActive(false);
        // Re-enable player controls
    }

    public void OnExitGame()
    {
        // Logic for exiting the game
        Application.Quit();
    }
}
