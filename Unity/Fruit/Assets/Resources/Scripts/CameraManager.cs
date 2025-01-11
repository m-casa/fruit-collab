using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public CinemachineCamera startCamera;        // Camera for the starting position
    public CinemachineCamera mainMenuCamera;     // Camera for the main menu
    public CinemachineCamera characterSelectCamera; // Camera for character selection

    public GameObject startUI;                          // UI for "Press Any Button to Start"
    public GameObject mainMenuUI;                       // UI for the main menu
    public GameObject characterSelectUI;                // UI for character selection

    public void OnStartButtonPressed()
    {
        // Transition to the main menu camera
        startCamera.Priority = 0;
        mainMenuCamera.Priority = 10;

        // Update UI
        startUI.SetActive(false);
        mainMenuUI.SetActive(true);
    }

    public void OnHostGameSelected()
    {
        // Transition to the character selection camera
        mainMenuCamera.Priority = 0;
        characterSelectCamera.Priority = 10;

        // Update UI
        mainMenuUI.SetActive(false);
        characterSelectUI.SetActive(true);
    }
}
