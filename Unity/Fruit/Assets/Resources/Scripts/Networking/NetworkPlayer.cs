using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Represents the player’s networked state and actions.</summary>
public class NetworkPlayer : NetworkBehaviour
{
    #region FIELDS

    public static NetworkPlayer LocalInstance { get; private set; }

    [SyncVar(hook = nameof(HandleSteamIdUpdated))]
    private ulong _steamId;

    [SyncVar]
    private NetworkIdentity _characterIdentity;

    [SerializeField] private TextMeshProUGUI _playerName;

    //private GameObject _playerCharacter;

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
    /// Sets the steam id for this client.
    /// </summary>

    public void SetSteamId(ulong steamId)
    {
        _steamId = steamId;
    }

    /// <summary>
    /// Sets the character for this client.
    /// </summary>

    public void SetCharacter(GameObject character)
    {
        //_playerCharacter = character;
        _characterIdentity = character.GetComponent<NetworkIdentity>();
    }

    /// <summary>
    /// Moves the target client's camera to the character selection area.
    /// Then sends a request to the server to start the character selection process.
    /// </summary>

    [TargetRpc]
    public void RpcTransitionToCharacterSelect(NetworkConnection conn)
    {
        CameraManager.Instance.MoveCameraToCharacterSelect();

        UIManager.Instance.SetMainMenuStatus(false);

        CmdRequestSelectionStart();
    }

    /// <summary>
    /// Teleports the target client's character into the match.
    /// </summary>

    [TargetRpc]
    public void RpcTeleportCharacter(NetworkConnection conn, Vector3 pos, Quaternion rot)
    {
        //if (_playerCharacter != null)
        //    _playerCharacter.transform.SetPositionAndRotation(pos, rot);

        if (_characterIdentity != null)
        {
            _characterIdentity.transform.SetPositionAndRotation(pos, rot);
        }
        else
        {
            Debug.LogWarning("No character identity assigned on client.");
        }
    }

    /// <summary>
    /// Begins the character selection process on the server 
    ///  for the client that requested it.
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
