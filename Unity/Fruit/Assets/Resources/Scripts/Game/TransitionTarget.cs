using UnityEngine;

public class TransitionTarget : MonoBehaviour
{
    [SerializeField] private int _transitionIndex; // For ordering

    public int transitionIndex => _transitionIndex;
}
