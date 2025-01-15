using HeathenEngineering.SteamworksIntegration;
using Mirror;
using Steamworks;
using System;
using UnityEngine;

public class SurviveNetworkManager : NetworkManager
{
    public static event Action ClientDisconnected;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // Add player
        GameObject player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);

        if (SteamSettings.Initialized)
        {
            // Set this player's Steam ID and use numPlayers - 1 since
            //  we're grabbing an index, which starts counting at 0
            CSteamID cSteamId = SteamMatchmaking.GetLobbyMemberByIndex(
                SteamLogic.LobbyId,
                numPlayers - 1);

            PlayerInfo playerInfo = conn.identity.GetComponent<PlayerInfo>();

            // NOTE: This is only being set on the server's version of the player
            playerInfo.SetSteamId(cSteamId.m_SteamID);
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // Call base functionality (actually destroys the player)
        base.OnServerDisconnect(conn);
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        // If not null, call the event
        ClientDisconnected?.Invoke();
    }
}
