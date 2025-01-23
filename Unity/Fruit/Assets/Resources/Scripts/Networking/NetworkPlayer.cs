using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;

/// <summary>Represents the player’s networked state and actions.</summary>
public class NetworkPlayer : NetworkBehaviour
{
    #region FIELDS

    public static NetworkPlayer LocalInstance { get; private set; }

    [SyncVar(hook = nameof(HandleSteamIdUpdated))]
    private ulong _steamId;

    [SerializeField] private TextMeshProUGUI _playerName;

    private bool _inGame;

    #endregion

    #region MONOBEHAVIOR

    /// <summary>
    /// Like Start(), but only called on client and host for the local player object.
    /// </summary>

    public override void OnStartLocalPlayer()
    {
        // Only the local player sets this reference
        if (isLocalPlayer)
            LocalInstance = this;
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Returns true or false for whether or not the player is in game.
    /// </summary>

    public bool InGame()
    {
        return _inGame;
    }

    /// <summary>
    /// Sets the steam id for this client.
    /// </summary>

    public void SetSteamId(ulong steamId)
    {
        _steamId = steamId;
    }

    /// <summary>
    /// Moves the player's camera to the character selection area.
    /// Then sends a request to the server to start the character selection process.
    /// </summary>

    [TargetRpc]
    public void RpcTransitionToCharacterSelect(NetworkConnection conn)
    {
        CameraManager.Instance.MoveCameraToCharacterSelect();

        _inGame = true;

        CmdRequestSelectionStart();
    }

    /// <summary>
    /// Begins the character selection process on the server 
    ///  for the player that requested it.
    /// </summary>

    [Command]
    private void CmdRequestSelectionStart()
    {
        GameManager.Instance.StartSelection(connectionToClient);
    }

    /// <summary>
    /// Attempts to claim the requested character if available.
    /// </summary>

    [Command]
    public void CmdRequestCharacter(string selectedCharacter)
    {
        GameManager.Instance.ClaimCharacter(selectedCharacter, connectionToClient);
    }

    /// <summary>
    /// When this client's steam id is updated, the id is updated for the other clients.
    /// </summary>

    private void HandleSteamIdUpdated(ulong oldSteamId, ulong newSteamId)
    {
        CSteamID cSteamId = new CSteamID(newSteamId);

        //_playerName.text = SteamFriends.GetFriendPersonaName(cSteamId);
    }

    #endregion
}
