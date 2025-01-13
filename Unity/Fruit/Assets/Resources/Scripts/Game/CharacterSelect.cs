using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSelect : MonoBehaviour
{
    #region FIELDS

    [Header("Characters")]
    [SerializeField] private Transform[] _characterPositions; // Array of character positions
    [SerializeField] private string[] _characterNames = new string[] { "Apple", "Grape", "Lemon", "Peach" }; // Names of the characters
    private int _currentIndex = 0; // Currently selected character index

    [Header("Arrow")]
    [SerializeField] private float _arrowHoverSpeed = 1f; // Speed of up-and-down hover animation
    [SerializeField] private float _arrowHoverAmount = 0.2f; // Distance of the hover animation
    [SerializeField] private float _arrowScaleSpeed = 2f; // Speed of the arrow scaling animation
    [SerializeField] private Vector3 _arrowMinScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 _arrowMaxScale = new Vector3(1.2f, 1.2f, 1.2f);

    [Header("Navigation Settings")]
    [SerializeField] private InputActionReference _navigate; // Input for left/right navigation
    [SerializeField] private InputActionReference _submit;

    private Vector3 _arrowBasePosition;
    private bool _isArrowGrowing = true;

    #endregion

    #region METHODS

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
    }

    void Update()
    {
        AnimateArrow();
    }

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

    private void NavigateCharacters(int direction)
    {
        // Update index and wrap around
        _currentIndex += direction;

        if (_currentIndex >= _characterPositions.Length)
            _currentIndex = 0;
        else if (_currentIndex < 0)
            _currentIndex = _characterPositions.Length - 1;

        // Pan the camera to the new character
        //if (CameraManager.Instance != null)
        //{
        //CameraManager.Instance.PanToPosition(characters[currentIndex].position);
        //}

        // Move the arrow to the new character
        _arrowBasePosition = _characterPositions[_currentIndex].position + Vector3.up;
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        string selectedCharacter = _characterNames[_currentIndex];

        GameManager.Instance.SelectCharacter(selectedCharacter);

        //if (GameManager.Instance.IsCharacterAvailable(selectedCharacter))
        //{
        //    GameManager.Instance.SelectCharacter(selectedCharacter);
        //    Debug.Log($"Character {selectedCharacter} selected.");
        //}
        //else
        //{
        //    Debug.LogWarning($"Character {selectedCharacter} is already taken.");
        //}
    }

    public void DeselectCurrentCharacter()
    {
        string deselectedCharacter = _characterNames[_currentIndex];

        GameManager.Instance.DeselectCharacter(deselectedCharacter);
    }

    private void AnimateArrow()
    {
        //// Hover animation
        //float hoverOffset = Mathf.Sin(Time.time * _arrowHoverSpeed) * _arrowHoverAmount;
        //transform.position = _arrowBasePosition + new Vector3(0, hoverOffset, 0);

        //// Scaling animation
        //Vector3 targetScale = _isArrowGrowing ? _arrowMaxScale : _arrowMinScale;
        //transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * _arrowScaleSpeed);

        //if (Vector3.Distance(transform.localScale, targetScale) < 0.05f)
        //{
        //    _isArrowGrowing = !_isArrowGrowing; // Switch scaling direction
        //}

        // Hover animation
        float hoverOffset = Mathf.Sin(Time.time * _arrowHoverSpeed) * _arrowHoverAmount;
        transform.localPosition = _arrowBasePosition + new Vector3(0, hoverOffset, 0);

        // Normalize hover offset (0 = lowest point, 1 = highest point)
        float normalizedHover = (hoverOffset + _arrowHoverAmount) / (2 * _arrowHoverAmount);

        // Calculate scale based on hover position (grow when descending, shrink when ascending)
        Vector3 targetScale = Vector3.Lerp(_arrowMaxScale, _arrowMinScale, normalizedHover);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * _arrowScaleSpeed);
    }

    #endregion
}
