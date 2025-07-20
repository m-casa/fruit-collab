using EasyCharacterMovement;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkCombat : NetworkBehaviour
{
    #region FIELDS

    [SerializeField] private Combat _combat;
    [SerializeField] private Collider _punchHitbox;
    [SerializeField] private LayerMask _enemyMask;
    [SerializeField] private float _cooldownDuration = 0.15f; // Cooldown duration after combo ends or fails

    private FruitCharacter _character, _currentTarget;
    private NetworkCombat _currentAttacker;
    private Queue<int> _punchQueue = new Queue<int>();
    private bool _blockButtonPressed, _isBlocking, _punchButtonPressed,
        _isGroundPunching, _isAirPunching, _takingDamage, _blockCooldown;
    private int _currentComboStep, _nextComboStep, _blockPoints = 6;
    private float _cooldownTimer, _blockRechargeTimer, _attackerResetDelay = 0.5f;
    private string[] _comboAnimations = { "_Punch.1", "_Punch.2", "_Punch.3" };

    #endregion

    #region PROPERTIES

    public Combat combat => _combat;

    public bool isBlocking => _isBlocking;

    public bool blockCooldown => _blockCooldown;

    public bool isGroundPunching => _isGroundPunching;

    public bool isAirPunching => _isAirPunching;

    public bool isPunching => _isGroundPunching || _isAirPunching;

    public NetworkCombat currentAttacker
    {
        get => _currentAttacker;
        set => _currentAttacker = value;
    }

    public int blockPoints
    {
        get => _blockPoints;
        set => _blockPoints = value;
    }

    public float blockRechargeTimer
    {
        get => _blockRechargeTimer;
        set => _blockRechargeTimer = value;
    }

    public bool takingDamage
    {
        get => _takingDamage;
        set => _takingDamage = value;
    }

    #endregion

    #region MONOBEHAVIOR

    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>

    private void Awake()
    {
        _isGroundPunching = false;

        _isAirPunching = false;

        _isBlocking = false;
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to set the ground punching value for this player's Character.
    /// </summary>

    [Command]
    public void CmdSetGroundPunching(bool val)
    {
        RpcSetGroundPunching(val);
    }

    /// <summary>
    /// Sets the ground punching value on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc]
    private void RpcSetGroundPunching(bool val)
    {
        _isGroundPunching = val;
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to set the air punching value for this player's Character.
    /// </summary>

    [Command]
    public void CmdSetAirPunching(bool val)
    {
        RpcSetAirPunching(val);
    }

    /// <summary>
    /// Sets the air punching value on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc]
    private void RpcSetAirPunching(bool val)
    {
        _isAirPunching = val;
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to set the blocking value for this player's Character.
    /// </summary>

    [Command]
    public void CmdSetBlocking(bool val)
    {
        RpcSetBlocking(val);
    }

    /// <summary>
    /// Sets the blocking value on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc]
    private void RpcSetBlocking(bool val)
    {
        _isBlocking = val;
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to set the current attacker for this player's Character.
    /// </summary>

    [Command]
    public void CmdSetCurrentAttacker(NetworkIdentity attackerIdentity)
    {
        RpcSetCurrentAttacker(attackerIdentity);
    }

    /// <summary>
    /// Sets the current attacker on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc]
    private void RpcSetCurrentAttacker(NetworkIdentity attackerIdentity)
    {
        _currentAttacker = attackerIdentity.GetComponent<NetworkCombat>();
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to set the block points left for this player's Character.
    /// </summary>

    [Command]
    public void CmdSetBlockPoints(int blockPoints)
    {
        RpcSetBlockPoints(blockPoints);
    }

    /// <summary>
    /// Sets the current block points left on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc]
    private void RpcSetBlockPoints(int blockPoints)
    {
        _blockPoints = blockPoints;
    }

    /// <summary>
    /// Set the character's block on cooldown until fully recharged.
    /// </summary>

    [Command]
    public void CmdStartBlockCooldownRoutine()
    {
        StartCoroutine(RpcStartBlockCooldownRoutine());
    }

    /// <summary>
    /// Set the character's block on cooldown until fully recharged.
    /// </summary>

    public IEnumerator RpcStartBlockCooldownRoutine()
    {
        _blockCooldown = true;
        _blockPoints = 0;
        RpcSetBlockPoints(_blockPoints);

        yield return new WaitForSeconds(3f);
        _blockPoints++;
        RpcSetBlockPoints(_blockPoints);

        while (_blockPoints < 6)
        {
            yield return new WaitForSeconds(1f);
            _blockPoints++;
            RpcSetBlockPoints(_blockPoints);
        }

        _blockCooldown = false;
    }

    /// <summary>
    /// Face the direction of the current attacker.
    /// </summary>

    [Command]
    public void FaceAttacker(NetworkIdentity attackerIdentity)
    {
        if (attackerIdentity == null) return;

        Transform attacker = attackerIdentity.transform; // Get transform server-side
        Vector3 lookDir = (attacker.position - transform.position).normalized;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
            transform.forward = lookDir;
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

            combat.ResetCombo();
            combat.ResetAirPunch();

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
