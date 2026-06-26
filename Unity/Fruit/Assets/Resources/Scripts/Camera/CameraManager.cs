using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    #region FIELDS

    public static CameraManager Instance { get; private set; }

    [System.Serializable]
    public class TransitionArena
    {
        public Transform fromTarget;
        public Transform toTarget;
        public ArenaArea arenaArea;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        public void CacheInitialState()
        {
            _initialPosition = arenaArea.transform.position;
            _initialRotation = arenaArea.transform.rotation;
        }

        public void ResetState()
        {
            arenaArea.transform.position = _initialPosition;
            arenaArea.transform.rotation = _initialRotation;
        }
    }

    [Header("Virtual Cameras")]
    [SerializeField] private CinemachineCamera _startCamera;
    [SerializeField] private CinemachineCamera _mainMenuCamera;
    [SerializeField] private CinemachineCamera _characterSelectCamera;
    [SerializeField] private CinemachineCamera _playerFollowCamera;
    [SerializeField] private CinemachineCamera _arenaTransitionCamera;
    [SerializeField] private CinemachineTargetGroup _playerTargetGroup;
    [SerializeField] private Transform _cameraAnchor;

    private ArenaArea[] _staticArenas;
    private TransitionArena[] _transitionArenas;

    public int TransitionCount => _transitionArenas.Length;

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

    public void InitializeArenas(ArenaArea[] staticArenas, TransitionArena[] transitionArenas)
    {
        _staticArenas = staticArenas;
        _transitionArenas = transitionArenas;
    }

    public void ResetTransitionArenas()
    {
        foreach (TransitionArena transitionArena in _transitionArenas)
        {
            transitionArena.ResetState();
        }
    }

    public ArenaArea GetStaticArena(int index)
    {
        if (index < 0 || index >= _staticArenas.Length)
            return null;

        return _staticArenas[index];
    }

    public ArenaArea GetTransitionArena(int index)
    {
        if (index < 0 || index >= _transitionArenas.Length)
            return null;

        return _transitionArenas[index].arenaArea;
    }

    /// <summary>
    /// Show the start screen initially. Also grabs the transition targets in the map.
    /// </summary>

    public void EnableCamera()
    {
        GetComponentInChildren<Camera>().enabled = true;
    }

    /// <summary>
    /// Moves the camera from the starting position/lobby to the main menu's position.
    /// </summary>

    public void MoveCameraToMainMenu()
    {
        // Transition to the main menu camera
        _startCamera.Priority = 0;
        _playerFollowCamera.Priority = 0;
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
    /// Moves the camera from the character select position to the player follow position.
    /// </summary>

    public void MoveCameraToPlayerFollow()
    {
        // Transition to the player follow camera
        _characterSelectCamera.Priority = 0;
        _arenaTransitionCamera.Priority = 0;
        _playerFollowCamera.Priority = 10;
    }

    /// <summary>
    /// Moves the camera from the current arena to the next.
    /// </summary>

    public void MoveCameraToNextArena(int transitionIndex, float duration)
    {
        if (transitionIndex < 0 || transitionIndex >= _transitionArenas.Length)
        {
            Debug.LogWarning($"[CameraManager] Invalid transition index {transitionIndex}");
            return;
        }

        StopAllCoroutines();

        MoveCameraToTransitionPosition();

        TransitionArena transitionArena = _transitionArenas[transitionIndex];

        StartCoroutine(
            CameraTransitionRoutine(transitionArena.fromTarget,
            transitionArena.toTarget,
            duration,
            transitionArena.arenaArea)
        );
    }

    /// <summary>
    /// The routine the camera follows to transition.
    /// </summary>

    private IEnumerator CameraTransitionRoutine(Transform fromTarget, Transform toTarget, float duration, ArenaArea arenaArea)
    {
        Vector3 startPos = fromTarget.position;
        Vector3 endPos = toTarget.position;
        Vector3 arenaOffset = arenaArea.transform.position - startPos;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            arenaArea.transform.position = pos + arenaOffset;
            _cameraAnchor.position = pos;

            yield return null;
        }

        arenaArea.transform.position = endPos + arenaOffset;
        _cameraAnchor.position = endPos;

        MoveCameraToPlayerFollow();
    }

    /// <summary>
    /// Moves the camera from the player follow position to the transition position.
    /// </summary>

    public void MoveCameraToTransitionPosition()
    {
        _playerFollowCamera.Priority = 0;
        _arenaTransitionCamera.Priority = 10;
    }

    /// <summary>
    /// Adds a character to the target group, 
    ///  which the dynamic camera uses to keep all players on screen.
    /// </summary>

    public void AddCharacterToCamera(GameObject character)
    {
        if (!IsCharacterInCameraGroup(character))
        {
            _playerTargetGroup.AddMember(character.transform, 1f, 2f); // Weight = 1, Radius = 2
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
        _playerTargetGroup.RemoveMember(character.transform);
    }

    /// <summary>
    /// Removes all characters from the target group.
    /// </summary>

    public void RemoveAllCharactersFromCamera()
    {
        if (_playerTargetGroup != null && _playerTargetGroup.Targets != null)
        {
            _playerTargetGroup.Targets.Clear();
        }
    }

    /// <summary>
    /// Checks if the passed character is already being tracked by the camera.
    /// </summary>

    private bool IsCharacterInCameraGroup(GameObject character)
    {
        foreach (var member in _playerTargetGroup.Targets)
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
