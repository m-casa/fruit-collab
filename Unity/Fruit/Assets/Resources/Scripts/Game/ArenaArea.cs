using UnityEngine;

public class ArenaArea : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] private Vector2 _halfExtents = new Vector2(4f, 3f);
    [SerializeField] private float _softPadding = 0.75f;

    [Header("Pull Force")]
    [SerializeField] private float _pullStrength = 18f;
    [SerializeField] private float _maxPullStrength = 35f;

    #region PROPERTIES

    public Vector2 halfExtents => _halfExtents;
    public float softPadding => _softPadding;
    public float pullStrength => _pullStrength;
    public float maxPullStrength => _maxPullStrength;

    #endregion


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(_halfExtents.x * 2f, 0.1f, _halfExtents.y * 2f)
        );

        // Soft boundary
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(
                (_halfExtents.x - _softPadding) * 2f,
                0.1f,
                (_halfExtents.y - _softPadding) * 2f
            )
        );
    }
#endif
}
