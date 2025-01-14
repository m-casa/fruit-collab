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

    public void TransitionToMainMenu()
    {
        // Transition to the main menu camera
        _startCamera.Priority = 0;
        _mainMenuCamera.Priority = 10;
    }

    public void TransitionToCharacterSelect()
    {
        // Transition to the character selection camera
        _mainMenuCamera.Priority = 0;
        _characterSelectCamera.Priority = 10;
    }

    public void TransitionToLobby()
    {
        // Transition to the lobby camera
        _characterSelectCamera.Priority = 0;
        _lobbyCamera.Priority = 10;
    }

    public void AddPlayerToCamera(GameObject player)
    {
        _playerTargetGroup.AddMember(player.transform, 1f, 2f); // Weight = 1, Radius = 2
    }

    public void RemovePlayerFromCamera(GameObject player)
    {
        _playerTargetGroup.RemoveMember(player.transform);
    }

    #endregion
}
