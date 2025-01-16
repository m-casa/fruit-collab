using HeathenEngineering.SteamworksIntegration;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    #region FIELDS

    public static UIManager Instance { get; private set; }

    private GameObject networkManager;

    [Header("Network Managers")]
    [SerializeField] private GameObject _networkManagerPrefab;
    [SerializeField] private GameObject _steamNetworkManagerPrefab;

    [Header("Menu References")]
    [SerializeField] private GameObject _startScreen; // UI for "Press Any Button to Start"
    [SerializeField] private GameObject _mainMenu; // Main menu UI
    [SerializeField] private GameObject _pauseMenu; // Pause menu UI
    [SerializeField] private GameObject _gameplayUI; // Gameplay UI

    [Header("UI References")]
    [SerializeField] private Text _timerText; // Reference to the timer text
    [SerializeField] private Text[] _playerScoreTexts; // Array of score displays for each player
    [SerializeField] private Slider[] _playerHealthBars; // Array of health bars for each player
    [SerializeField] private GameObject _joinButton;
    [SerializeField] private GameObject _connectingTxt;

    #endregion

    #region MONOBEHAVIOR

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
    }

    /// <summary>
    /// Steam API will initialize after Awake, so check for the API on Start.
    /// </summary>

    void Start()
    {
        // Show the start screen initially
        SetActivePanel(_startScreen);

        // Make sure the Steam API is available (Steam is running)
        if (!SteamSettings.Initialized) { return; }
    }

    /// <summary>
    /// Enable any events we should listen to.
    /// </summary>

    void OnEnable()
    {
        SurviveNetworkManager.ClientDisconnected += HandleClientDisconnected;

        SteamLogic.LobbyFailed += HandleClientDisconnected;
        SteamLogic.LobbyJoined += HandleClientConnecting;
    }

    /// <summary>
    /// Disable any events we are still listening to.
    /// </summary>

    void OnDisable()
    {
        SurviveNetworkManager.ClientDisconnected -= HandleClientDisconnected;

        SteamLogic.LobbyFailed -= HandleClientDisconnected;
        SteamLogic.LobbyJoined -= HandleClientConnecting;
    }

    void Update()
    {
        // Detect any button press for the start screen
        if (_startScreen.activeSelf && IsAnyInputPressed())
        {
            OnStartGame();
        }

        // Detect pause input (e.g., ESC or Start button on a controller)
        if (GameManager.Instance.InGame())
        {
            // Toggle pause menu
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Calls the Host coroutine.
    /// </summary>

    public void HostGame()
    {
        //StartCoroutine(Host());
        OnHostGame();
    }

    /// <summary>
    /// Calls the Join coroutine.
    /// </summary>

    public void JoinGame()
    {
        //StartCoroutine(Join());
        Join();
    }

    public void OnHostGame()
    {
        Host();

        SetActivePanel(null);

        CameraManager.Instance.TransitionToCharacterSelect();

        GameManager.Instance.StartSelection();
        // Logic for hosting a game...
    }

    public void TogglePauseMenu()
    {
        if (!_pauseMenu.activeSelf)
        {
            SetActivePanel(_pauseMenu);
            // Disable player controls if needed
        }
        else
        {
            SetActivePanel(null);
            // Re-enable player controls
        }
    }

    public void OnResumeGame()
    {
        // Unpause the game explicitly (used by a Resume button)
        SetActivePanel(null);

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

    public void UpdateTimer(float timeRemaining)
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        _timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void UpdatePlayerScore(int playerIndex, int score)
    {
        if (playerIndex < _playerScoreTexts.Length)
        {
            _playerScoreTexts[playerIndex].text = $"P{playerIndex + 1}: {score}";
        }
    }

    public void UpdatePlayerHealth(int playerIndex, float health)
    {
        if (playerIndex < _playerHealthBars.Length)
        {
            _playerHealthBars[playerIndex].value = health;
        }
    }

    private void SetActivePanel(GameObject activePanel)
    {
        // Disable all panels
        _startScreen.SetActive(false);
        _mainMenu.SetActive(false);
        _pauseMenu.SetActive(false);
        _gameplayUI.SetActive(false);

        if (activePanel != null)
            activePanel.SetActive(true); // Enable the selected panel
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
        SetActivePanel(_mainMenu);

        CameraManager.Instance.TransitionToMainMenu();
        // Additional logic for starting the game (e.g., enabling player controls).
    }

    private void ShowGameplayUI()
    {
        SetActivePanel(_gameplayUI);
    }

    /// <summary>
    /// Host a lobby on our local network.
    /// Use Steam if the API is initialized
    /// </summary>

    private void Host()
    {
        //yield return StartCoroutine(transition.SceneChange());

        HandleClientConnecting();

        if (SteamSettings.Initialized)
        {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, NetworkManager.singleton.maxConnections);

            //yield break;
            return;
        }

        NetworkManager.singleton.StartHost();
    }

    /// <summary>
    /// Join the lobby on our local network.
    /// </summary>

    private void Join()
    {
        //yield return StartCoroutine(transition.SceneChange());

        HandleClientConnecting();

        NetworkManager.singleton.StartClient();
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

    /// <summary>
    /// What to do when connecting to the server.
    /// </summary>

    private void HandleClientConnecting()
    {
        //connectingTxt.SetActive(true);

        //StartCoroutine(transition.ScreenFade(1.0f, 0.0f, 1.5f));

        SpawnNetworkManager();
    }

    /// <summary>
    /// What to do if we couldn't connect to the server.
    /// 0 = Main Screen, 1 = Play Screen
    /// </summary>

    private void HandleClientDisconnected()
    {
        Destroy(networkManager);

        //connectingTxt.SetActive(false);

        //Re-Enable menu here:
        //menuScreens[0].SetActive(true);

        //foreach (Button button in menuScreens[1].GetComponentsInChildren<Button>())
        //{
        //    button.interactable = true;
        //}
    }

    #endregion
}
