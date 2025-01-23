using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSelect : NetworkBehaviour
{
    #region FIELDS

    [Header("Characters")]
    [SerializeField] private Transform[] _characterPositions; // Array of character positions
    [SerializeField] private string[] _characterNames = new string[] { "Apple", "Grape", "Lemon", "Peach" }; // Names of the characters
    private int _currentIndex = 0; // Currently selected character index

    [Header("Arrow Setup")]
    [SerializeField] private float _arrowMoveSpeed = 5f; // Speed of smooth movement
    [SerializeField] private float _arrowRotationSpeed = 100f; // Degrees per second
    [SerializeField] private float _arrowHoverSpeed = 3.5f; // Speed of up-and-down hover animation
    [SerializeField] private float _arrowHoverAmount = 0.1f; // Distance of the hover animation
    [SerializeField] private Vector3 _arrowMinScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 _arrowMaxScale = new Vector3(1.5f, 1.5f, 1.5f);

    [Header("Navigation Settings")]
    [SerializeField] private InputActionReference _navigate; // Input for left/right navigation
    [SerializeField] private InputActionReference _submit;

    private Vector3 _arrowBasePosition;
    private Vector3 _targetPosition; // New target position for the arrow

    #endregion

    #region MONOBEHAVIOR

    /// <summary>
    /// Subscribes to the navigate/submit actions.
    /// </summary>

    void OnEnable()
    {
        // Subscribe to the events
        if (_navigate != null)
            _navigate.action.performed += OnNavigate;

        if (_submit != null)
            _submit.action.performed += OnSubmit;

        // Enable the actions
        _navigate?.action.Enable();
        _submit?.action.Enable();
    }

    /// <summary>
    /// Unsubscribes from the navigate/submit actions.
    /// </summary>

    void OnDisable()
    {
        // Unsubscribe from the events
        if (_navigate != null)
            _navigate.action.performed -= OnNavigate;

        if (_submit != null)
            _submit.action.performed -= OnSubmit;

        // Disable the actions
        _navigate?.action.Disable();
        _submit?.action.Disable();
    }

    /// <summary>
    /// Sets up the selection arrow's initial position.
    /// </summary>

    void Start()
    {
        if (_characterPositions.Length == 0)
        {
            Debug.LogError("Characters or ArrowTransform are not assigned.");
            return;
        }

        // Position the arrow above the first character
        _arrowBasePosition = _characterPositions[_currentIndex].position + Vector3.up;
        transform.position = _arrowBasePosition;
        _targetPosition = transform.position;
    }

    /// <summary>
    /// Calls the selection arrow's animation method every frame.
    /// </summary>

    void Update()
    {
        AnimateArrow();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Determines which direction the user is trying to navigate towards.
    /// </summary>

    private void OnNavigate(InputAction.CallbackContext context)
    {
        Vector2 direction = context.ReadValue<Vector2>();

        if (direction.x > 0) // Move right
        {
            NavigateCharacters(1);
        }
        else if (direction.x < 0) // Move left
        {
            NavigateCharacters(-1);
        }
    }

    /// <summary>
    /// Navigates the character choices in the direction of the user's input.
    /// </summary>

    private void NavigateCharacters(int direction)
    {
        // Update index and wrap around
        _currentIndex += direction;

        if (_currentIndex >= _characterPositions.Length)
            _currentIndex = 0;
        else if (_currentIndex < 0)
            _currentIndex = _characterPositions.Length - 1;

        // Move the arrow to the new character
        _targetPosition = _characterPositions[_currentIndex].position + Vector3.up;
    }

    /// <summary>
    /// Sends a request to the server to try and claim the selected character.
    /// </summary>

    private void OnSubmit(InputAction.CallbackContext context)
    {
        string selectedCharacter = _characterNames[_currentIndex];

        NetworkPlayer.LocalInstance.CmdRequestCharacter(selectedCharacter);
    }

    /// <summary>
    /// Logic for animating the selection arrrow.
    /// </summary>

    private void AnimateArrow()
    {
        // Smoothly move the base position toward the target position
        _arrowBasePosition = Vector3.Lerp(_arrowBasePosition, _targetPosition, Time.deltaTime * _arrowMoveSpeed);

        // Hover animation (up and down motion)
        float hoverOffset = Mathf.Sin(Time.time * _arrowHoverSpeed) * _arrowHoverAmount;
        transform.position = _arrowBasePosition + new Vector3(0, hoverOffset, 0);

        // Normalize hover offset to range [0, 1] (0 = lowest, 1 = highest)
        float normalizedHover = (hoverOffset + _arrowHoverAmount) / (2 * _arrowHoverAmount);

        // Calculate scale for growing/shrinking animation
        Vector3 targetScale = Vector3.Lerp(_arrowMaxScale, _arrowMinScale, normalizedHover);
        transform.localScale = targetScale;

        // Rotation animation (continuous spin)
        transform.rotation *= Quaternion.Euler(_arrowRotationSpeed * Time.deltaTime, 0, 0);
    }

    #endregion
}
