using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    #region FIELDS

    public static CameraManager Instance { get; private set; }

    [SerializeField] private CinemachineCamera _startCamera;
    [SerializeField] private CinemachineCamera _mainMenuCamera;
    [SerializeField] private CinemachineCamera _characterSelectCamera;
    [SerializeField] private CinemachineCamera _lobbyCamera;
    [SerializeField] private CinemachineTargetGroup _playerTargetGroup;

    #endregion

    #region MONOBEHAVIOR

    /// <summary>
    /// The initialization of this instance.
    /// </summary>

    void Awake()
    {
        // Check if we have an instance of the camera manager
        if (Instance != null)
        {
            // If we already have a camera manager, destroy this one
            Destroy(gameObject);
        }

        // Set this camera manager as the primary instance since we don't have one
        Instance = this;

        // When our new scene loads, don't delete the camera manager
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Moves the camera from the starting position to the main menu's position.
    /// </summary>

    public void MoveCameraToMainMenu()
    {
        // Transition to the main menu camera
        _startCamera.Priority = 0;
        _mainMenuCamera.Priority = 10;
    }

    /// <summary>
    /// Moves the camera from the main menu's position to the character select position.
    /// </summary>

    public void MoveCameraToCharacterSelect()
    {
        // Transition to the character selection camera
        _mainMenuCamera.Priority = 0;
        _characterSelectCamera.Priority = 10;
    }

    /// <summary>
    /// Moves the camera from the character select position to the lobby's position.
    /// </summary>

    public void MoveCameraToLobby()
    {
        // Transition to the lobby camera
        _characterSelectCamera.Priority = 0;
        _lobbyCamera.Priority = 10;
    }

    /// <summary>
    /// Adds a player to the target group, 
    ///  which the dynamic camera uses to keep all players on screen.
    /// </summary>

    public void AddPlayerToCamera(GameObject player)
    {
        _playerTargetGroup.AddMember(player.transform, 1f, 2f); // Weight = 1, Radius = 2
    }

    /// <summary>
    /// Removes a player from the target group, 
    ///  so that the dynamic camera stops tracking them.
    /// </summary>

    public void RemovePlayerFromCamera(GameObject player)
    {
        _playerTargetGroup.RemoveMember(player.transform);
    }

    #endregion
}
