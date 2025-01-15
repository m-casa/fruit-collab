using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;

public class PlayerInfo : NetworkBehaviour
{
    [SyncVar(hook = nameof(HandleSteamIdUpdated))]
    private ulong steamId;

    [SerializeField] private TextMeshProUGUI playerName;

    public void SetSteamId(ulong steamId)
    {
        this.steamId = steamId;
    }

    private void HandleSteamIdUpdated(ulong oldSteamId, ulong newSteamId)
    {
        CSteamID cSteamId = new CSteamID(newSteamId);

        playerName.text = SteamFriends.GetFriendPersonaName(cSteamId);
    }
}
