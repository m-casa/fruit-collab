using UnityEngine;
using static CameraManager;

public class ArenaProvider : MonoBehaviour
{
    [SerializeField] private ArenaArea[] _staticArenas;
    [SerializeField] private TransitionArena[] _transitionArenas;

    private void Awake()
    {
        foreach (TransitionArena transitionArena in _transitionArenas)
        {
            transitionArena.CacheInitialState();
        }

        CameraManager.Instance.InitializeArenas(_staticArenas, _transitionArenas);
    }
}
