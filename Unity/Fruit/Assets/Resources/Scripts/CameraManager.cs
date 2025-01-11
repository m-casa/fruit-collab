using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    #region FIELDS

    [SerializeField] private CinemachineCamera startCamera;
    [SerializeField] private CinemachineCamera mainMenuCamera;
    [SerializeField] private CinemachineCamera characterSelectCamera;
    [SerializeField] private CinemachineCamera lobbyCamera;

    #endregion

    #region METHODS

    public void TransitionToMainMenu()
    {
        // Transition to the main menu camera
        startCamera.Priority = 0;
        mainMenuCamera.Priority = 10;
    }

    public void TransitionToCharacterSelect()
    {
        // Transition to the character selection camera
        mainMenuCamera.Priority = 0;
        characterSelectCamera.Priority = 10;
    }

    public void TransitionToLobby()
    {
        // Transition to the lobby camera
        characterSelectCamera.Priority = 0;
        lobbyCamera.Priority = 10;
    }

    #endregion
}
