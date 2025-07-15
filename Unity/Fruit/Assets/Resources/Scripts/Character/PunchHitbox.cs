using UnityEngine;

public class PunchHitbox : MonoBehaviour
{
    private Combat _combat;

    private void Awake()
    {
        _combat = GetComponentInParent<Combat>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_combat || !_combat.isPunching || !_combat.isActiveAndEnabled) return;

        if (other.TryGetComponent(out CharacterHealth targetHealth))
        {
            _combat.TryHitTarget(targetHealth);
        }
    }
}
