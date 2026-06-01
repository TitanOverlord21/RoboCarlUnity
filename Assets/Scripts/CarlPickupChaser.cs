using UnityEngine;

[RequireComponent(typeof(CarlLocomotion))]
[RequireComponent(typeof(CarlResources))]
[DefaultExecutionOrder(0)]
public class CarlPickupChaser : MonoBehaviour
{
    CarlLocomotion _locomotion;
    CarlResources _resources;

    void Awake()
    {
        _locomotion = GetComponent<CarlLocomotion>();
        _resources = GetComponent<CarlResources>();
    }

    void FixedUpdate()
    {
        if (_resources.IsGameOver)
            return;

        _locomotion.RefreshGrounded();
        if (!_locomotion.IsGrounded)
            return;

        if (!TrySelectTargetPickup(out var pickup))
            return;

        float deltaX = pickup.transform.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) < 0.001f)
            return;

        _locomotion.WalkHorizontally(Mathf.Sign(deltaX));
    }

    PickupType GetLowestResourceType()
    {
        if (_resources.Energy < _resources.Oil)
            return PickupType.Energy;

        if (_resources.Oil < _resources.Energy)
            return PickupType.Oil;

        return PickupType.Oil;
    }

    bool TrySelectTargetPickup(out ResourcePickup pickup)
    {
        pickup = null;
        var preferredType = GetLowestResourceType();

        if (TryFindPickupInScene(preferredType, out pickup))
            return true;

        var fallbackType = preferredType == PickupType.Oil ? PickupType.Energy : PickupType.Oil;
        return TryFindPickupInScene(fallbackType, out pickup);
    }

    static bool TryFindPickupInScene(PickupType type, out ResourcePickup pickup)
    {
        pickup = null;

        foreach (var candidate in ResourcePickup.ActivePickups)
        {
            if (candidate == null || !candidate.isActiveAndEnabled || candidate.Type != type)
                continue;

            pickup = candidate;
            return true;
        }

        return false;
    }
}
