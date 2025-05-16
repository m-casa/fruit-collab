using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    #region FIELDS

    public static CameraManager Instance { get; private set; }

    [SerializeField] private CinemachineCamera _startCamera;
    [SerializeField] private CinemachineCamera _mainMenuCamera;
    [SerializeField] private CinemachineCamera _characterSelectCamera;
    [SerializeField] private CinemachineCamera _lobbyCamera;
    [SerializeField] private CinemachineTargetGroup _characterTargetGroup;

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
    /// Show the start screen initially.
    /// </summary>

    public void EnableCamera()
    {
        GetComponentInChildren<Camera>().enabled = true;
    }

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
    /// Adds a character to the target group, 
    ///  which the dynamic camera uses to keep all players on screen.
    /// </summary>

    public void AddCharacterToCamera(GameObject character)
    {
        if (!IsCharacterInCameraGroup(character))
        {
            _characterTargetGroup.AddMember(character.transform, 1f, 2f); // Weight = 1, Radius = 2
        }
        else
        {
            Debug.Log($"[CameraManager] {character.name} is already in the target group.");
        }
    }

    /// <summary>
    /// Removes a character from the target group, 
    ///  so that the dynamic camera stops tracking them.
    /// </summary>

    public void RemoveCharacterFromCamera(GameObject character)
    {
        _characterTargetGroup.RemoveMember(character.transform);
    }

    /// <summary>
    /// Checks if the passed character is already being tracked by the camera.
    /// </summary>

    private bool IsCharacterInCameraGroup(GameObject character)
    {
        foreach (var member in _characterTargetGroup.Targets)
        {
            if (member.Object != null)
            {
                if (member.Object.transform == character.transform)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    #endregion
}
