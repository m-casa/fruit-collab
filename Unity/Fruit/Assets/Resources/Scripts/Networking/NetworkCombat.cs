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
    ///  to set the current attacker for the opponent.
    /// </summary>

    [Command]
    public void CmdSetAttackerForOponnent(NetworkIdentity opponentIdentity, NetworkIdentity attackerIdentity)
    {
        opponentIdentity.GetComponent<NetworkCombat>().SetAttacker(attackerIdentity);
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
    ///  to face the opponent in the direction of the attacker.
    /// </summary>

    [Command]
    public void CmdFaceOpponentTowardsAttacker(NetworkIdentity opponentIdentity, NetworkIdentity attackerIdentity)
    {
        opponentIdentity.GetComponent<NetworkCombat>().RpcFaceAttacker(opponentIdentity.connectionToClient, attackerIdentity);
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
    public void CmdSetBlockPoints(NetworkIdentity opponentIdentity, int blockPoints)
    {
        opponentIdentity.GetComponent<NetworkCombat>().SetBlockPoints(blockPoints);
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
    ///  to make the current opponent flinch.
    /// </summary>

    [Command]
    public void CmdFlinchTarget(NetworkIdentity opponentIdentity, string flinchClip)
    {
        opponentIdentity.GetComponent<NetworkCombat>().RpcFlinch(opponentIdentity.connectionToClient, flinchClip);
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
    ///  to knockback the current opponent.
    /// </summary>

    [Command]
    public void CmdKnockbackTarget(NetworkIdentity opponentIdentity)
    {
        opponentIdentity.GetComponent<NetworkCombat>().RpcStartKnockback(opponentIdentity.connectionToClient, 0.2f, transform.forward);
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
        CharacterHealth targetHealth = targetIdentity.GetComponent<CharacterHealth>();
        NetworkCombat targetCombat = targetIdentity.GetComponent<NetworkCombat>();

        targetHealth.ApplyTemporaryInvulnerability(stunDuration);

        if (targetCombat.currentAttacker != null)
        {
            targetCombat.ResetCurrentAttacker();
        }
    }

    #endregion
}
