using Animancer;
using EasyCharacterMovement;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Combat : NetworkBehaviour
{
    #region FIELDS

    [SerializeField] private Collider _punchHitbox;
    [SerializeField] private LayerMask _enemyMask;
    [SerializeField] private float _cooldownDuration = 0.15f; // Cooldown duration after combo ends or fails

    private FruitCharacter _character, _currentTarget;
    private Combat _currentAttacker;
    private Queue<int> _punchQueue = new Queue<int>();
    private bool _blockButtonPressed, _isBlocking, _punchButtonPressed, 
        _isGroundPunching, _isAirPunching, _takingDamage, _blockCooldown;
    private int _currentComboStep, _nextComboStep, _blockPoints = 6;
    private float _cooldownTimer, _blockRechargeTimer, _attackerResetDelay = 0.5f;
    private string[] _comboAnimations = { "_Punch.1", "_Punch.2", "_Punch.3" };

    #endregion

    #region PROPERTIES

    public bool isBlocking => _isBlocking;

    public bool isPunching => _isGroundPunching || _isAirPunching;

    public bool isAirPunching => _isAirPunching;

    public bool takingDamage
    {
        get => _takingDamage;
        set => _takingDamage = value;
    }

    #endregion

    #region INPUT ACTIONS

    /// <summary>
    /// Punch InputAction.
    /// </summary>

    public InputAction punchInputAction { get; set; }

    /// <summary>
    /// Block InputAction.
    /// </summary>

    public InputAction blockInputAction { get; set; }

    #endregion

    #region INPUT ACTION HANDLERS

    /// <summary>
    /// Punch input action handler.
    /// </summary>

    private void OnPunch(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
            Punch();
        else if (context.canceled)
            ReleasePunch();
    }

    /// <summary>
    /// Block input action handler.
    /// </summary>

    private void OnBlock(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
            Block();
        else if (context.canceled)
            StopBlocking();
    }

    #endregion

    #region MONOBEHAVIOR

    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>

    private void Awake()
    {
        _character = GetComponent<FruitCharacter>();

        _punchButtonPressed = false;
        _isGroundPunching = false;
        _isAirPunching = false;

        _blockButtonPressed = false;
        _isBlocking = false;

        _currentComboStep = 0;
        _nextComboStep = 0;
        _cooldownTimer = 0f;
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Captures any punches the player initiates.
    /// </summary>

    private void HandlePunching()
    {
        if (_character.IsGrounded())
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
    /// Handles punch logic while the character is in the air.
    /// </summary>

    private void HandleAirPunch()
    {
        if (_punchButtonPressed)
        {
            if (!_isAirPunching)
            {
                string airPunchClip = _character.rightFootUp ? "_AirPunch.R" : "_AirPunch.L";

                _character.SetRotationMode(RotationMode.None);

                if (_character.animancer.States.TryGet(airPunchClip, out var state))
                {
                    _character.animancer.Play(state);
                    state.Speed = 1.25f;
                    state.Time = 0f;

                    if (_character.currentAnimationClip != airPunchClip)
                    {
                        _character.currentAnimationClip = airPunchClip;
                        _character.networkAnimations.CmdPlayAirPunchAnimation(_character.currentAnimationClip);
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
    /// Captures block input that the player initiates.
    /// </summary>

    private void HandleBlocking()
    {
        if (_character.IsGrounded() && _blockButtonPressed && !_blockCooldown)
        {
            if (!isPunching)
            {
                _isBlocking = true;
                _blockRechargeTimer = 0f;

                punchInputAction?.Disable();
                _character.movementInputAction?.Disable();
                _character.jumpInputAction?.Disable();

                _character.animancer.TryPlay("_Block", 0.15f);

                if (_character.currentAnimationClip != "_Block")
                {
                    _character.currentAnimationClip = "_Block";
                    _character.networkAnimations.CmdPlayBlockAnimation();
                }
            }
        }

        if (!_blockButtonPressed && !_blockCooldown && _blockPoints < 6)
        {
            _blockRechargeTimer += Time.deltaTime;

            if (_blockRechargeTimer >= 3f && _blockPoints < 6f)
            {
                _blockPoints++;
            }
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
        _character.movementInputAction?.Disable();
        _character.jumpInputAction?.Disable();

        // Get the next punch index from the queue
        _currentComboStep = _punchQueue.Dequeue();

        // Attempt to punch an opponent
        CmdEnablePunchHitbox();

        // Play and adjust the animation state
        if (_character.animancer.States.TryGet(_comboAnimations[_currentComboStep], out var state))
        {
            _character.animancer.Play(state);
            state.Speed = 1.25f;
            state.Time = 0f;

            if (_character.currentAnimationClip != _comboAnimations[_currentComboStep])
            {
                _character.currentAnimationClip = _comboAnimations[_currentComboStep];
                _character.networkAnimations.CmdPlayPunchAnimation(_character.currentAnimationClip);
            }
        }

        // Mark as punching
        _isGroundPunching = true;

        // Apply a slight forward push for each punch
        _character.LaunchCharacter(transform.forward * 1.5f);

        // Schedule the transition back to idle after the animation
        state.Events.OnEnd = () =>
        {
            CmdDisablePunchHitbox();

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
    /// Face our character in the direction of the target we're attacking.
    /// </summary>

    private void FaceClosestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 8f, _enemyMask);
        if (hits.Length == 0) return;

        Vector3 preferredDir = _character.GetMovementDirection();
        if (preferredDir == Vector3.zero)
            preferredDir = transform.forward;

        float bestDot = -1f;
        Transform bestTarget = null;

        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;
            Vector3 toTarget = (hit.transform.position - transform.position).normalized;
            float dot = Vector3.Dot(preferredDir, toTarget);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestTarget = hit.transform;
            }
        }

        if (bestTarget != null)
        {
            Vector3 lookDir = (bestTarget.position - transform.position).normalized;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
                transform.forward = lookDir;
        }
    }

    /// <summary>
    /// Enables the punch hitbox.
    /// </summary>

    [Command]
    private void CmdEnablePunchHitbox()
    {
        if (_punchHitbox != null)
            _punchHitbox.enabled = true;
    }

    /// <summary>
    /// Disables the punch hitbox.
    /// </summary>

    [Command]
    private void CmdDisablePunchHitbox()
    {
        if (_punchHitbox != null)
            _punchHitbox.enabled = false;
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
        if (_character.IsGrounded())
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
        if (_character.IsGrounded())
            _character.PlayIdleAnimation();

        // Now that our combo has ended, apply invulnerability/knockback to the current target
        if (_currentTarget != null)
        {
            CharacterHealth targetHealth = _currentTarget.GetComponent<CharacterHealth>();
            Combat targetCombat = _currentTarget.GetComponent<Combat>();
            NetworkIdentity targetIdentity = _currentTarget.GetComponent<NetworkIdentity>();
            int punchesLanded = _currentComboStep + 1;
            float stunDuration = punchesLanded == 1 ? 1f : punchesLanded == 2 ? 2f : 2.5f;

            targetHealth.ApplyTemporaryInvulnerability(stunDuration);
            targetCombat.RpcStartKnockback(targetIdentity.connectionToClient, 0.2f, transform.forward);
            
            if (targetCombat._currentAttacker != null) 
            {
                targetCombat._currentAttacker = null;
            }

            _currentTarget = null;
        }

        // Reset combo state
        _isGroundPunching = false;
        _currentComboStep = 0;

        // Clear the queue
        _punchQueue.Clear();

        // Only re-enable movement if the player isn't paused
        if (!UIManager.Instance.PauseMenuActive())
        {
            _character.movementInputAction?.Enable();
            _character.jumpInputAction?.Enable();
        }
    }

    /// <summary>
    /// Starts a cooldown to avoid punch spamming.
    /// </summary>

    private void StartCooldown() => _cooldownTimer = _cooldownDuration;

    /// <summary>
    /// Initialize combat InputActions.
    /// </summary>

    public void InitializeCombat()
    {
        // Setup Punch input action handlers
        punchInputAction = _character.inputActions.FindAction("Punch");
        if (punchInputAction != null)
        {
            punchInputAction.started += OnPunch;
            punchInputAction.performed += OnPunch;
            punchInputAction.canceled += OnPunch;

            punchInputAction.Enable();
        }

        // Setup Block input action handlers
        blockInputAction = _character.inputActions.FindAction("Block");
        if (blockInputAction != null)
        {
            blockInputAction.started += OnBlock;
            blockInputAction.performed += OnBlock;
            blockInputAction.canceled += OnBlock;

            blockInputAction.Enable();
        }
    }

    /// <summary>
    /// Unsubscribe from combat input action events and disable their actions.
    /// </summary>

    public void DeinitializeCombat()
    {
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
    }

    /// <summary>
    /// Start a punch initiated by the player.
    /// </summary>

    public void Punch()
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

    public void ReleasePunch()
    {
        _punchButtonPressed = false;
    }

    /// <summary>
    /// Reset the character's air punch to a default state.
    /// </summary

    public void ResetAirPunch()
    {
        _character.PlayFallAnimation();

        _isAirPunching = false;

        _character.SetRotationMode(RotationMode.OrientToMovement);
    }

    /// <summary>
    /// Start blocking, initiated by the player.
    /// </summary>

    public void Block()
    {
        _blockButtonPressed = true;
    }

    /// <summary>
    /// Manually releases the block button,
    ///  stopping the player from blocking.
    /// </summary>

    public void StopBlocking()
    {
        _blockButtonPressed = false;

        if (_isBlocking)
        {
            _isBlocking = false;

            // Only re-enable movement if the player isn't paused
            if (!UIManager.Instance.PauseMenuActive())
            {
                punchInputAction?.Enable();
                _character.movementInputAction?.Enable();
                _character.jumpInputAction?.Enable();
            }
        }
    }

    /// <summary>
    /// Captures any combat the player initiates.
    /// </summary>

    public void HandleCombat()
    {
        HandlePunching();

        HandleBlocking();
    }

    /// <summary>
    /// Attempt to damage the target we are punching.
    /// </summary>

    public void TryHitTarget(FruitCharacter target)
    {
        // Make sure we're not hitting ourself
        if (target == null || target == _character.GetComponent<CharacterHealth>()) return;

        CharacterHealth targetHealth = target.GetComponent<CharacterHealth>();

        if (targetHealth.isInvulnerable) return;

        Combat targetCombat = target.GetComponent<Combat>();

        // If we're in the middle of a combo, make sure we don't accidentally hit someone else
        if (_currentTarget != null && targetCombat._currentAttacker != this) return;

        // Blocking
        if (targetCombat.isBlocking)
        {
            if (targetCombat._currentAttacker == null || targetCombat._currentAttacker == this)
            {
                if (targetCombat._blockPoints > 0 && !targetCombat._blockCooldown)
                {
                    targetCombat.FaceAttacker(transform);
                    targetCombat._blockPoints--;
                    targetCombat._currentAttacker = this;
                    targetCombat._blockRechargeTimer = 0f;

                    if (targetCombat._blockPoints <= 0)
                    {
                        targetCombat.StartCoroutine(targetCombat.BlockCooldownRoutine());
                    }
                }
            }
            return;
        }

        // Damage
        targetHealth.TakeDamage(1);
        targetCombat._currentAttacker = this;
        _currentTarget = target;
    }

    /// <summary>
    /// Face the direction of the current attacker.
    /// </summary>

    public void FaceAttacker(Transform attacker)
    {
        Vector3 lookDir = (attacker.position - transform.position).normalized;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
            transform.forward = lookDir;
    }

    /// <summary>
    /// Set the character's block on cooldown until fully recharged.
    /// </summary>

    private IEnumerator BlockCooldownRoutine()
    {
        _blockCooldown = true;
        _blockPoints = 0;

        yield return new WaitForSeconds(3f);
        _blockPoints++;

        while (_blockPoints < 6)
        {
            yield return new WaitForSeconds(1f);
            _blockPoints++;
        }

        _blockCooldown = false;
    }

    /// <summary>
    /// Damage the character and push them back.
    /// </summary>

    [TargetRpc]
    public void RpcStartKnockback(NetworkConnection conn, float effectDuration, Vector3 direction)
    {
        if (!takingDamage)
        {
            takingDamage = true;

            ResetCombo();
            ResetAirPunch();

            _character.DisableCharacter();

            _character.SetVelocity(Vector3.zero);

            _character.PauseGroundConstraint();

            // Push the player back
            _character.LaunchCharacter((direction * 5f) + (_character.GetUpVector() * 2.5f), true);

            if (_character.animancer.States.TryGet("_Hurt", out var state))
            {
                _character.animancer.Play(state);
                state.Time = 0f;

                if (_character.currentAnimationClip != "_Hurt")
                {
                    _character.currentAnimationClip = "_Hurt";
                    _character.networkAnimations.CmdPlayHurtAnimation();
                }
            }

            Invoke(nameof(StopKnockback), effectDuration);
        }
    }

    /// <summary>
    /// Take the character out of the damage state.
    /// </summary>

    public void StopKnockback()
    {
        takingDamage = false;

        // Only re-enable movement if the player isn't paused
        if (!UIManager.Instance.PauseMenuActive())
        {
            _character.EnableCharacter();
        }
    }

    #endregion
}
