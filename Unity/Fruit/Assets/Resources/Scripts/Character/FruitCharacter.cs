using Animancer;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        [SerializeField] private NamedAnimancerComponent _animancer;
        [SerializeField] private NetworkAnimations _networkAnimations;
        [SerializeField] private Transform _chestTransform; // Reference to the chest bone
        [SerializeField] private float _leanAmount = 12.5f; // Maximum degrees to lean
        [SerializeField] private float _leanSpeed = 8f; // Speed at which the lean is applied
        [SerializeField] private float _cooldownDuration = 0.15f; // Cooldown duration after combo ends or fails

        private bool _rightFootUp, _isSpeeding, _queueLaunch, 
            _blockButtonPressed, _isBlocking, _takingDamage, 
            _punchButtonPressed, _isGroundPunching, _isAirPunching,
            _pauseButtonPressed;
        private int _currentComboStep, _nextComboStep; // Keep track of the next combo step
        private float _cooldownTimer;
        private string _currentAnimationClip;
        private string[] _comboAnimations = { "_Punch.1", "_Punch.2", "_Punch.3" };
        private Queue<int> _punchQueue = new Queue<int>();
        private Quaternion _chestOverrideTransform; // The dummy transform used to update the real one
        private Quaternion _chestTargetRotation; // Target rotation for the lean

        private bool _isPunching => _isGroundPunching || _isAirPunching;

        #endregion

        #region INPUT ACTIONS

        /// <summary>
        /// Punch InputAction.
        /// </summary>

        protected InputAction punchInputAction { get; set; }

        /// <summary>
        /// Block InputAction.
        /// </summary>

        protected InputAction blockInputAction { get; set; }

        /// <summary>
        /// Pause InputAction.
        /// </summary>

        protected InputAction pauseInputAction { get; set; }

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
                ReleasePunch();
        }

        /// <summary>
        /// Block input action handler.
        /// </summary>

        protected virtual void OnBlock(InputAction.CallbackContext context)
        {
            if (context.started || context.performed)
                Block();
            else if (context.canceled)
                StopBlocking();
        }

        /// <summary>
        /// Pause input action handler.
        /// </summary>

        protected virtual void OnPause(InputAction.CallbackContext context)
        {
            if (context.started || context.performed)
                Pause();
            else if (context.canceled)
                ReleasePause();
        }

        #endregion

        #region EVENTS

        public delegate void LaunchedEventHandler();

        /// <summary>
        /// Event triggered when character gets launched.
        /// </summary>

        public event LaunchedEventHandler Launched;

        #endregion

        #region MONOBEHAVIOR

        /// <summary>
        /// Called when the script instance is being loaded (Awake).
        /// If overridden, must call base method in order to fully initialize the class.
        /// </summary>

        protected override void OnAwake()
        {
            base.OnAwake();

            // Cache the Character's starting animation clip
            _currentAnimationClip = "Idle";

            _rightFootUp = true;
            _isSpeeding = false;
            _queueLaunch = false;

            _punchButtonPressed = false;
            _isGroundPunching = false;
            _isAirPunching = false;

            _blockButtonPressed = false;
            _isBlocking = false;
            _takingDamage = false;

            _pauseButtonPressed = false;

            _currentComboStep = 0;
            _nextComboStep = 0;
            _cooldownTimer = 0f;
        }

        /// <summary>
        /// Extends OnStart.
        /// Setup the local player's Character.
        /// </summary>

        protected override void OnStart()
        {
            base.OnStart();

            // We don't want to take control of another player's character
            if (!isLocalPlayer)
            {
                //UnsubFromInputActions();

                return;
            }

            NetworkIdentity characterIdentity = GetComponent<NetworkIdentity>();
            UIManager.Instance.SetupPauseMenu(characterIdentity);

            InitPlayerInput();
            
            camera = Camera.main;

            Jumped += PlayJumpAnimation;
            Landed += PlayLandAnimation;
            Launched += PlayLaunchAnimation;
            UIManager.Instance.Paused += CheckIfPaused;
        }

        /// <summary>
        /// Extends OnStartLocalPlayer.
        /// Setup the local player's input, pause menu, and camera.
        /// </summary>

        //public override void OnStartLocalPlayer()
        //{
        //    base.OnStartLocalPlayer();

        //    UIManager.Instance.SetupPauseMenu(GetComponent<NetworkIdentity>());
        //    camera = Camera.main;
        //}

        /// <summary>
        /// Subscribe to events related to being off the ground.
        /// </summary>

        //protected override void OnOnEnable()
        //{
        //    base.OnOnEnable();

        //    Jumped += PlayJumpAnimation;
        //    Landed += PlayLandAnimation;
        //    Launched += PlayLaunchAnimation;

        //    UIManager.Instance.Paused += CheckIfPaused;
        //}

        /// <summary>
        /// Unsubscribe from events related to being off the ground.
        /// </summary>

        protected override void OnOnDisable()
        {
            base.OnOnDisable();

            Jumped -= PlayJumpAnimation;
            Landed -= PlayLandAnimation;
            Launched -= PlayLaunchAnimation;
            UIManager.Instance.Paused -= CheckIfPaused;
        }

        /// <summary>
        /// Our Update method.
        /// </summary>

        protected override void OnUpdate()
        {
            if (isLocalPlayer)
            {
                base.OnUpdate();

                HandleLeanInput();
            }

            //Debug.DrawLine(transform.position, GetVelocity() * 10, Color.red, .5f);
        }

        /// <summary>
        /// Our LateFixedUpdate method. E.g called AFTER Physics internal update.
        /// </summary>

        protected override void OnLateFixedUpdate()
        {
            if (isLocalPlayer)
                ApplyLean();
            
            base.OnLateFixedUpdate();
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

            // Setup Block input action handlers
            blockInputAction = inputActions.FindAction("Block");
            if (blockInputAction != null)
            {
                blockInputAction.started += OnBlock;
                blockInputAction.performed += OnBlock;
                blockInputAction.canceled += OnBlock;

                blockInputAction.Enable();
            }

            // Setup Pause input action handlers
            pauseInputAction = inputActions.FindAction("Pause");
            if (pauseInputAction != null)
            {
                pauseInputAction.started += OnPause;
                pauseInputAction.performed += OnPause;
                pauseInputAction.canceled += OnPause;

                pauseInputAction.Enable();
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

            if (blockInputAction != null)
            {
                blockInputAction.started -= OnBlock;
                blockInputAction.performed -= OnBlock;
                blockInputAction.canceled -= OnBlock;

                blockInputAction.Disable();
                blockInputAction = null;
            }

            if (pauseInputAction != null)
            {
                pauseInputAction.started -= OnPause;
                pauseInputAction.performed -= OnPause;
                pauseInputAction.canceled -= OnPause;

                pauseInputAction.Disable();
                pauseInputAction = null;
            }
        }

        /// <summary>
        /// Handle Player input, only if actions are assigned (eg: actions != null).
        /// </summary>

        protected override void HandleInput()
        {
            base.HandleInput();

            HandlePunching();

            HandleBlocking();

            HandlePausing();
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
        /// Perform character's movement based on its current MovementMode.
        /// </summary>

        protected override void Move()
        {
            // If Character movement is disabled, return
            if (IsDisabled())
                return;

            // Toggle walking / falling mode based on ground status
            if (IsWalking() && !characterMovement.isGrounded)
                SetMovementMode(MovementMode.Falling);

            if (IsFalling() && characterMovement.isGrounded)
                SetMovementMode(MovementMode.Walking);

            // Compute new velocity based on Character's movement mode
            Vector3 desiredVelocity = CalcDesiredVelocity();

            switch (_movementMode)
            {
                case MovementMode.None:
                    characterMovement.velocity = Vector3.zero;
                    break;

                case MovementMode.Walking:
                    Walking(desiredVelocity);
                    break;

                case MovementMode.Falling:
                    Falling(desiredVelocity);
                    break;

                case MovementMode.Flying:
                    Flying(desiredVelocity);
                    break;

                case MovementMode.Swimming:
                    Swimming(desiredVelocity);
                    break;

                case MovementMode.Custom:
                    CustomMovementMode(desiredVelocity);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Handle crouching state
            Crouching();

            // Handle jumping state
            Jumping();

            // Handle launching state
            Launching();

            // Move the character (perform collision constrained movement) with velocity updated by movement mode
            characterMovement.Move(deltaTime);
        }

        /// <summary>
        /// Check what state the character is in and play the appropriate animation.
        /// </summary>

        protected override void Animate()
        {
            if (_isPunching || _isBlocking || _takingDamage)
            {
                // Override movement animations when punching/blocking
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
                    if (_isSpeeding)
                    {
                        PlaySprintAnimation(movementInput);
                    }
                    else
                    {
                        PlayRunAnimation(movementInput);
                    }
                }
            }
            else if (!WaitingForJumpApex())
            {
                PlayFallAnimation();
            }
        }

        /// <summary>
        /// Saves the rotation data needed to lean the character in the direction they move.
        /// </summary>

        private void HandleLeanInput()
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
            _chestOverrideTransform = Quaternion.Slerp(_chestTransform.localRotation, _chestTargetRotation, Time.fixedDeltaTime * _leanSpeed);
        }

        /// <summary>
        /// Applies the correct rotation determined in HandleLeanInput.
        /// </summary>

        private void ApplyLean()
        {
            // Override animation data on the chest with our dummy transform
            _chestTransform.localRotation = _chestOverrideTransform;
        }

        /// <summary>
        /// Unsub from all input action handlers.
        /// </summary>

        //private void UnsubFromInputActions()
        //{
        //    movementInputAction = null;

        //    if (sprintInputAction != null)
        //    {
        //        sprintInputAction.started -= OnSprint;
        //        sprintInputAction.performed -= OnSprint;
        //        sprintInputAction.canceled -= OnSprint;

        //        sprintInputAction = null;
        //    }

        //    if (crouchInputAction != null)
        //    {
        //        crouchInputAction.started -= OnCrouch;
        //        crouchInputAction.performed -= OnCrouch;
        //        crouchInputAction.canceled -= OnCrouch;

        //        crouchInputAction = null;
        //    }

        //    if (jumpInputAction != null)
        //    {
        //        jumpInputAction.started -= OnJump;
        //        jumpInputAction.performed -= OnJump;
        //        jumpInputAction.canceled -= OnJump;

        //        jumpInputAction = null;
        //    }

        //    if (punchInputAction != null)
        //    {
        //        punchInputAction.started -= OnPunch;
        //        punchInputAction.performed -= OnPunch;
        //        punchInputAction.canceled -= OnPunch;

        //        punchInputAction = null;
        //    }

        //    if (blockInputAction != null)
        //    {
        //        blockInputAction.started -= OnBlock;
        //        blockInputAction.performed -= OnBlock;
        //        blockInputAction.canceled -= OnBlock;

        //        blockInputAction = null;
        //    }

        //    if (pauseInputAction != null)
        //    {
        //        pauseInputAction.started -= OnPause;
        //        pauseInputAction.performed -= OnPause;
        //        pauseInputAction.canceled -= OnPause;

        //        pauseInputAction = null;
        //    }

        //    UIManager.Instance.Paused -= CheckIfPaused;
        //}

        /// <summary>
        /// Check whether the pause menu is active/inactive,
        ///  and disable/enable player controls.
        /// </summary>

        private void CheckIfPaused()
        {
            if (UIManager.Instance.PauseMenuActive())
            {
                DisableCharacter();
            }
            else if (!_takingDamage)
            {
                // We shouldn't re-enable movement if taking damage
                //  Instead, the StopDamage method will re-enable movement
                EnableCharacter();
            }
        }

        /// <summary>
        /// Disables the player's character.
        /// </summary>

        private void DisableCharacter()
        {
            movementInputAction.Disable();
            jumpInputAction.Disable();
            punchInputAction.Disable();
            blockInputAction.Disable();
        }

        /// <summary>
        /// Enables the player's character.
        /// </summary>

        private void EnableCharacter()
        {
            movementInputAction.Enable();
            jumpInputAction.Enable();
            punchInputAction.Enable();
            blockInputAction.Enable();
        }

        /// <summary>
        /// Captures any punches the player initiates.
        /// </summary>

        private void HandlePunching()
        {
            if (IsGrounded())
            {
                if (_isAirPunching)
                {
                    ResetAirPunch();
                }

                HandleGroundedPunch();
            }
            else
            {
                if (_isGroundPunching)
                {
                    ResetCombo();
                }

                HandleAirPunch();
            }
        }

        /// <summary>
        /// Captures block input that the player initiates.
        /// </summary>

        private void HandleBlocking()
        {
            if (IsGrounded() && _blockButtonPressed)
            {
                if (!_isPunching)
                {
                    _isBlocking = true;

                    punchInputAction.Disable();
                    movementInputAction.Disable();
                    jumpInputAction.Disable();

                    _animancer.TryPlay("_Block", 0.15f);

                    if (_currentAnimationClip != "_Block")
                    {
                        _currentAnimationClip = "_Block";
                        _networkAnimations.CmdPlayBlockAnimation();
                    }
                }
            }
        }

        /// <summary>
        /// Captures pause input that the player initiates.
        /// </summary>

        private void HandlePausing()
        {
            if (_pauseButtonPressed)
            {
                _pauseButtonPressed = false;

                UIManager.Instance.TogglePauseMenu();
            }
        }

        /// <summary>
        /// Start a punch initiated by the player.
        /// </summary>

        private void Punch()
        {
            if (_cooldownTimer <= 0f)
            {
                _punchButtonPressed = true;
            }
        }

        /// <summary>
        /// Manually releases the punch button, allowing the player
        ///  to initiate another punch, possibly a combo.
        /// </summary>

        private void ReleasePunch()
        {
            _punchButtonPressed = false;
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
                if (!_isGroundPunching)
                {
                    _punchQueue.Enqueue(0); // Always start combo with the first punch
                    _nextComboStep = 1;      // Update the next combo step after the first punch
                    ExecuteComboStep();
                }
                else if (_nextComboStep < _comboAnimations.Length)
                {
                    // Queue the next punch if already punching
                    _punchQueue.Enqueue(_nextComboStep);
                    _nextComboStep++;  // Move to the next step in the combo
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

            // Get the next punch index from the queue
            _currentComboStep = _punchQueue.Dequeue();

            // Play and adjust the animation state
            if (_animancer.States.TryGet(_comboAnimations[_currentComboStep], out var state))
            {
                _animancer.Play(state);
                state.Speed = 1.25f;
                state.Time = 0f;

                if (_currentAnimationClip != _comboAnimations[_currentComboStep])
                {
                    _currentAnimationClip = _comboAnimations[_currentComboStep];
                    _networkAnimations.CmdPlayPunchAnimation(_currentAnimationClip);
                }
            }

            // Mark as punching
            _isGroundPunching = true;

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

            // Check if we're holding the last frame for a ground/air punch
            if (IsGrounded())
            {
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
            else
            {
                ResetAirPunch();
            }
        }

        /// <summary>
        /// Reset the character's combo to a default state.
        /// </summary>

        private void ResetCombo()
        {
            // Transition back to idle animation
            if (IsGrounded())
                PlayIdleAnimation();

            // Reset combo state
            _isGroundPunching = false;
            _currentComboStep = 0;

            // Clear the queue
            _punchQueue.Clear();

            // Only re-enable movement if the player isn't paused
            if (!UIManager.Instance.PauseMenuActive())
            {
                movementInputAction.Enable();
                jumpInputAction.Enable();
            }
        }

        /// <summary>
        /// Starts a cooldown to avoid punch spamming.
        /// </summary>

        private void StartCooldown()
        {
            _cooldownTimer = _cooldownDuration;
        }

        /// <summary>
        /// Handles punch logic while the character is in the air.
        /// </summary>

        private void HandleAirPunch()
        {
            if (_punchButtonPressed)
            {
                if (!_isAirPunching)
                {
                    string airPunchClip = _rightFootUp ? "_AirPunch.R" : "_AirPunch.L";

                    SetRotationMode(RotationMode.None);

                    if (_animancer.States.TryGet(airPunchClip, out var state))
                    {
                        _animancer.Play(state);
                        state.Speed = 1.25f;
                        state.Time = 0f;

                        if (_currentAnimationClip != airPunchClip)
                        {
                            _currentAnimationClip = airPunchClip;
                            _networkAnimations.CmdPlayAirPunchAnimation(_currentAnimationClip);
                        }
                    }

                    _isAirPunching = true;

                    state.Events.OnEnd = () =>
                    {
                        StartCoroutine(HoldLastFrame(state, 0.1f));
                    };
                }

                ReleasePunch();
            }
        }

        /// <summary>
        /// Reset the character's air punch to a default state.
        /// </summary

        private void ResetAirPunch()
        {
            PlayFallAnimation();

            _isAirPunching = false;

            SetRotationMode(RotationMode.OrientToMovement);
        }

        /// <summary>
        /// Start blocking, initiated by the player.
        /// </summary>

        private void Block()
        {
            _blockButtonPressed = true;
        }

        /// <summary>
        /// Manually releases the block button,
        ///  stopping the player from blocking.
        /// </summary>

        private void StopBlocking()
        {
            _blockButtonPressed = false;

            if (_isBlocking)
            {
                _isBlocking = false;

                // Only re-enable movement if the player isn't paused
                if (!UIManager.Instance.PauseMenuActive())
                {
                    punchInputAction.Enable();
                    movementInputAction.Enable();
                    jumpInputAction.Enable();
                }
            }
        }

        /// <summary>
        /// Initiate the pause menu.
        /// </summary>

        private void Pause()
        {
            _pauseButtonPressed = true;
        }

        /// <summary>
        /// Release the pause button, allowing the player 
        ///  to open or close the pause menu again.
        /// </summary>

        private void ReleasePause()
        {
            _pauseButtonPressed = false;
        }

        /// <summary>
        /// Handle launching state.
        /// Eg: check if a launch was queued.
        /// </summary>

        private void Launching()
        {
            if (_queueLaunch)
            {
                _queueLaunch = false;

                if (_isAirPunching)
                    ResetAirPunch();

                if (_isBlocking)
                    StopBlocking();

                Vector3 newVerticalVelocity = GetVelocity();
                newVerticalVelocity.y = 0.0f;
                SetVelocity(newVerticalVelocity);

                SetMovementMode(MovementMode.Falling);

                PauseGroundConstraint();

                // Launch the player upwards
                LaunchCharacter(GetUpVector() * 4f, true);

                _waitingForJumpApex = true;

                Launched?.Invoke();
            }
        }

        /// <summary>
        /// Play the launch animation for the character.
        /// </summary>

        private void PlayLaunchAnimation()
        {
            _animancer.TryPlay("_Launch", 0.25f);

            if (_currentAnimationClip != "_Launch")
            {
                _currentAnimationClip = "_Launch";
                _networkAnimations.CmdPlayLaunchAnimation();
            }
        }

        /// <summary>
        /// Sets the right foot as up so we can use that foot to jump.
        /// </summary>

        private void SetRightFootUp()
        {
            if (IsGrounded())
                _rightFootUp = true;
        }

        /// <summary>
        /// Sets the right foot as down so we can use the left foot to jump.
        /// </summary>

        private void SetRightFootDown()
        {
            if (IsGrounded())
                _rightFootUp = false;
        }

        /// <summary>
        /// Play the jump animation for the character.
        /// </summary>

        private void PlayJumpAnimation()
        {
            string jumpClip = _rightFootUp ? "_Jump.L" : "_Jump.R";

            if (_animancer.States.TryGet(jumpClip, out var state))
            {
                _animancer.Play(state, 0.25f);
                state.Time = 0;

                if (_currentAnimationClip != jumpClip)
                {
                    _currentAnimationClip = jumpClip;
                    _networkAnimations.CmdPlayJumpAnimation(_currentAnimationClip);
                }
            }
        }

        /// <summary>
        /// Play the fall animation for the character.
        /// </summary>

        private void PlayFallAnimation()
        {
            string fallClip = _rightFootUp ? "_Fall.R" : "_Fall.L";

            if (_animancer.States.TryGet(fallClip, out var state))
            {
                _animancer.Play(state, 0.25f);

                if (_currentAnimationClip != fallClip)
                {
                    state.Time = 0;

                    _currentAnimationClip = fallClip;
                    _networkAnimations.CmdPlayFallAnimation(_currentAnimationClip);
                }
            }
        }

        /// <summary>
        /// Play the land animation for the character.
        /// </summary>

        private void PlayLandAnimation()
        {
            _animancer.TryPlay("_Land", 0.25f);

            if (_currentAnimationClip != "_Land")
            {
                _currentAnimationClip = "_Land";
                _networkAnimations.CmdPlayLandAnimation();
            }
        }

        /// <summary>
        /// Play the idle animation for the character.
        /// </summary>

        private void PlayIdleAnimation()
        {
            if (!_animancer.IsPlaying("_Land"))
            {
                _animancer.TryPlay("_Idle", 0.15f);

                if (_currentAnimationClip != "_Idle")
                {
                    _currentAnimationClip = "Idle";
                    _networkAnimations.CmdPlayIdleAnimation();
                }
            }
        }

        /// <summary>
        /// Play the run animation for the character.
        /// </summary>

        private void PlayRunAnimation(Vector2 movementInput)
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

                if (_currentAnimationClip != "_Run")
                {
                    _currentAnimationClip = "_Run";
                    _networkAnimations.CmdPlayRunAnimation(inputMagnitude);
                }
            }
        }

        /// <summary>
        /// Play the sprint animation for the character.
        /// </summary>

        private void PlaySprintAnimation(Vector2 movementInput)
        {
            if (movementInput.y != 0f || movementInput.x != 0f)
            {
                // Input magnitude determines how fast to animate the run animation
                //  when the player is slightly tilting the control stick
                float inputMagnitude = movementInput.magnitude;

                if (inputMagnitude < 0.25f)
                    inputMagnitude = 0.25f;

                var state = _animancer.TryPlay("_Sprint", 0.25f);
                state.Speed = 1.25f * inputMagnitude;

                if (_currentAnimationClip != "_Sprint")
                {
                    _currentAnimationClip = "_Sprint";
                    _networkAnimations.CmdPlaySprintAnimation(inputMagnitude);
                }
            }
        }

        /// <summary>
        /// Reset the character's speed.
        /// </summary>

        private void ResetSpeed()
        {
            maxWalkSpeed = 3f;

            _isSpeeding = false;
        }

        /// <summary>
        /// Take the character out of the damage state.
        /// </summary>

        private void StopDamage()
        {
            _takingDamage = false;

            // Only re-enable movement if the player isn't paused
            if (!UIManager.Instance.PauseMenuActive())
            {
                EnableCharacter();
            }
        }

        /// <summary>
        /// Speed the character up.
        /// </summary>

        public void BoostSpeed(float effectDuration)
        {
            if (!_isSpeeding)
            {
                maxWalkSpeed = 5f;

                _isSpeeding = true;

                Invoke(nameof(ResetSpeed), effectDuration);
            }
        }

        /// <summary>
        /// Queue the character to launch.
        /// Happens during the next simulation step.
        /// </summary>

        public void QueueLaunch()
        {
            _queueLaunch = true;
        }

        /// <summary>
        /// Damage the character and push them back.
        /// </summary>

        public void TakeDamage(float effectDuration, Vector3 direction)
        {
            if (!_takingDamage)
            {
                _takingDamage = true;

                ResetCombo();
                ResetAirPunch();

                DisableCharacter();

                SetVelocity(Vector3.zero);

                PauseGroundConstraint();

                // Push the player back
                LaunchCharacter((direction * 5f) + (GetUpVector() * 2.5f), true);

                if (_animancer.States.TryGet("_Hurt", out var state))
                {
                    _animancer.Play(state);
                    state.Time = 0f;

                    if (_currentAnimationClip != "_Hurt")
                    {
                        _currentAnimationClip = "_Hurt";
                        _networkAnimations.CmdPlayHurtAnimation();
                    }
                }

                Invoke(nameof(StopDamage), effectDuration);
            }
        }

        #endregion
    }
}