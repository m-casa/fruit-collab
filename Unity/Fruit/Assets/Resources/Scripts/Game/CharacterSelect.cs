using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSelect : MonoBehaviour
{
    [Header("Characters")]
    public Transform[] characters; // Array of character positions
    private int currentIndex = 0; // Currently selected character index

    [Header("Camera")]
    public CameraManager cameraManager; // Reference to your CameraManager

    [Header("Arrow")]
    public Transform arrowTransform; // Transform of the 3D arrow
    public float arrowHoverSpeed = 1f; // Speed of up-and-down hover animation
    public float arrowHoverAmount = 0.2f; // Distance of the hover animation
    public float arrowScaleSpeed = 2f; // Speed of the arrow scaling animation
    public Vector3 arrowMinScale = new Vector3(1f, 1f, 1f);
    public Vector3 arrowMaxScale = new Vector3(1.2f, 1.2f, 1.2f);

    [Header("Input")]
    public InputAction movementInputAction; // Input for left/right navigation

    private Vector3 arrowBasePosition;
    private bool isArrowGrowing = true;

    private void OnEnable()
    {
        movementInputAction.Enable();
        movementInputAction.performed += OnMove;
    }

    private void OnDisable()
    {
        movementInputAction.performed -= OnMove;
        movementInputAction.Disable();
    }

    private void Start()
    {
        if (characters.Length == 0 || arrowTransform == null)
        {
            Debug.LogError("Characters or ArrowTransform are not assigned.");
            return;
        }

        // Position the arrow above the first character
        arrowBasePosition = characters[currentIndex].position + Vector3.up;
        arrowTransform.position = arrowBasePosition;
    }

    private void Update()
    {
        AnimateArrow();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();

        if (input.x > 0) // Move right
        {
            ChangeCharacter(1);
        }
        else if (input.x < 0) // Move left
        {
            ChangeCharacter(-1);
        }
    }

    private void ChangeCharacter(int direction)
    {
        // Update index and wrap around
        currentIndex += direction;

        if (currentIndex >= characters.Length)
            currentIndex = 0;
        else if (currentIndex < 0)
            currentIndex = characters.Length - 1;

        // Pan the camera to the new character
        if (cameraManager != null)
        {
            //cameraManager.PanToPosition(characters[currentIndex].position);
        }

        // Move the arrow to the new character
        arrowBasePosition = characters[currentIndex].position + Vector3.up;
    }

    private void AnimateArrow()
    {
        // Hover animation
        float hoverOffset = Mathf.Sin(Time.time * arrowHoverSpeed) * arrowHoverAmount;
        arrowTransform.position = arrowBasePosition + new Vector3(0, hoverOffset, 0);

        // Scaling animation
        Vector3 targetScale = isArrowGrowing ? arrowMaxScale : arrowMinScale;
        arrowTransform.localScale = Vector3.Lerp(arrowTransform.localScale, targetScale, Time.deltaTime * arrowScaleSpeed);

        if (Vector3.Distance(arrowTransform.localScale, targetScale) < 0.05f)
        {
            isArrowGrowing = !isArrowGrowing; // Switch scaling direction
        }
    }
}
