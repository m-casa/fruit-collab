using UnityEngine;

public class MatchSpawn : MonoBehaviour
{
    [SerializeField] private int _matchSpawnIndex; // For ordering

    public int matchSpawnIndex => _matchSpawnIndex;
}
