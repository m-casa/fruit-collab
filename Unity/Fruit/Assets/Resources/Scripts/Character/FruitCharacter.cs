using Animancer;
using Mirror;
using System;
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
        [SerializeField] private Combat _combat;
        [SerializeField] private Transform _chestTransform; // Reference to the chest bone
        [SerializeField] private float _leanAmount = 12.5f; // Maximum degrees to lean
        [SerializeField] private float _leanSpeed = 8f; // Speed at which the lean is applied

        private Quaternion _chestOverrideTransform; // The dummy transform used to update the real one
        private Quaternion _chestTargetRotation; // Target rotation for the lean
        private string _currentAnimationClip;
        private float currentRunMagnitude; // Keeps track of the current run magnitude input
        private bool _rightFootUp, _isSpeeding, _queueLaunch, 
            _pauseButtonPressed, _readyButtonPressed;

        #endregion

        #region PROPERTIES

        public NamedAnimancerComponent animancer => _animancer;

        public NetworkAnimations networkAnimations => _networkAnimations;

        public Combat combat => _combat;

        public bool rightFootUp => _rightFootUp;

        public string currentAnimationClip
        {
            get => _currentAnimationClip;
            set => _currentAnimationClip = value;
        }

        #endregion

        #region INPUT ACTIONS

        /// <summary>
        /// Ready InputAction.
        /// </summary>

        public InputAction readyInputAction { get; set; }

        /// <summary>
        /// Pause InputAction.
        /// </summary>

        public InputAction pauseInputAction { get; set; }

        /// <summary>
        /// Cursor Lock InputAction.
        /// </summary>

        public InputAction cursorLockInputAction { get; set; }

        /// <summary>
        /// Cursor Unlock InputAction.
        /// </summary>

        public InputAction cursorUnlockInputAction { get; set; }

        #endregion

        #region INPUT ACTION HANDLERS

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

        /// <summary>
        /// Ready input action handler.
        /// </summary>

        protected virtual void OnReady(InputAction.CallbackContext context)
        {
            if (context.started || context.performed)
                Ready();
            else if (context.canceled)
                ReleaseReady();
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

            _pauseButtonPressed = false;
            _readyButtonPressed = false;
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
                return;
            }

            NetworkIdentity characterIdentity = GetComponent<NetworkIdentity>();
            UIManager.Instance.SetupPauseMenu(characterIdentity);

            InitPlayerInput();
            
            camera = Camera.main;

            Jumped += PlayJumpAnimation;
            Landed += PlayLandAnimation;
            Launched += PlayLaunchAnimation;
            UIManager.Instance.PauseToggled += CheckIfPauseMenuActive;
        }

        /// <summary>
        /// Unsubscribe from events related to being off the ground.
        /// </summary>

        protected override void OnOnDisable()
        {
            base.OnOnDisable();

            Jumped -= PlayJumpAnimation;
            Landed -= PlayLandAnimation;
            Launched -= PlayLaunchAnimation;
            UIManager.Instance.PauseToggled -= CheckIfPauseMenuActive;
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

            combat.InitializeCombat();

            // Setup Pause input action handlers
            pauseInputAction = inputActions.FindAction("Pause");
            if (pauseInputAction != null)
            {
                pauseInputAction.started += OnPause;
                pauseInputAction.performed += OnPause;
                pauseInputAction.canceled += OnPause;

                pauseInputAction.Enable();
            }

            // Setup Ready input action handlers
            readyInputAction = inputActions.FindAction("Ready");
            if (readyInputAction != null)
            {
                readyInputAction.started += OnReady;
                readyInputAction.performed += OnReady;
                readyInputAction.canceled += OnReady;

                readyInputAction.Enable();
            }
        }

        /// <summary>
        /// Unsubscribe from input action events and disable their actions.
        /// </summary>

        protected override void DeinitPlayerInput()
        {
            // Call base method implementation
            base.DeinitPlayerInput();

            combat.DeinitializeCombat();

            if (pauseInputAction != null)
            {
                pauseInputAction.started -= OnPause;
                pauseInputAction.performed -= OnPause;
                pauseInputAction.canceled -= OnPause;

                pauseInputAction.Disable();
                pauseInputAction = null;
            }

            if (readyInputAction != null)
            {
                readyInputAction.started -= OnReady;
                readyInputAction.performed -= OnReady;
                readyInputAction.canceled -= OnReady;

                readyInputAction.Disable();
                readyInputAction = null;
            }
        }

        /// <summary>
        /// Handle Player input, only if actions are assigned (eg: actions != null).
        /// </summary>

        protected override void HandleInput()
        {
            base.HandleInput();

            combat.HandleCombat();

            HandlePausing();

            HandleReadyUp();
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
            if (combat.isPunching || combat.isBlocking || combat.takingDamage)
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
        /// Check whether the pause menu is active/inactive,
        ///  and disable/enable player controls.
        /// </summary>

        private void CheckIfPauseMenuActive()
        {
            if (UIManager.Instance.PauseMenuActive())
            {
                DisableCharacter();
            }
            else if (!combat.takingDamage)
            {
                // We shouldn't re-enable movement if taking damage
                //  Instead, the StopDamage method will re-enable movement
                EnableCharacter();
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
        /// Captures ready input that the player initiates.
        /// </summary>

        private void HandleReadyUp()
        {
            if (GameManager.Instance.InLobby() && _readyButtonPressed)
            {
                _readyButtonPressed = false;

                NetworkPlayer.LocalInstance.CmdToggleReadyStatus();
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
        /// Initiate the ready up state.
        /// </summary>

        private void Ready()
        {
            _readyButtonPressed = true;
        }

        /// <summary>
        /// Release the ready button.
        /// </summary>

        private void ReleaseReady()
        {
            _readyButtonPressed = false;
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

                if (combat.isAirPunching)
                    combat.ResetAirPunch();

                if (combat.isBlocking)
                    combat.StopBlocking();

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
        /// Play the launch animation for the character.
        /// </summary>

        public void PlayLaunchAnimation()
        {
            _animancer.TryPlay("_Launch", 0.25f);

            if (_currentAnimationClip != "_Launch")
            {
                _currentAnimationClip = "_Launch";
                _networkAnimations.CmdPlayLaunchAnimation();
            }
        }

        /// <summary>
        /// Play the jump animation for the character.
        /// </summary>

        public void PlayJumpAnimation()
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

        public void PlayFallAnimation()
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

        public void PlayLandAnimation()
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

        public void PlayIdleAnimation()
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

        public void PlayRunAnimation(Vector2 movementInput)
        {
            if (movementInput.y != 0f || movementInput.x != 0f)
            {
                // Input magnitude determines how fast to animate the run animation
                // When the player is slightly tilting the control stick
                // Round to the nerest 0.1f
                float inputMagnitude = Mathf.Round(movementInput.magnitude * 10f) / 10f;
                bool shouldUpdateNetworkAnimation = false;


                if (inputMagnitude < 0.2f)
                {
                    inputMagnitude = 0.2f;
                }

                // Update the run magnitude if it has changed
                // This is used to determine if we need to update the network animation
                // To avoid sending too many network messages
                if (currentRunMagnitude != inputMagnitude)
                {
                    currentRunMagnitude = inputMagnitude;
                    shouldUpdateNetworkAnimation = true;
                }

                var state = _animancer.TryPlay("_Run", 0.25f);
                state.Speed = 1.25f * inputMagnitude;

                if (_currentAnimationClip != "_Run" || shouldUpdateNetworkAnimation)
                {
                    _currentAnimationClip = "_Run";
                    _networkAnimations.CmdPlayRunAnimation(inputMagnitude);
                }
            }
        }

        /// <summary>
        /// Play the sprint animation for the character.
        /// </summary>

        public void PlaySprintAnimation(Vector2 movementInput)
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
        /// Disables the player's character.
        /// </summary>

        public void DisableCharacter()
        {
            movementInputAction?.Disable();
            jumpInputAction?.Disable();
            combat.punchInputAction?.Disable();
            combat.blockInputAction?.Disable();
        }

        /// <summary>
        /// Enables the player's character.
        /// </summary>

        public void EnableCharacter()
        {
            movementInputAction?.Enable();
            jumpInputAction?.Enable();
            combat.punchInputAction?.Enable();
            combat.blockInputAction?.Enable();
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
        /// Reset the character's speed.
        /// </summary>

        public void ResetSpeed()
        {
            maxWalkSpeed = 3f;

            _isSpeeding = false;
        }

        /// <summary>
        /// Queue the character to launch.
        /// Happens during the next simulation step.
        /// </summary>

        public void QueueLaunch()
        {
            _queueLaunch = true;
        }

        #endregion
    }
}
