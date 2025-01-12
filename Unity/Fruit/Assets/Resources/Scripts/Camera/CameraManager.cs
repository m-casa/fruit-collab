using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    #region FIELDS

    [SerializeField] private CinemachineCamera _startCamera;
    [SerializeField] private CinemachineCamera _mainMenuCamera;
    [SerializeField] private CinemachineCamera _characterSelectCamera;
    [SerializeField] private CinemachineCamera _lobbyCamera;

    private CinemachineCamera _currentCamera;

    #endregion

    #region METHODS

    private void Start()
    {
        if (_startCamera != null)
            _currentCamera = _startCamera;
    }

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

    #endregion
}
