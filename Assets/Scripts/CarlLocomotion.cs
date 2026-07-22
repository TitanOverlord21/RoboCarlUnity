using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CarlResources))]
[DefaultExecutionOrder(10)]
public class CarlLocomotion : MonoBehaviour
{
    public const float BaseWalkSpeed = 2.5f;
    public const float DirectionThinkDuration = 0.15f;

    const float GroundCheckInset = 0.03f;
    /// <summary>Max ledge height Carl can step onto, as a fraction of collider height.</summary>
    const float MaxStepHeightFraction = 0.2f;
    const float StepForwardDistance = 0.12f;
    const float StepSkin = 0.02f;

    static readonly RaycastHit2D[] StepHits = new RaycastHit2D[8];

    [SerializeField] LayerMask groundMask = ~0;

    Rigidbody2D _rigidbody;
    CarlResources _resources;
    Collider2D _collider;
    bool _walkRequested;
    bool _forcedMovement;
    ContactFilter2D _groundFilter;
    float _movingDirection;
    float _desiredDirection;
    float _thinkTimer;

    public bool IsGrounded { get; private set; }
    public float EffectiveWalkSpeed => BaseWalkSpeed * (_resources.IsOilLow ? 0.5f : 1f);

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _resources = GetComponent<CarlResources>();
        _collider = GetComponent<Collider2D>();
        _groundFilter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = groundMask
        };
    }

    void Start()
    {
        SnapFeetToGround();
    }

    /// <summary>
    /// Places Carl so his collider feet rest on the floor under him (no sky drop).
    /// </summary>
    public void SnapFeetToGround()
    {
        if (_rigidbody == null || _collider == null)
            return;

        float probeX = transform.position.x;
        var hits = Physics2D.RaycastAll(new Vector2(probeX, 8f), Vector2.down, 40f, groundMask);
        RaycastHit2D? floorHit = null;
        float bestY = float.NegativeInfinity;

        for (var i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit.collider == null || hit.collider == _collider || hit.collider.isTrigger)
                continue;
            if (hit.collider.GetComponentInParent<CarlLocomotion>() != null)
                continue;
            // Prefer the highest solid surface under the probe (floor top).
            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                floorHit = hit;
            }
        }

        if (floorHit == null)
            return;

        float feetFromCenter = transform.position.y - _collider.bounds.min.y;
        float newY = floorHit.Value.point.y + feetFromCenter + StepSkin;
        _rigidbody.position = new Vector2(probeX, newY);
        _rigidbody.linearVelocity = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (_resources.IsGameOver)
        {
            _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);
            return;
        }

        RefreshGrounded();

        if (IsGrounded && !_forcedMovement && !_walkRequested)
        {
            _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);
            ClearDirectionIntent();
        }

        _walkRequested = false;
    }

    /// <summary>
    /// Carl's own horizontal walking (counts toward oil use).
    /// Pauses briefly whenever he starts moving or switches direction.
    /// </summary>
    public void WalkHorizontally(float direction)
    {
        if (_resources.IsGameOver || _forcedMovement)
            return;

        direction = Mathf.Clamp(direction, -1f, 1f);
        if (Mathf.Approximately(direction, 0f) || !IsGrounded)
            return;

        direction = Mathf.Sign(direction);

        if (!Mathf.Approximately(direction, _movingDirection))
        {
            if (!Mathf.Approximately(direction, _desiredDirection))
            {
                _desiredDirection = direction;
                _thinkTimer = DirectionThinkDuration;
                _movingDirection = 0f;
            }

            _walkRequested = true;
            _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);

            if (_thinkTimer > 0f)
            {
                _thinkTimer -= Time.fixedDeltaTime;
                if (_thinkTimer > 0f)
                    return;

                _movingDirection = _desiredDirection;
            }
        }

        TryStepUp(_movingDirection);

        _walkRequested = true;
        float speed = EffectiveWalkSpeed;
        _resources.RegisterSelfWalkDistance(speed * Time.fixedDeltaTime);
        _rigidbody.linearVelocity = new Vector2(_movingDirection * speed, _rigidbody.linearVelocity.y);
    }

    void ClearDirectionIntent()
    {
        _movingDirection = 0f;
        _desiredDirection = 0f;
        _thinkTimer = 0f;
    }

    /// <summary>
    /// For future scripted movement; does not drain oil.
    /// </summary>
    public void SetForcedHorizontalVelocity(float velocityX)
    {
        _forcedMovement = true;
        _rigidbody.linearVelocity = new Vector2(velocityX, _rigidbody.linearVelocity.y);
    }

    public void ClearForcedMovement()
    {
        _forcedMovement = false;
    }

    /// <summary>
    /// Instant upward launch with a fixed velocity (used by springs). Not AddForce.
    /// </summary>
    public void LaunchVertical(float velocityY)
    {
        if (_resources.IsGameOver)
            return;

        _walkRequested = false;
        IsGrounded = false;
        ClearDirectionIntent();
        _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, velocityY);
    }

    public void RefreshGrounded() => IsGrounded = CheckGrounded();

    bool CheckGrounded()
    {
        if (_collider == null)
            return false;

        var bounds = _collider.bounds;
        var feetCenter = new Vector2(bounds.center.x, bounds.min.y + GroundCheckInset);
        var probeSize = new Vector2(Mathf.Max(0.2f, bounds.size.x * 0.85f), 0.06f);

        var hits = Physics2D.OverlapBoxAll(feetCenter, probeSize, 0f, groundMask);
        foreach (var hit in hits)
        {
            if (!IsExternalGround(hit))
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// If a short ledge sits in front of Carl's feet (up to 20% of his height), step onto it.
    /// </summary>
    void TryStepUp(float direction)
    {
        if (_collider == null)
            return;

        var bounds = _collider.bounds;
        float maxStep = bounds.size.y * MaxStepHeightFraction;
        if (maxStep < 0.01f)
            return;

        float facing = Mathf.Sign(direction);
        float feetMinY = bounds.min.y;
        // Probe slightly below the feet so flush / near-flush ledges still register as a face.
        float lowProbeY = feetMinY - StepSkin;
        float forwardX = bounds.center.x + facing * Mathf.Max(0.05f, bounds.extents.x - 0.04f);
        float castDist = bounds.extents.x + StepForwardDistance;

        var lowOrigin = new Vector2(forwardX, lowProbeY);
        int lowCount = Physics2D.Raycast(lowOrigin, new Vector2(facing, 0f), _groundFilter, StepHits, castDist);
        if (!HasExternalHit(lowCount))
            return;

        var highOrigin = new Vector2(forwardX, feetMinY + maxStep + StepSkin);
        int highCount = Physics2D.Raycast(highOrigin, new Vector2(facing, 0f), _groundFilter, StepHits, castDist);
        if (HasExternalHit(highCount))
            return;

        float probeX = bounds.center.x + facing * (bounds.extents.x + StepForwardDistance);
        var downOrigin = new Vector2(probeX, feetMinY + maxStep + StepSkin);
        float downDist = maxStep + StepSkin * 4f;
        int downCount = Physics2D.Raycast(downOrigin, Vector2.down, _groundFilter, StepHits, downDist);
        if (!TryGetNearestExternalHit(downCount, out var ledgeHit))
            return;

        float stepUp = ledgeHit.point.y - feetMinY;
        // Allow tiny lifts (flush platforms can still block via box corner overlap).
        if (stepUp <= 0.001f || stepUp > maxStep + StepSkin)
            return;

        var position = _rigidbody.position;
        position.y += stepUp + StepSkin;
        _rigidbody.position = position;
        _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, 0f);
        IsGrounded = true;
    }

    bool HasExternalHit(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var col = StepHits[i].collider;
            if (col != null && IsExternalGround(col))
                return true;
        }

        return false;
    }

    bool TryGetNearestExternalHit(int count, out RaycastHit2D nearest)
    {
        nearest = default;
        float best = float.MaxValue;
        bool found = false;

        for (var i = 0; i < count; i++)
        {
            var hit = StepHits[i];
            if (hit.collider == null || !IsExternalGround(hit.collider))
                continue;

            if (hit.distance >= best)
                continue;

            best = hit.distance;
            nearest = hit;
            found = true;
        }

        return found;
    }

    bool IsExternalGround(Collider2D other)
    {
        if (other == null || other == _collider)
            return false;

        if (other.transform == transform || other.transform.IsChildOf(transform))
            return false;

        return true;
    }
}
