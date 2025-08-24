using Animancer;
using Mirror;
using UnityEngine;

public class NetworkAnimations : NetworkBehaviour
{
    [SerializeField] private NamedAnimancerComponent _animancer;

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to play the idle animation for this player's Character.
    /// </summary>

    [Command]
    public void CmdPlayIdleAnimation()
    {
        RpcPlayIdleAnimation();
    }

    /// <summary>
    /// Plays the idle animation on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc(includeOwner = false)]
    private void RpcPlayIdleAnimation()
    {
        _animancer.TryPlay("_Idle", 0.15f);
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to play the run animation for this player's Character.
    /// </summary>

    [Command]
    public void CmdPlayRunAnimation(float inputMagnitude)
    {
        RpcPlayRunAnimation(inputMagnitude);
    }

    /// <summary>
    /// Plays the run animation on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc(includeOwner = false)]
    private void RpcPlayRunAnimation(float inputMagnitude)
    {
        var state = _animancer.TryPlay("_Run", 0.25f);
        state.Speed = 1.25f * inputMagnitude;
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to play the sprint animation for this player's Character.
    /// </summary>

    [Command]
    public void CmdPlaySprintAnimation(float inputMagnitude)
    {
        RpcPlaySprintAnimation(inputMagnitude);
    }

    /// <summary>
    /// Plays the sprint animation on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc(includeOwner = false)]
    private void RpcPlaySprintAnimation(float inputMagnitude)
    {
        var state = _animancer.TryPlay("_Sprint", 0.25f);
        state.Speed = 1.25f * inputMagnitude;
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to play the jump animation for this player's Character.
    /// </summary>

    [Command]
    public void CmdPlayJumpAnimation(string animationClip)
    {
        RpcPlayJumpAnimation(animationClip);
    }

    /// <summary>
    /// Plays the jump animation on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc(includeOwner = false)]
    private void RpcPlayJumpAnimation(string animationClip)
    {
        var state = _animancer.TryPlay(animationClip, 0.25f);
        state.Time = 0f;
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to play the launch animation for this player's Character.
    /// </summary>

    [Command]
    public void CmdPlayLaunchAnimation()
    {
        RpcPlayLaunchAnimation();
    }

    /// <summary>
    /// Plays the launch animation on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc(includeOwner = false)]
    private void RpcPlayLaunchAnimation()
    {
        _animancer.TryPlay("_Launch", 0.25f);
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to play the fall animation for this player's Character.
    /// </summary>

    [Command]
    public void CmdPlayFallAnimation(string animationClip)
    {
        RpcPlayFallAnimation(animationClip);
    }

    /// <summary>
    /// Plays the fall animation on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc(includeOwner = false)]
    private void RpcPlayFallAnimation(string animationClip)
    {
        var state = _animancer.TryPlay(animationClip, 0.25f);
        state.Time = 0f;
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to play the land animation for this player's Character.
    /// </summary>

    [Command]
    public void CmdPlayLandAnimation()
    {
        RpcPlayLandAnimation();
    }

    /// <summary>
    /// Plays the land animation on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc(includeOwner = false)]
    private void RpcPlayLandAnimation()
    {
        _animancer.TryPlay("_Land", 0.25f);
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to play the punch animation for this player's Character.
    /// </summary>

    [Command]
    public void CmdPlayPunchAnimation(string animationClip)
    {
        RpcPlayPunchAnimation(animationClip);
    }

    /// <summary>
    /// Plays the punch animation on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc(includeOwner = false)]
    private void RpcPlayPunchAnimation(string animationClip)
    {
        var state = _animancer.TryPlay(animationClip);
        state.Speed = 1.25f;
        state.Time = 0f;
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to play the air punch animation for this player's Character.
    /// </summary>

    [Command]
    public void CmdPlayAirPunchAnimation(string animationClip)
    {
        RpcPlayAirPunchAnimation(animationClip);
    }

    /// <summary>
    /// Plays the air punch animation on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc(includeOwner = false)]
    private void RpcPlayAirPunchAnimation(string animationClip)
    {
        var state = _animancer.TryPlay(animationClip);
        state.Speed = 1.25f;
        state.Time = 0f;
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to play the hurt animation for this player's Character.
    /// </summary>

    [Command]
    public void CmdPlayKnockbackAnimation()
    {
        RpcPlayKnockbackAnimation();
    }

    /// <summary>
    /// Plays the hurt animation on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc(includeOwner = false)]
    private void RpcPlayKnockbackAnimation()
    {
        var state = _animancer.TryPlay("_Knockback");
        state.Time = 0f;
    }

    /// <summary>
    /// Sends a command to the server, telling it
    ///  to play the block animation for this player's Character.
    /// </summary>

    [Command]
    public void CmdPlayBlockAnimation()
    {
        RpcPlayBlockAnimation();
    }

    /// <summary>
    /// Plays the block animation on
    ///  every client's version of this player's Character.
    /// </summary>

    [ClientRpc(includeOwner = false)]
    private void RpcPlayBlockAnimation()
    {
        _animancer.TryPlay("_Block", 0.15f);
    }
}
