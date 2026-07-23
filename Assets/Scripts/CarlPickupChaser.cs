using UnityEngine;

[RequireComponent(typeof(CarlLocomotion))]
[RequireComponent(typeof(CarlResources))]
[DefaultExecutionOrder(0)]
public class CarlPickupChaser : MonoBehaviour
{
    static readonly RaycastHit2D[] LosHits = new RaycastHit2D[16];

    CarlLocomotion _locomotion;
    CarlResources _resources;
    Collider2D _collider;

    void Awake()
    {
        _locomotion = GetComponent<CarlLocomotion>();
        _resources = GetComponent<CarlResources>();
        _collider = GetComponent<Collider2D>();
    }

    void FixedUpdate()
    {
        if (_resources.IsGameOver)
            return;

        if (!_locomotion.CanMakeDecisions)
            return;

        _locomotion.RefreshGrounded();
        if (!_locomotion.IsGrounded)
            return;

        if (!TrySelectVisiblePickup(out var pickup))
            return;

        float deltaX = pickup.transform.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) < 0.001f)
            return;

        _locomotion.WalkHorizontally(Mathf.Sign(deltaX));
    }

    /// <summary>
    /// Picks the closest pickup with a clear straight-line sight path.
    /// Blocked pickups are skipped so Carl can walk toward a farther visible one.
    /// </summary>
    bool TrySelectVisiblePickup(out ResourcePickup pickup)
    {
        pickup = null;
        float bestDist = float.MaxValue;
        Vector2 origin = _collider != null ? (Vector2)_collider.bounds.center : (Vector2)transform.position;

        foreach (var candidate in ResourcePickup.ActivePickups)
        {
            if (candidate == null || !candidate.isActiveAndEnabled)
                continue;

            Vector2 target = candidate.transform.position;
            bool clear = HasLineOfSight(origin, target);
            Debug.DrawLine(origin, target, clear ? Color.green : Color.red, Time.fixedDeltaTime);

            if (!clear)
                continue;

            float dist = Vector2.Distance(origin, target);
            if (dist >= bestDist)
                continue;

            bestDist = dist;
            pickup = candidate;
        }

        return pickup != null;
    }

    bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.001f)
            return true;

        int count = Physics2D.RaycastNonAlloc(from, delta / distance, LosHits, distance);
        for (var i = 0; i < count; i++)
        {
            var hit = LosHits[i];
            if (hit.collider == null)
                continue;
            if (hit.collider == _collider)
                continue;
            if (hit.collider.isTrigger)
                continue;
            if (hit.collider.GetComponentInParent<CarlLocomotion>() != null)
                continue;
            if (hit.collider.GetComponentInParent<ResourcePickup>() != null)
                continue;
            // Short hazards — Carl should still chase pickups past them.
            if (hit.collider.GetComponentInParent<Spikes>() != null)
                continue;

            // Solid blocker (wall, floor, spring, one-way deck, etc.).
            return false;
        }

        return true;
    }
}
