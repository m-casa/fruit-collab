using HeathenEngineering.SteamworksIntegration;
using Mirror;
using Steamworks;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    #region FIELDS

    public static UIManager Instance { get; private set; }

    private GameObject networkManager;
    private bool _inMainMenu;

    [Header("Network Managers")]
    [SerializeField] private GameObject _networkManagerPrefab;
    [SerializeField] private GameObject _steamNetworkManagerPrefab;

    [Header("Menu References")]
    [SerializeField] private GameObject _startScreen; // UI for "Press Any Button to Start"
    [SerializeField] private GameObject _mainMenu;
    [SerializeField] private GameObject _pauseMenu;
    [SerializeField] private GameObject _settingsMenu;
    [SerializeField] private GameObject _gameplayUI;

    [Header("UI References")]
    [SerializeField] private Text _timerText; // Reference to the timer text
    [SerializeField] private Text[] _playerScoreTexts; // Array of score displays for each player
    [SerializeField] private Slider[] _playerHealthBars; // Array of health bars for each player
    [SerializeField] private RectTransform[] _mainMenuButtons, _pauseMenuButtons;
    [SerializeField] private GameObject _connectingTxt;

    #endregion

    #region EVENTS

    public delegate void UIEventHandler();

    /// <summary>
    /// Event triggered when pause initiated.
    /// </summary>

    public event UIEventHandler PauseToggled;

    #endregion

    #region MONOBEHAVIOR

    /// <summary>
    /// The initialization of this instance.
    /// </summary>

    void Awake()
    {
        // Check if we have an instance of the UI manager
        if (Instance != null)
        {
            // If we already have a UI manager, destroy this one
            Destroy(gameObject);
        }

        // Set this UI manager as the primary instance since we don't have one
        Instance = this;

        // When our new scene loads, don't delete the UI manager
        DontDestroyOnLoad(gameObject);

        _inMainMenu = true;
    }

    /// <summary>
    /// Steam API will initialize after Awake, so check for the API on Start.
    /// </summary>

    void Start()
    {
        // If we're running on a LAN, include a join button
        if (!SteamSettings.Initialized) 
        {
            IncludeJoinButton();
        }
        else
        {
            ExcludeJoinButton();
        }
    }

    /// <summary>
    /// Enable any events we should listen to.
    /// </summary>

    void OnEnable()
    {
        FruitNetworkManager.OnClientDisconnected += HandleClientDisconnected;

        SteamLogic.LobbyJoined += HandleClientConnecting;
        SteamLogic.LobbyFailed += HandleClientDisconnected;
    }

    /// <summary>
    /// Disable any events we are still listening to.
    /// </summary>

    void OnDisable()
    {
        FruitNetworkManager.OnClientDisconnected -= HandleClientDisconnected;

        SteamLogic.LobbyJoined -= HandleClientConnecting;
        SteamLogic.LobbyFailed -= HandleClientDisconnected;
    }

    /// <summary>
    /// Checks every frame for user input on the start screen and when in game.
    /// </summary>

    void Update()
    {
        // Detect any button press for the start screen
        if (_startScreen.activeSelf && IsAnyInputPressed())
        {
            OnStartGame();
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Show the start screen initially.
    /// </summary>

    public void ShowStartScreen()
    {
        SetActivePanel(_startScreen);
    }

    /// <summary>
    /// Host button will call this method.
    /// </summary>

    public void HostGame()
    {
        OnHostGame();
    }

    /// <summary>
    /// Join button will call this method.
    /// NOTE: This button is only used for local play.
    /// </summary>

    public void JoinGame()
    {
        OnJoinGame();
    }

    /// <summary>
    /// Set whether or not the player is in the main menu.
    /// </summary>

    public void SetMainMenuStatus(bool val)
    {
        _inMainMenu = val;
    }

    /// <summary>
    /// Toggles end match on/off depending on whether the player is host.
    /// </summary>

    public void SetupPauseMenu(NetworkIdentity characterIdentity)
    {
        if (characterIdentity.isServer && characterIdentity.isLocalPlayer)
        {
            // Activate the end match button
            _pauseMenuButtons[0].gameObject.SetActive(true);

            // Shift the leave game button down
            Vector3 newAnchoredPosition = _pauseMenuButtons[1].anchoredPosition;
            newAnchoredPosition.y = 0f;
            _pauseMenuButtons[1].anchoredPosition = newAnchoredPosition;
        }
        else
        {
            // Deactivate the end match button
            _pauseMenuButtons[0].gameObject.SetActive(false);

            // Shift the leave game button up
            Vector3 newAnchoredPosition = _pauseMenuButtons[1].anchoredPosition;
            newAnchoredPosition.y = 125f;
            _pauseMenuButtons[1].anchoredPosition = newAnchoredPosition;
        }
    }

    /// <summary>
    /// Toggles the pause menu on and off.
    /// </summary>

    public void TogglePauseMenu()
    {
        if (!_pauseMenu.activeSelf)
        {
            SetActivePanel(_pauseMenu);

            PauseToggled?.Invoke();
        }
        else
        {
            OnResumeGame();
        }
    }

    /// <summary>
    /// Returns whether or not the pause menu is active.
    /// </summary>

    public bool PauseMenuActive()
    {
        return _pauseMenu.activeSelf;
    }

    /// <summary>
    /// Enables gameplay UI when in game.
    /// </summary>

    public void ShowGameplayUI()
    {
        SetActivePanel(_gameplayUI);
    }

    /// <summary>
    /// Resume button calls this method to toggle off the menus.
    /// </summary>

    public void OnResumeGame()
    {
        SetActivePanel(null);

        PauseToggled?.Invoke();
    }

    /// <summary>
    /// Settings button calls this method to enable the settings menu.
    /// </summary>

    public void OnSettingsSelected()
    {
        SetActivePanel(_settingsMenu);
    }

    /// <summary>
    /// Backing out of settings shows the correct UI,
    ///  based on whether the player is in the main menu or in game.
    /// </summary>

    public void OnReturnFromSettings()
    {
        if (_inMainMenu)
        {
            SetActivePanel(_mainMenu);
        }
        else
        {
            TogglePauseMenu();
        }
    }

    /// <summary>
    /// Switches back to the main menu when leaving a lobby.
    /// </summary>

    public void OnLeaveLobby()
    {
        NetworkManager.singleton.StopClient();

        GameManager.Instance.DestroyAllFruit();

        CameraManager.Instance.RemoveAllCharactersFromCamera();

        CameraManager.Instance.MoveCameraToMainMenu();

        SetActivePanel(_mainMenu);
    }

    /// <summary>
    /// Exit game button will call this method.
    /// </summary>

    public void OnExitGame()
    {
        Application.Quit();
    }

    /// <summary>
    /// Updates UI with remining match time.
    /// </summary>

    public void UpdateTimer(float timeRemaining)
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        //_timerText.text = $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// Updates UI with new player scores.
    /// </summary>

    public void UpdatePlayerScore(int playerIndex, int score)
    {
        if (playerIndex < _playerScoreTexts.Length)
        {
            _playerScoreTexts[playerIndex].text = $"P{playerIndex + 1}: {score}";
        }
    }

    /// <summary>
    /// Updates UI with each player's health.
    /// </summary>

    public void UpdatePlayerHealth(int playerIndex, float health)
    {
        if (playerIndex < _playerHealthBars.Length)
        {
            _playerHealthBars[playerIndex].value = health;
        }
    }

    /// <summary>
    /// Moves the main menu buttons down to include a join button when not using Steam.
    /// </summary>

    private void IncludeJoinButton()
    {
        // Activate the join button
        _mainMenuButtons[0].gameObject.SetActive(true);

        // Shift the settings button down
        Vector3 newAnchoredPosition = _mainMenuButtons[1].anchoredPosition;
        newAnchoredPosition.y = 125f;
        _mainMenuButtons[1].anchoredPosition = newAnchoredPosition;

        // Shift the exit button down
        newAnchoredPosition = _mainMenuButtons[2].anchoredPosition;
        newAnchoredPosition.y = 0f;
        _mainMenuButtons[2].anchoredPosition = newAnchoredPosition;
    }

    /// <summary>
    /// Moves the main menu buttons up if the join button is not needed.
    /// </summary>

    private void ExcludeJoinButton()
    {
        // Deactivate the join button
        _mainMenuButtons[0].gameObject.SetActive(false);

        // Shift the settings button up
        Vector3 newAnchoredPosition = _mainMenuButtons[1].anchoredPosition;
        newAnchoredPosition.y = 250f;
        _mainMenuButtons[1].anchoredPosition = newAnchoredPosition;

        // Shift the exit button up
        newAnchoredPosition = _mainMenuButtons[2].anchoredPosition;
        newAnchoredPosition.y = 125f;
        _mainMenuButtons[2].anchoredPosition = newAnchoredPosition;
    }

    /// <summary>
    /// Checks for any keyboard/gamepad input.
    /// </summary>

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

    /// <summary>
    /// Sets the main menu to active and moves the camera.
    /// </summary>

    private void OnStartGame()
    {
        CameraManager.Instance.MoveCameraToMainMenu();

        SetActivePanel(_mainMenu);
    }

    /// <summary>
    /// Sets the passed menu panel as active. Passing null exits all menus.
    /// </summary>

    private void SetActivePanel(GameObject activePanel)
    {
        // Disable all panels
        _startScreen.SetActive(false);
        _mainMenu.SetActive(false);
        _pauseMenu.SetActive(false);
        _settingsMenu.SetActive(false);
        _gameplayUI.SetActive(false);

        if (activePanel != null)
        {
            activePanel.SetActive(true); // Enable the selected panel

            StartCoroutine(SelectFirstButtonNextFrame(activePanel));
        }
    }

    /// <summary>
    /// Ensures there is a selected menu choice at all times.
    /// </summary>

    private IEnumerator SelectFirstButtonNextFrame(GameObject activePanel)
    {
        yield return null; // Wait one frame for UI to rebuild

        Selectable firstSelectable = activePanel.GetComponentInChildren<Selectable>();
        if (firstSelectable != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
        }
    }

    /// <summary>
    /// Handles setup when hosting a game.
    /// </summary>

    private void OnHostGame()
    {
        SetActivePanel(null);

        Host();
    }

    /// <summary>
    /// Handles setup when joining a game.
    /// </summary>

    private void OnJoinGame()
    {
        SetActivePanel(null);

        Join();
    }

    /// <summary>
    /// Host a lobby on our local network.
    /// Use Steam if the API is initialized
    /// </summary>

    private void Host()
    {
        HandleClientConnecting();

        if (SteamSettings.Initialized)
        {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, NetworkManager.singleton.maxConnections);

            return;
        }

        NetworkManager.singleton.StartHost();
    }

    /// <summary>
    /// Join the lobby on our local network.
    /// </summary>

    private void Join()
    {
        HandleClientConnecting();

        NetworkManager.singleton.StartClient();
    }

    /// <summary>
    /// What to do when connecting to the server.
    /// </summary>

    private void HandleClientConnecting()
    {
        // ---> Toggle on connecting text here.
        //connectingTxt.SetActive(true);

        SpawnNetworkManager();
    }

    /// <summary>
    /// What to do if we couldn't connect to the server.
    /// </summary>

    private void HandleClientDisconnected()
    {
        Destroy(networkManager);

        // ---> Toggle off connecting text here.
        //connectingTxt.SetActive(false);

        // ---> Re-Enable menu here.
    }

    /// <summary>
    /// Spawns the appropriate Network Manager.
    /// </summary>

    private void SpawnNetworkManager()
    {
        if (SteamSettings.Initialized)
        {
            networkManager = Instantiate(_steamNetworkManagerPrefab);
        }
        else
        {
            networkManager = Instantiate(_networkManagerPrefab);
        }
    }

    #endregion
}
