using Animancer;
using System.Collections;
using System.Collections.Generic;
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
        private bool _rightFootUp, _punchButtonPressed, _isPunching;
        private int _currentComboStep = 0;
        private int nextComboStep = 0;  // Keep track of the next combo step
        private float _cooldownTimer = 0f;
        private string[] _comboAnimations = { "_Punch.1", "_Punch.2", "_Punch.3" };
        private Queue<int> _punchQueue = new Queue<int>();
        private Quaternion _chestOverrideTransform; // The dummy transform used to update the real one
        private Quaternion _chestTargetRotation; // Target rotation for the lean

        [SerializeField] private NamedAnimancerComponent _animancer;
        [SerializeField] private Transform _chestTransform; // Reference to the chest bone
        [SerializeField] private float _leanAmount = 12.5f; // Maximum degrees to lean
        [SerializeField] private float _leanSpeed = 8f; // Speed at which the lean is applied
        [SerializeField] private float _cooldownDuration = 0.15f; // Cooldown duration after combo ends or fails


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
                StartPunch();
            else if (context.canceled)
                ReleasePunch();
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
            _isPunching = false;
        }

        /// <summary>
        /// Subscribe to events related to being off the ground.
        /// </summary>

        protected override void OnOnEnable()
        {
            base.OnOnEnable();

            Jumped += PlayJumpAnimation;
            Landed += PlayLandAnimation;
        }

        /// <summary>
        /// Unsubscribe from events related to being off the ground.
        /// </summary>

        protected override void OnOnDisable()
        {
            base.OnOnDisable();

            Jumped -= PlayJumpAnimation;
            Landed -= PlayLandAnimation;
        }

        /// <summary>
        /// Check what state the character is in and play the appropriate animation.
        /// </summary>

        protected override void Animate()
        {
            if (_isPunching)
            {
                // Override movement animations when punching
                return;
            }

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

            //Debug.DrawLine(transform.position, GetVelocity() * 10, Color.red, .5f);
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
            if (IsGrounded())
            {
                HandleGroundedPunch();
            }
        }

        /// <summary>
        /// Start a punch initiated by the player.
        /// </summary>

        protected virtual void StartPunch()
        {
            if (_cooldownTimer <= 0f)
            {
                _punchButtonPressed = true;
            }
        }

        /// <summary>
        /// Handles punch logic while the character is grounded.
        /// </summary>

        private void HandleGroundedPunch()
        {
            // Handle cooldown timer
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
                return; // Exit early during cooldown
            }

            if (_punchButtonPressed)
            {
                if (!_isPunching)
                {
                    _punchQueue.Enqueue(0); // Always start combo with the first punch
                    nextComboStep = 1;      // Update the next combo step after the first punch
                    ExecuteComboStep();
                }
                else if (nextComboStep < _comboAnimations.Length)
                {
                    // Queue the next punch if already punching
                    _punchQueue.Enqueue(nextComboStep);
                    nextComboStep++;  // Move to the next step in the combo
                }

                ReleasePunch(); // Reset the button press
            }
        }

        /// <summary>
        /// Combo different punches according to what is currently queued.
        /// </summary>

        private void ExecuteComboStep()
        {
            if (_punchQueue.Count == 0)
            {
                // If the queue is empty, reset the combo
                ResetCombo();
                StartCooldown();
                return;
            }

            // Disable input when punching starts
            movementInputAction.Disable();
            jumpInputAction.Disable();
            canEverJump = false;

            // Get the next punch index from the queue
            _currentComboStep = _punchQueue.Dequeue();

            // Play and adjust the animation state
            _animancer.TryPlay(_comboAnimations[_currentComboStep]);
            if (_animancer.States.TryGet(_comboAnimations[_currentComboStep], out var state))
            {
                state.Speed = 1.25f;
                state.Time = 0f;
            }

            // Mark as punching
            _isPunching = true;

            // Apply a slight forward push for each punch
            LaunchCharacter(transform.forward * 1.5f);

            // Schedule the transition back to idle after the animation
            state.Events.OnEnd = () =>
            {
                if (_punchQueue.Count > 0)
                {
                    // Execute the next combo step immediately
                    ExecuteComboStep();
                }
                else
                {
                    // No punch is queued, so hold the last frame
                    StartCoroutine(HoldLastFrame(state, 0.1f));
                }
            };
        }

        /// <summary>
        /// Hold the last frame of the punch to give time for a combo.
        /// </summary>

        private IEnumerator HoldLastFrame(AnimancerState state, float holdDuration)
        {
            // Freeze on the last frame
            state.Speed = 0f;
            state.Time = state.Length;

            yield return new WaitForSeconds(holdDuration);

            if (_punchQueue.Count > 0)
            {
                ExecuteComboStep();
            }
            else
            {
                ResetCombo();
                StartCooldown();
            }
        }

        /// <summary>
        /// Manually releases the punch button.
        /// </summary>

        protected virtual void ReleasePunch()
        {
            _punchButtonPressed = false;
        }

        /// <summary>
        /// Reset the character back to a default state.
        /// </summary>

        private void ResetCombo()
        {
            // Transition back to idle animation
            PlayIdleAnimation();

            // Reset combo state
            _isPunching = false;
            _currentComboStep = 0;

            // Clear the queue
            _punchQueue.Clear();

            // Re-enable input when done punching
            movementInputAction.Enable();
            jumpInputAction.Enable();
            canEverJump = true;

        }

        /// <summary>
        /// Starts a cooldown to avoid punch spamming.
        /// </summary>

        private void StartCooldown()
        {
            _cooldownTimer = _cooldownDuration;
        }

        /// <summary>
        /// Sets the right foot as up so we can use that foot to jump.
        /// </summary>

        protected virtual void SetRightFootUp()
        {
            if (IsGrounded())
                _rightFootUp = true;
        }

        /// <summary>
        /// Sets the right foot as down so we can use the left foot to jump.
        /// </summary>

        protected virtual void SetRightFootDown()
        {
            if (IsGrounded())
                _rightFootUp = false;
        }

        /// <summary>
        /// Play the jump animation for the character and check for their apex.
        /// </summary>

        protected virtual void PlayJumpAnimation()
        {
            string jumpClip = _rightFootUp ? "_Jump.L" : "_Jump.R";

            if (_animancer.States.TryGet(jumpClip, out var state))
            {
                state.Time = 0;
                _animancer.Play(state, 0.25f);
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
            _animancer.TryPlay("_Land", 0.25f);
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
            if (movementInput.y != 0f || movementInput.x != 0f)
            {
                // Input magnitude determines how fast to animate the run animation
                //  when the player is slightly tilting the control stick
                float inputMagnitude = movementInput.magnitude;
                

                if (inputMagnitude < 0.25f)
                    inputMagnitude = 0.25f;

                var state = _animancer.TryPlay("_Run", 0.25f);
                state.Speed = 1.25f * inputMagnitude;
            }
        }

        #endregion
    }
}