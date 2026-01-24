using UnityEngine;

public class ArenaArea : MonoBehaviour
{
    [Header("Boundary")]
    [SerializeField] private Vector2 _mainBoundary = new Vector2(4f, 3f);

    [Header("Boundary Settings")]
    [SerializeField] private float _softBoundary = 1.5f; // How far outside you can go
    [SerializeField] private float _pullForceStrength = 25f; // How strong the pullback is
    [SerializeField] private float _velocityDamping = 8f; // Prevents oscillation

    [SerializeField] private bool _isTransitionArena;
    [SerializeField] private int _arenaIndex;

    #region PROPERTIES

    public Vector2 mainBoundary => _mainBoundary;
    public float softBoundary => _softBoundary;
    public float pullForceStrength => _pullForceStrength;
    public float velocityDamping => _velocityDamping;
    public bool isTransitionArena => _isTransitionArena;
    public int arenaIndex => _arenaIndex;

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;

        // Main arena boundary
        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(_mainBoundary.x * 2f, 0.1f, _mainBoundary.y * 2f)
        );

        // Soft boundary (how far outside you can go)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange
        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(
                (_mainBoundary.x + _softBoundary) * 2f,
                0.1f,
                (_mainBoundary.y + _softBoundary) * 2f
            )
        );
    }
#endif
}
