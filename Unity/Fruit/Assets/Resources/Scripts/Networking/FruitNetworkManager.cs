using HeathenEngineering.SteamworksIntegration;
using Mirror;
using Steamworks;
using System;
using UnityEngine;

public class FruitNetworkManager : NetworkManager
{
    [SerializeField] private GameObject _gameManager;

    // Define a delegate and event to notify when the client is fully connected or disconnects
    public static event Action OnClientConnected;
    public static event Action OnClientDisconnected;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // When the host is "connected" spawn the Game Manager in for the server
        if (conn.connectionId == 0 && GameManager.Instance == null)
        {
            GameObject gameManagerInstance = Instantiate(_gameManager);
            NetworkServer.Spawn(gameManagerInstance);
        }

        // Add player
        GameObject player = Instantiate(playerPrefab);
        player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
        NetworkServer.AddPlayerForConnection(conn, player);

        PlayerInfo playerInfo = conn.identity.GetComponent<PlayerInfo>();

        if (SteamSettings.Initialized)
        {
            // Set this player's Steam ID and use numPlayers - 1 since
            //  we're grabbing an index, which starts counting at 0
            CSteamID cSteamId = SteamMatchmaking.GetLobbyMemberByIndex(
                SteamLogic.LobbyId,
                numPlayers - 1);

            // NOTE: This is only being set on the server's version of the player
            playerInfo.SetSteamId(cSteamId.m_SteamID);
        }

        playerInfo.TransitionToCharacterSelect(conn);

        Debug.Log("Player " + conn.connectionId + " added for connection.");

        // Trigger the event after the player has been added
        //OnClientConnected?.Invoke();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        // If not null, call the event
        OnClientDisconnected?.Invoke();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        Debug.Log("SERVER HAS STARTED.");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // Call base functionality (actually destroys the player)
        base.OnServerDisconnect(conn);
    }
}
