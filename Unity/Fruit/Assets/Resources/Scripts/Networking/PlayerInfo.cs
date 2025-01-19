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

    void OnEnable()
    {
        FruitNetworkManager.OnClientConnected += HandleClientConnected;
    }

    /// <summary>
    /// Disable any events we are still listening to.
    /// </summary>

    void OnDisable()
    {
        FruitNetworkManager.OnClientConnected -= HandleClientConnected;
    }

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
    /// When this client's steam id is updated, the id is updated for the other clients.
    /// </summary>

    private void HandleSteamIdUpdated(ulong oldSteamId, ulong newSteamId)
    {
        CSteamID cSteamId = new CSteamID(newSteamId);

        //playerName.text = SteamFriends.GetFriendPersonaName(cSteamId);
    }

    /// <summary>
    /// What to do when successfully connected to the server.
    /// </summary>

    private void HandleClientConnected()
    {
        CameraManager.Instance.TransitionToCharacterSelect();

        CmdRequestStartSelection();
    }

    [Command]
    private void CmdRequestStartSelection()
    {
        GameManager.Instance.CmdStartSelection();
    }

    #endregion
}
