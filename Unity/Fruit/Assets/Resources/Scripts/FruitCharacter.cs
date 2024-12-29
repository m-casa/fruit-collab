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
        private bool _rightFootUp, _punchButtonPressed, _secondPunchQueued, 
            _isThrowingFirstPunch, _isThrowingSecondPunch, _firstPunchIsAnimating, _secondPunchIsAnimating;
        private Quaternion _chestOverrideTransform; // The dummy transform used to update the real one
        private Quaternion _chestTargetRotation; // Target rotation for the lean

        [SerializeField] private NamedAnimancerComponent _animancer;
        [SerializeField] private Transform _chestTransform; // Reference to the chest bone
        [SerializeField] private float _leanAmount = 12.5f; // Maximum degrees to lean
        [SerializeField] private float _leanSpeed = 8f; // Speed at which the lean is applied


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

        /// <summary>
        /// Punch InputAction.
        /// </summary>

        protected InputAction punchInputAction { get; set; }

        /// <summary>
        /// Mouse Look InputAction.
        /// </summary>

        protected InputAction mouseLookInputAction { get; set; }

        /// <summary>
        /// Mouse Scroll InputAction.
        /// </summary>

        protected InputAction mouseScrollInputAction { get; set; }

        /// <summary>
        /// Controller Look InputAction.
        /// </summary>

        protected InputAction controllerLookInputAction { get; set; }

        /// <summary>
        /// Cursor Lock InputAction.
        /// </summary>

        protected InputAction cursorLockInputAction { get; set; }

        /// <summary>
        /// Cursor Unlock InputAction.
        /// </summary>

        protected InputAction cursorUnlockInputAction { get; set; }

        #endregion

        #region INPUT ACTION HANDLERS

        /// <summary>
        /// Punch input action handler.
        /// </summary>

        protected virtual void OnPunch(InputAction.CallbackContext context)
        {
            if (context.started || context.performed)
                Punch();
            else if (context.canceled)
                StopPunching();
        }

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

        #region EVENTS

        public delegate void PunchedEventHandler();

        /// <summary>
        /// Event triggered when the player initiates the first punch.
        /// </summary>

        public event PunchedEventHandler FirstPunchThrown;

        /// <summary>
        /// Event triggered when the player queues a second punch.
        /// </summary>

        public event PunchedEventHandler SecondPunchQueued;

        /// <summary>
        /// Event triggered when the second punch is ready to be thrown.
        /// </summary>

        public event PunchedEventHandler SecondPunchThrown;

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

            // Setup Punch input action handlers

            punchInputAction = inputActions.FindAction("Punch");
            if (punchInputAction != null)
            {
                punchInputAction.started += OnPunch;
                punchInputAction.performed += OnPunch;
                punchInputAction.canceled += OnPunch;

                punchInputAction.Enable();
            }

            // Setup Mouse input action handlers

            mouseLookInputAction = inputActions.FindAction("Mouse Look");
            mouseLookInputAction?.Enable();

            mouseScrollInputAction = inputActions.FindAction("Mouse Scroll");
            mouseScrollInputAction?.Enable();

            // Setup Controller input action handlers

            controllerLookInputAction = inputActions.FindAction("Controller Look");
            controllerLookInputAction?.Enable();

            // Setup Cursor input action handlers

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

            if (punchInputAction != null)
            {
                punchInputAction.started -= OnPunch;
                punchInputAction.performed -= OnPunch;
                punchInputAction.canceled -= OnPunch;

                punchInputAction.Disable();
                punchInputAction = null;
            }

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
        /// Called when the script instance is being loaded (Awake).
        /// If overridden, must call base method in order to fully initialize the class.
        /// </summary>

        protected override void OnAwake()
        {
            base.OnAwake();

            _rightFootUp = true;
            _punchButtonPressed = false;
            _secondPunchQueued = false;
            _isThrowingFirstPunch = false;
            _isThrowingSecondPunch = false;
            _firstPunchIsAnimating = false;
            _secondPunchIsAnimating = false;
        }

        /// <summary>
        /// Subscribe to events related to being off the ground.
        /// </summary>

        protected override void OnOnEnable()
        {
            base.OnOnEnable();

            Jumped += PlayJumpAnimation;
            Landed += PlayLandAnimation;
            FirstPunchThrown += PlayFirstPunchAnimation;
            SecondPunchQueued += QueueSecondPunch;
            SecondPunchThrown += PlaySecondPunchAnimation;
        }

        /// <summary>
        /// Unsubscribe from events related to being off the ground.
        /// </summary>

        protected override void OnOnDisable()
        {
            base.OnOnDisable();

            Jumped -= PlayJumpAnimation;
            Landed -= PlayLandAnimation;
            FirstPunchThrown -= PlayFirstPunchAnimation;
            SecondPunchQueued -= QueueSecondPunch;
            SecondPunchThrown -= PlaySecondPunchAnimation;
        }

        /// <summary>
        /// Attempts to perform a requested jump.
        /// </summary>

        protected override void DoJump()
        {
            // Update held down timer

            if (_jumpButtonPressed)
                _jumpButtonHeldDownTime += deltaTime;

            // Wants to jump and not already jumping..

            if (_jumpButtonPressed && !IsJumping())
            {
                // If jumpPreGroundedTime is enabled,
                // allow to jump only if held down time is less than tolerance

                if (jumpPreGroundedTime > 0.0f)
                {
                    bool canJump = _jumpButtonHeldDownTime <= jumpPreGroundedTime;
                    if (!canJump)
                        return;
                }

                // Can perform the requested jump ?

                if (CanJump())
                {
                    punchInputAction.Disable();

                    // Jump!

                    SetMovementMode(MovementMode.Falling);

                    characterMovement.PauseGroundConstraint();
                    characterMovement.LaunchCharacter(CalcJumpImpulse(), true);

                    _jumpCount++;
                    _isJumping = true;

                    // Trigger Jumped event

                    OnJumped();
                }
            }
        }

        /// <summary>
        /// Check what state the character is in and play the appropriate animation.
        /// </summary>

        protected override void Animate()
        {
            if (!FirstPunchIsAnimating() && !SecondPunchIsAnimating())
            {
                if (IsGrounded())
                {
                    Vector2 movementInput = GetMovementInput();

                    if (movementInput == Vector2.zero)
                    {
                        PlayIdleAnimation();
                    }

                    else if (movementInput != Vector2.zero)
                    {
                        PlayRunAnimation(movementInput);
                    }
                }
                else if (!WaitingForJumpApex())
                {
                    PlayFallAnimation();
                }
            }
        }

        /// <summary>
        /// Updates the character's rotation based on its current RotationMode PLUS its current up direction.
        /// </summary>

        protected override void UpdateRotation()
        {
            // Call base method (eg: rotate towards movement direction)

            base.UpdateRotation();

            // Update's gravity direction and orient character's Up to -gravity direction

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
        /// Handle Player input, only if actions are assigned (eg: actions != null).
        /// </summary>

        protected override void HandleInput()
        {
            base.HandleInput();

            HandlePunching();
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

                _chestTargetRotation = Quaternion.Euler(0, 0, _leanAmount);
            }
            else if (angleDifference > 15)
            {
                // Calculate target rotation for leaning right

                _chestTargetRotation = Quaternion.Euler(0, 0, -_leanAmount);
            }
            else
            {
                // Return to upright position

                _chestTargetRotation = Quaternion.Euler(0, 0, 0);
            }

            // Smoothly interpolate to the target rotation on the chest

            _chestOverrideTransform = Quaternion.Slerp(_chestTransform.localRotation, _chestTargetRotation, Time.deltaTime * _leanSpeed);
        }

        /// <summary>
        /// Applies the correct rotation determined in HandleLeanInput.
        /// </summary>

        protected virtual void ApplyLean()
        {
            // Override animation data on the chest with our dummy transform

            _chestTransform.localRotation = _chestOverrideTransform;
        }

        /// <summary>
        /// Captures any punches the player initiates.
        /// </summary>

        protected virtual void HandlePunching()
        {
            _animancer.States.TryGet("_Punch.R", out var punchRState);
            _animancer.States.TryGet("_Punch.L", out var punchLState);

            if (_punchButtonPressed && !_secondPunchQueued)
            {
                _punchButtonPressed = false;

                if (IsGrounded())
                {
                    if (!FirstPunchIsAnimating() && !SecondPunchIsAnimating())
                    {
                        jumpInputAction.Disable();

                        FirstPunchThrown?.Invoke();
                    }
                    else if (punchRState.Weight < 0.75 && punchRState.Time >= punchRState.Length)
                    {
                        punchRState.Time = 0;

                        _animancer.TryPlay("_Punch.R", 0.15f);
                        punchRState.Events.OnEnd = ResetAnimationState;
                    }
                    else if (!SecondPunchIsAnimating())
                    {
                        //if (punchRState.Weight < 0.75 && punchRState.Time >= punchRState.Length)
                        //    return;

                        jumpInputAction.Disable();

                        SecondPunchQueued?.Invoke();
                    }
                    else if (punchLState.Time >= punchLState.Length)
                    {
                        punchRState.Time = 0;

                        _animancer.TryPlay("_Punch.R");
                        punchRState.Events.OnEnd = ResetAnimationState;
                    }
                }
                else
                {
                    // Do air punch logic
                }
            }
            else if (!SecondPunchIsAnimating() && _secondPunchQueued)
            {
                _punchButtonPressed = false;
                
                SecondPunchThrown?.Invoke();
            }

            if (FirstPunchIsAnimating() || SecondPunchIsAnimating())
                SetMovementDirection(Vector3.zero);
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
            _waitingForJumpApex = true;
            
            if (_rightFootUp)
            {
                _animancer.TryPlay("_JiggleJump.L", 0.15f);
            }
            else
            {
                _animancer.TryPlay("_JiggleJump.R", 0.15f);
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
        /// Play the land animation for the character.
        /// </summary>

        protected virtual void PlayLandAnimation()
        {
            punchInputAction.Enable();

            var state = _animancer.TryPlay("_Land", 0.25f);
            state.Events.OnEnd = ResetAnimationState;
        }

        /// <summary>
        /// Start a punch initiated by the player.
        /// </summary>

        protected virtual void Punch()
        {
            _punchButtonPressed = true;
        }

        /// <summary>
        /// Play the first punch animation for the character.
        /// </summary>

        protected virtual void PlayFirstPunchAnimation()
        {
            if (_animancer.States.TryGet("_Punch.R", out var state))
            {
                _animancer.TryPlay("_Punch.R");
                state.Events.OnEnd = ResetAnimationState;
            }
        }

        /// <summary>
        /// Is the character's first punch still being animated?
        /// </summary>

        protected virtual bool FirstPunchIsAnimating()
        {
            if (_animancer.States.TryGet("_Punch.R", out var state))
            {
                if (state.Weight > 0)
                {
                    _firstPunchIsAnimating = true;
                    canEverJump = false;
                }
                else
                {
                    _firstPunchIsAnimating = false;
                    canEverJump = true;
                }
            }

            return _firstPunchIsAnimating;
        }

        /// <summary>
        /// Queue the second punch for the character.
        /// </summary>

        protected virtual void QueueSecondPunch()
        {
            _secondPunchQueued = true;
        }

        /// <summary>
        /// Play the second punch animation for the character.
        /// </summary>

        protected virtual void PlaySecondPunchAnimation()
        {
            _animancer.States.TryGet("_Punch.R", out var punchRState);

            if (punchRState.Time >= punchRState.Length * 0.75f)
            {
                _secondPunchQueued = false;
                _isThrowingSecondPunch = true;

                var punchLState = _animancer.TryPlay("_Punch.L");
                punchLState.Events.OnEnd = ResetAnimationState;
            }
        }

        /// <summary>
        /// Is the character's second punch still being animated?
        /// </summary>

        protected virtual bool SecondPunchIsAnimating()
        {
            if (_animancer.States.TryGet("_Punch.L", out var state))
            {
                if (state.Weight > 0)
                {
                    _secondPunchIsAnimating = true;
                    canEverJump = false;
                }
                else
                {
                    _secondPunchIsAnimating = false;
                    canEverJump = true;
                }
            }

            return _secondPunchIsAnimating;
        }

        /// <summary>
        /// Stop the player from punching.
        /// </summary>

        protected virtual void StopPunching()
        {
            _punchButtonPressed = false;
        }

        /// <summary>
        /// Play the idle animation for the character.
        /// </summary>

        protected virtual void PlayIdleAnimation()
        {
            if (!_animancer.IsPlaying("_Land"))
            {
                _animancer.TryPlay("_Idle", 0.15f);
            }
        }

        /// <summary>
        /// Play the run animation for the character.
        /// </summary>

        protected virtual void PlayRunAnimation(Vector2 movementInput)
        {
            if (movementInput.y > 0f || movementInput.y < 0f)
            {
                _animancer.TryPlay("_Run", 0.25f);
            }

            else if (movementInput.x > 0f || movementInput.x < 0f)
            {
                _animancer.TryPlay("_Run", 0.25f);
            }
        }

        /// <summary>
        /// Return the character to idle.
        /// </summary>

        protected virtual void ResetAnimationState()
        {
            jumpInputAction.Enable();
            _animancer.TryPlay("_Idle", 0.25f);
        }

        #endregion
    }
}