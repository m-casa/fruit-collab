using EasyCharacterMovement;
using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class NetworkCombat : NetworkBehaviour
{
    #region FIELDS

    [SyncVar] private NetworkCombat _currentAttacker;
    [SyncVar] private int _blockPoints = 6;
    [SyncVar] private bool _isBlocking = false;
    [SyncVar] private bool _blockCooldown = false;

    [SerializeField] private Combat _combat;

    #endregion

    #region PROPERTIES

    public NetworkCombat currentAttacker => _currentAttacker;

    public int blockPoints => _blockPoints;

    public bool isBlocking => _isBlocking;

    public bool blockCooldown => _blockCooldown;

    #endregion

    #region METHODS
    
    /// <summary>
    /// Sends a command to the server, telling it
    ///  to set the current attacker for the target.
    /// </summary>

    [Command]
    public void CmdSetAttacker(NetworkIdentity targetIdentity, NetworkIdentity attackerIdentity)
    {
        NetworkCombat target = targetIdentity.GetComponent<NetworkCombat>();

        target.SetAttacker(attackerIdentity);
    }

    /// <summary>
    /// Set the current attacker for this player's Character.
    /// </summary>

    public void SetAttacker(NetworkIdentity attackerIdentity)
    {
        _currentAttacker = attackerIdentity.GetComponent<NetworkCombat>();
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to face the target in the direction of the attacker.
    /// </summary>

    [Command]
    public void CmdFaceAttacker(NetworkIdentity targetIdentity, NetworkIdentity attackerIdentity)
    {
        NetworkCombat target = targetIdentity.GetComponent<NetworkCombat>();

        target.RpcFaceAttacker(targetIdentity.connectionToClient, attackerIdentity);
    }

    /// <summary>
    /// Face the direction of the current attacker on
    ///  the original client's version of this Character.
    /// </summary>

    [TargetRpc]
    public void RpcFaceAttacker(NetworkConnection conn, NetworkIdentity attackerIdentity)
    {
        if (attackerIdentity == null) return;

        Transform attacker = attackerIdentity.transform; // Get transform server-side
        Vector3 lookDir = (attacker.position - transform.position).normalized;
        lookDir.y = 0f;

        if (lookDir != Vector3.zero)
        {
            GetComponent<FruitCharacter>().previousMovementDirection = lookDir;
            transform.forward = lookDir;
        }
    }

    /// <summary>
    /// Reset the current attacker for this player's Character.
    /// </summary>

    public void ResetCurrentAttacker()
    {
        _currentAttacker = null;
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to set the blocking value for this player's Character.
    /// </summary>

    [Command]
    public void CmdSetBlocking(bool val)
    {
        _isBlocking = val;
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to set the block points left for this player's Character.
    /// </summary>

    [Command]
    public void CmdSetBlockPoints(NetworkIdentity targetIdentity, int blockPoints)
    {
        NetworkCombat target = targetIdentity.GetComponent<NetworkCombat>();

        target.SetBlockPoints(blockPoints);
    }

    /// <summary>
    /// Set the block points left for this player's Character.
    /// </summary>

    public void SetBlockPoints(int blockPoints)
    {
        _blockPoints = blockPoints;

        if (_blockPoints <= 0)
        {
            StartCoroutine(RpcStartBlockCooldownRoutine());
        }
    }

    /// <summary>
    /// Set the character's block on cooldown until fully recharged.
    /// </summary>

    public IEnumerator RpcStartBlockCooldownRoutine()
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
    /// Sends a command to the server, telling it
    ///  to make the current target flinch.
    /// </summary>

    [Command]
    public void CmdFlinchTarget(NetworkIdentity targetIdentity, string flinchClip)
    {
        NetworkCombat target = targetIdentity.GetComponent<NetworkCombat>();

        target.RpcFlinch(targetIdentity.connectionToClient, flinchClip);
    }

    /// <summary>
    /// Make the character flinch on
    ///  the original client's version of this Character.
    /// </summary>

    [TargetRpc]
    public void RpcFlinch(NetworkConnection conn, string flinchClip)
    {
        _combat.Flinch(flinchClip);
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to knockback the current target.
    /// </summary>

    [Command]
    public void CmdKnockbackTarget(NetworkIdentity targetIdentity)
    {
        NetworkCharacterHealth targetHealth = targetIdentity.GetComponent<NetworkCharacterHealth>();

        // Only knockback if they are alive
        if (!targetHealth.healthDepleted)
        {
            NetworkCombat target = targetIdentity.GetComponent<NetworkCombat>();

            target.RpcStartKnockback(targetIdentity.connectionToClient, 0.2f, transform.forward);
        }
    }

    /// <summary>
    /// Damage the character and push them back on
    ///  the original client's version of this Character.
    /// </summary>

    [TargetRpc]
    public void RpcStartKnockback(NetworkConnection conn, float effectDuration, Vector3 direction)
    {
        _combat.StartKnockback(effectDuration, direction);
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to apply invulnerability to the current target.
    /// </summary>

    [Command]
    public void CmdDisengageTarget(NetworkIdentity targetIdentity, float stunDuration)
    {
        NetworkCombat target = targetIdentity.GetComponent<NetworkCombat>();
        NetworkCharacterHealth targetHealth = targetIdentity.GetComponent<NetworkCharacterHealth>();

        target.RpcStopFlinch(targetIdentity.connectionToClient);

        if (target.currentAttacker != null)
            target.ResetCurrentAttacker();

        if (!targetHealth.healthDepleted)
            targetHealth.ApplyTemporaryInvulnerability(stunDuration);
    }

    /// <summary>
    /// Stop the character from flinching on
    ///  the original client's version of this Character.
    /// </summary>

    [TargetRpc]
    public void RpcStopFlinch(NetworkConnection conn)
    {
        _combat.StopFlinch();
    }

    #endregion
}
