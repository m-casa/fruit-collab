using UnityEngine;

public class LobbySpawn : MonoBehaviour
{
    [SerializeField] private int _lobbySpawnIndex; // For ordering

    public int lobbySpawnIndex => _lobbySpawnIndex;
}
