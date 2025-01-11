using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class MenuManager : MonoBehaviour
{
    #region FIELDS

    [Header("Camera")]
    [SerializeField] private CameraManager _cameraManager;

    [Header("Input")]
    [SerializeField] private InputActionAsset _inputActions;

    [Header("UI References")]
    [SerializeField] private GameObject _startScreen; // UI for "Press Any Button to Start"
    [SerializeField] private GameObject _mainMenu; // Main menu UI
    [SerializeField] private GameObject _pauseMenu; // Pause menu UI

    [Header("Game States")]
    private bool isGamePaused = false;

    #endregion

    #region INPUT ACTIONS

    private InputAction navigationInputAction { get; set; }

    private InputAction selectInputAction { get; set; }

    #endregion

    #region INPUT ACTION HANDLERS

    private Vector2 GetNavigationInput()
    {
        return navigationInputAction?.ReadValue<Vector2>() ?? Vector2.zero;
    }

    private void OnSelect(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
            return;
            //Select();
    }

    #endregion

    #region METHODS

    private void OnEnable()
    {
        InitPlayerInput();
    }

    private void OnDisable()
    {
        DeinitPlayerInput();
    }

    private void Start()
    {
        // Show the start screen initially
        _startScreen.SetActive(true);
        _mainMenu.SetActive(false);
        _pauseMenu.SetActive(false);
    }

    private void InitPlayerInput()
    {
        // Attempts to cache Character InputActions (if any)
        if (_inputActions == null)
            return;

        // Navigation input action (no handler, this is polled, e.g. GetNavigationInput())
        navigationInputAction = _inputActions.FindAction("Navigation");
        navigationInputAction?.Enable();

        // Setup select input action handlers
        selectInputAction = _inputActions.FindAction("Select");
        if (selectInputAction != null)
        {
            selectInputAction.started += OnSelect;
            selectInputAction.performed += OnSelect;
            selectInputAction.canceled += OnSelect;

            selectInputAction.Enable();
        }
    }

    private void DeinitPlayerInput()
    {
        // Unsubscribe from input action events and disable input actions

        if (navigationInputAction != null)
        {
            navigationInputAction.Disable();
            navigationInputAction = null;
        }

        if (selectInputAction != null)
        {
            selectInputAction.started -= OnSelect;
            selectInputAction.performed -= OnSelect;
            selectInputAction.canceled -= OnSelect;

            selectInputAction.Disable();
            selectInputAction = null;
        }
    }

    private void Update()
    {
        Vector2 navigationInput = GetNavigationInput();

        // Detect any button press for the start screen
        if (_startScreen.activeSelf && IsAnyInputPressed())
        {
            OnStartGame();
        }

        // Detect pause input (e.g., ESC or Start button on a controller)
        if (!_startScreen.activeSelf && Keyboard.current.escapeKey.wasPressedThisFrame || 
            Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
        {
            //TogglePauseMenu();
        }
    }

    private bool IsAnyInputPressed()
    {
        // Check keyboard input
        if (Keyboard.current.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        // Check gamepad input
        if (Gamepad.current != null)
        {
            foreach (var control in Gamepad.current.allControls)
            {
                if (control is ButtonControl button && button.wasPressedThisFrame)
                {
                    return true;
                }
            }
        }

        return false; // No input detected
    }

    private void OnStartGame()
    {
        _startScreen.SetActive(false);
        _mainMenu.SetActive(true);

        _cameraManager.TransitionToMainMenu();
        // Additional logic for starting the game (e.g., enabling player controls).
    }

    public void OnHostGame()
    {
        _mainMenu.SetActive(false);

        _cameraManager.TransitionToCharacterSelect();
        // Logic for hosting a game...
    }

    public void TogglePauseMenu()
    {
        isGamePaused = !isGamePaused;

        if (isGamePaused)
        {
            Time.timeScale = 0f; // Pause game time
            _pauseMenu.SetActive(true);
            // Disable player controls if needed
        }
        else
        {
            Time.timeScale = 1f; // Resume game time
            _pauseMenu.SetActive(false);
            // Enable player controls
        }
    }

    public void OnResumeGame()
    {
        // Unpause the game explicitly (used by a Resume button)
        isGamePaused = false;
        Time.timeScale = 1f;
        _pauseMenu.SetActive(false);
        // Re-enable player controls
    }

    public void OnLeaveLobby()
    {
        // Logic for exiting the lobby
    }

    public void OnSettingsSelected()
    {
        // Logic for selecting the settings
    }

    public void OnExitGame()
    {
        // Logic for exiting the game
        Application.Quit();
    }

    #endregion
}
