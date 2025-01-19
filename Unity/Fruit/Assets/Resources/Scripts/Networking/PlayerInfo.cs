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
        if (NetworkClient.isConnected)
        {
            NetworkConnection localPlayerConnection = NetworkClient.connection;
            // Check if the local player owns the spawned character
            if (GetComponent<NetworkIdentity>().isLocalPlayer)
            {
                Debug.Log("This PLAYER INFO is the local player's PLAYER INFO.");
            }
            else
            {
                Debug.Log("This PLAYER INFO is not the local player's PLAYER INFO.");
            }
            // Now pass the connection to the command method
            GameManager.Instance.CmdStartSelection(connectionToClient);
        }
    }

    [Command]
    private void CmdRequestStartSelection(NetworkConnectionToClient sender = null)
    {
        GameManager.Instance.CmdStartSelection(sender);
    }

    #endregion
}
