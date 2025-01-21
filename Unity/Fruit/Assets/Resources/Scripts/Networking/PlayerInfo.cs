using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;

public class PlayerInfo : NetworkBehaviour
{
    #region FIELDS

    [SyncVar(hook = nameof(HandleSteamIdUpdated))]
    private ulong steamId;

    [SerializeField] private TextMeshProUGUI playerName;

    #endregion

    #region MONOBEHAVIOR

    /// <summary>
    /// Enable any events we should listen to.
    /// </summary>

    //void OnEnable()
    //{
    //    FruitNetworkManager.OnClientConnected += TransitionToCharacterSelect;
    //}

    /// <summary>
    /// Disable any events we are still listening to.
    /// </summary>

    //void OnDisable()
    //{
    //    FruitNetworkManager.OnClientConnected -= TransitionToCharacterSelect;
    //}

    #endregion

    #region METHODS

    /// <summary>
    /// Sets the steam id for this client.
    /// </summary>

    public void SetSteamId(ulong steamId)
    {
        this.steamId = steamId;
    }

    /// <summary>
    /// What to do when successfully connected to the server.
    /// </summary>
    
    [TargetRpc]
    public void TransitionToCharacterSelect(NetworkConnection conn)
    {
        CameraManager.Instance.MoveCameraToCharacterSelect();

        CmdStartSelection();
    }

    [Command]
    public void CmdRequestCharacter(string selectedCharacter)
    {
        GameManager.Instance.ClaimCharacter(selectedCharacter, connectionToClient);
    }

    [Command]
    private void CmdStartSelection()
    {
        GameManager.Instance.StartSelection(connectionToClient);
    }

    /// <summary>
    /// When this client's steam id is updated, the id is updated for the other clients.
    /// </summary>

    private void HandleSteamIdUpdated(ulong oldSteamId, ulong newSteamId)
    {
        CSteamID cSteamId = new CSteamID(newSteamId);

        //playerName.text = SteamFriends.GetFriendPersonaName(cSteamId);
    }

    #endregion
}
