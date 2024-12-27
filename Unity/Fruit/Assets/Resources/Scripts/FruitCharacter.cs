using Animancer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EasyCharacterMovement
{
    /// <summary>
    /// FruitCharacter.
    ///
    /// This extends the Character class to add controls for gameplay inspired by Power Stone 2.
    /// </summary>

    public class FruitCharacter : Character
    {
        #region FIELDS

        private ThirdPersonCameraController _cameraController;
        private bool _wasFalling, _rightFootUp;
        private Quaternion chestOverrideTransform; // The dummy transform used to update the real one
        private Quaternion chestTargetRotation; // Target rotation for the lean

        [SerializeField] private NamedAnimancerComponent _animancer;
        [SerializeField] private Transform chestTransform; // Reference to the chest bone
        [SerializeField] private float leanAmount = 12.5f; // Maximum degrees to lean
        [SerializeField] private float leanSpeed = 8f; // Speed at which the lean is applied


        #endregion

        #region PROPERTIES


        /// <summary>
        /// Cached camera controller.
        /// </summary>

        protected ThirdPersonCameraController cameraController
        {
            get
            {
                if (_cameraController == null)
                    _cameraController = camera.GetComponent<ThirdPersonCameraController>();

                return _cameraController;
            }
        }

        #endregion

        #region INPUT ACTIONS

        protected InputAction mouseLookInputAction { get; set; }

        protected InputAction mouseScrollInputAction { get; set; }

        protected InputAction controllerLookInputAction { get; set; }

        protected InputAction cursorLockInputAction { get; set; }

        protected InputAction cursorUnlockInputAction { get; set; }

        #endregion

        #region INPUT ACTION HANDLERS

        /// <summary>
        /// Gets the mouse look value.
        /// Return its current value or zero if no valid InputAction found.
        /// </summary>

        protected virtual Vector2 GetMouseLookInput()
        {
            if (mouseLookInputAction != null)
                return mouseLookInputAction.ReadValue<Vector2>();

            return Vector2.zero;
        }


        /// <summary>
        /// Gets the mouse scroll input value.
        /// Return its current value or zero if no valid InputAction found.
        /// </summary>

        protected virtual Vector2 GetMouseScrollInput()
        {
            if (mouseScrollInputAction != null)
                return mouseScrollInputAction.ReadValue<Vector2>();

            return Vector2.zero;
        }

        /// <summary>
        /// Gets the controller look input value.
        /// Return its current value or zero if no valid InputAction found.
        /// </summary>

        protected virtual Vector2 GetControllerLookInput()
        {
            if (controllerLookInputAction != null)
                return controllerLookInputAction.ReadValue<Vector2>();

            return Vector2.zero;
        }

        /// <summary>
        /// Handle cursor lock InputAction.
        /// </summary>

        protected virtual void OnCursorLock(InputAction.CallbackContext context)
        {
            // Do not allow to lock cursor if using UI

            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
                return;

            if (context.started)
                cameraController.LockCursor();
        }

        /// <summary>
        /// Handle cursor unlock InputAction.
        /// </summary>

        protected virtual void OnCursorUnlock(InputAction.CallbackContext context)
        {
            if (context.started)
                cameraController.UnlockCursor();
        }

        #endregion

        #region METHODS  

        /// <summary>
        /// Initialize player InputActions (if any).
        /// E.g. Subscribe to input action events and enable input actions here.
        /// </summary>

        protected override void InitPlayerInput()
        {
            // Call base method implementation

            base.InitPlayerInput();

            // Attempts to cache and init this InputActions (if any)

            if (inputActions == null)
                return;

            mouseLookInputAction = inputActions.FindAction("Mouse Look");
            mouseLookInputAction?.Enable();

            mouseScrollInputAction = inputActions.FindAction("Mouse Scroll");
            mouseScrollInputAction?.Enable();

            controllerLookInputAction = inputActions.FindAction("Controller Look");
            controllerLookInputAction?.Enable();

            cursorLockInputAction = inputActions.FindAction("Cursor Lock");
            if (cursorLockInputAction != null)
            {
                cursorLockInputAction.started += OnCursorLock;
                cursorLockInputAction.Enable();
            }

            cursorUnlockInputAction = inputActions.FindAction("Cursor Unlock");
            if (cursorUnlockInputAction != null)
            {
                cursorUnlockInputAction.started += OnCursorUnlock;
                cursorUnlockInputAction.Enable();
            }
        }

        /// <summary>
        /// Unsubscribe from input action events and disable input actions.
        /// </summary>

        protected override void DeinitPlayerInput()
        {
            // Call base method implementation

            base.DeinitPlayerInput();

            if (mouseLookInputAction != null)
            {
                mouseLookInputAction.Disable();
                mouseLookInputAction = null;
            }

            if (mouseScrollInputAction != null)
            {
                mouseScrollInputAction.Disable();
                mouseScrollInputAction = null;
            }

            if (controllerLookInputAction != null)
            {
                controllerLookInputAction.Disable();
                controllerLookInputAction = null;
            }

            if (cursorLockInputAction != null)
            {
                cursorLockInputAction.started -= OnCursorLock;

                cursorLockInputAction.Disable();
                cursorLockInputAction = null;
            }

            if (cursorUnlockInputAction != null)
            {
                cursorUnlockInputAction.started -= OnCursorUnlock;

                cursorUnlockInputAction.Disable();
                cursorUnlockInputAction = null;
            }
        }

        /// <summary>
        /// Subscribe to events related to being off the ground.
        /// </summary>

        protected override void OnOnEnable()
        {
            base.OnOnEnable();

            Jumped += PlayJumpAnimation;
            ReachedJumpApex += PlayFallAnimation;
            Landed += DoneFalling;
        }

        /// <summary>
        /// Unsubscribe from events related to being off the ground.
        /// </summary>

        protected override void OnOnDisable()
        {
            base.OnOnDisable();

            Jumped -= PlayJumpAnimation;
            ReachedJumpApex -= PlayFallAnimation;
            Landed -= DoneFalling;
        }

        /// <summary>
        /// Check what state the character is in and play the appropriate animation.
        /// </summary>

        protected override void Animate()
        {
            Vector2 movementInput = GetMovementInput();

            if (IsGrounded())
            {
                if (WasFalling())
                {
                    _wasFalling = false;
                    var state = _animancer.TryPlay("_Land", 0.25f);
                    state.Events.OnEnd = FinishedLanding;
                }

                else if (movementInput == Vector2.zero)
                {
                    if (!_animancer.IsPlaying("_Land"))
                    {
                        _animancer.TryPlay("_Idle", 0.15f);
                    }
                }

                else if (movementInput.y > 0f || movementInput.y < 0f)
                {
                    _animancer.TryPlay("_Run", 0.25f);
                }

                else if (movementInput.x > 0f || movementInput.x < 0f)
                {
                    _animancer.TryPlay("_Run", 0.25f);
                }
            }
            else if (!_animancer.IsPlaying("_JiggleJump.R") && !_animancer.IsPlaying("_JiggleJump.L"))
            {
                if (_rightFootUp)
                {
                    _animancer.TryPlay("_Fall.R", 0.25f);
                }
                else
                {
                    _animancer.TryPlay("_Fall.L", 0.25f);
                }
            }
        }

        /// <summary>
        /// Updates the Character's rotation based on its current RotationMode PLUS its current up direction.
        /// </summary>

        protected override void UpdateRotation()
        {
            // Call base method (eg: rotate towards movement direction)

            base.UpdateRotation();

            // Update's gravity direction and orient Character's Up to -gravity direction

            RaycastHit hit;

            if (Physics.Raycast(transform.position, Vector3.down, out hit, 0.25f))
            {
                // Calculate the slope normal

                Vector3 slopeNormal = hit.normal;

                // Calculate the angle between the character's up vector and the slope normal

                float angle = Vector3.Angle(transform.up, slopeNormal);

                if (angle <= 45)
                {
                    // Rotate the player to align with the slope

                    Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, slopeNormal) * transform.rotation;

                    // Smoothly interpolate between current rotation and target rotation

                    transform.rotation = Quaternion.Slerp(transform.rotation, slopeRotation, 10f * Time.fixedDeltaTime);
                }
            }
            else
            {
                // If not grounded, smoothly return to upright position

                Quaternion uprightRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, uprightRotation, 10f * Time.fixedDeltaTime);
            }
        }

        /// <summary>
        /// Our Update method.
        /// </summary>

        protected override void OnUpdate()
        {
            base.OnUpdate();

            HandleCameraInput();

            HandleLeanInput();
        }

        /// <summary>
        /// Our LateUpdate method.
        /// </summary>

        protected override void OnLateUpdate()
        {
            base.OnLateUpdate();

            ApplyLean();
        }

        /// <summary>
        /// Perform camera related input actions, eg: Look Up / Down, Turn, etc.
        /// </summary>

        protected virtual void HandleCameraInput()
        {
            if (!cameraController.IsCursorLocked())
                return;

            Vector2 mouseLookInput = GetMouseLookInput();
            if (mouseLookInput.sqrMagnitude > 0)
            {
                // Mouse look input

                if (mouseLookInput.x != 0.0f)
                    cameraController.Turn(mouseLookInput.x);

                if (mouseLookInput.y != 0.0f)
                    cameraController.LookUp(mouseLookInput.y);

            }
            else
            {
                // Controller look input

                Vector2 controllerLookInput = GetControllerLookInput();

                if (controllerLookInput.x != 0.0f)
                    cameraController.TurnAtRate(controllerLookInput.x);

                if (controllerLookInput.y != 0.0f)
                    cameraController.LookUpAtRate(controllerLookInput.y);
            }

            // Mouse scroll input

            Vector2 mouseScrollInput = GetMouseScrollInput();

            if (mouseScrollInput.y != 0.0f)
                cameraController.ZoomAtRate(mouseScrollInput.y);
        }

        /// <summary>
        /// Saves the rotation data needed to lean the character in the direction they move.
        /// </summary>

        protected virtual void HandleLeanInput()
        {
            // Project on a horizontal plane so we only have to consider horizontal direction without vertical rotation creating issues

            Vector3 characterForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            Vector3 movementDirection = Vector3.ProjectOnPlane(GetMovementDirection(), Vector3.up);

            // Calculate the signed angle between character's forward direction and target movement direction

            float angleDifference = Vector3.SignedAngle(characterForward, movementDirection, transform.up);

            if (angleDifference < -15)
            {
                // Calculate target rotation for leaning left

                chestTargetRotation = Quaternion.Euler(0, 0, leanAmount);
            }
            else if (angleDifference > 15)
            {
                // Calculate target rotation for leaning right

                chestTargetRotation = Quaternion.Euler(0, 0, -leanAmount);
            }
            else
            {
                // Return to upright position

                chestTargetRotation = Quaternion.Euler(0, 0, 0);
            }

            // Smoothly interpolate to the target rotation on the chest

            chestOverrideTransform = Quaternion.Slerp(chestTransform.localRotation, chestTargetRotation, Time.deltaTime * leanSpeed);
        }

        /// <summary>
        /// Applies the correct rotation determined in HandleLeanInput.
        /// </summary>

        protected virtual void ApplyLean()
        {
            // Override animation data on the chest with our dummy transform

            chestTransform.localRotation = chestOverrideTransform;
        }

        /// <summary>
        /// Sets the right foot as up so we can use that foot to jump.
        /// </summary>

        protected virtual void SetRightFootUp()
        {
            _rightFootUp = true;
        }

        /// <summary>
        /// Sets the right foot as down so we can use the left foot to jump.
        /// </summary>

        protected virtual void SetRightFootDown()
        {
            _rightFootUp = false;
        }

        /// <summary>
        /// Play the jump animation for the character and check for their apex.
        /// </summary>

        protected virtual void PlayJumpAnimation()
        {
            justJumped = true;
            notifyJumpApex = true;

            _animancer.Stop("_Run");
            
            if (_rightFootUp)
            {
                _animancer.TryPlay("_JiggleJump.L", 0.25f);
                SetRightFootDown();
            }
            else
            {
                _animancer.TryPlay("_JiggleJump.R", 0.25f);
                SetRightFootUp();
            }
        }

        /// <summary>
        /// Play the fall animation for the character.
        /// </summary>

        protected virtual void PlayFallAnimation()
        {
            if (_rightFootUp)
            {
                _animancer.TryPlay("_Fall.R", 0.25f);
            }
            else
            {
                _animancer.TryPlay("_Fall.L", 0.25f);
            }
        }

        /// <summary>
        /// State that the player was falling.
        /// </summary>

        protected virtual void DoneFalling()
        {
            _wasFalling = true;
        }

        /// <summary>
        /// Return whether or not the player was falling.
        /// </summary>

        protected virtual bool WasFalling()
        {
            return _wasFalling;
        }

        /// <summary>
        /// Return the character to Idle once they've finished landing.
        /// </summary>

        protected virtual void FinishedLanding()
        {
            _animancer.TryPlay("_Idle", 0.25f);
        }

        #endregion
    }
}