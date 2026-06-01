using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CarlResources))]
[DefaultExecutionOrder(10)]
public class CarlLocomotion : MonoBehaviour
{
    public const float BaseWalkSpeed = 2.5f;

    const float GroundCheckDistance = 0.2f;
    const float GroundCheckInset = 0.03f;

    [SerializeField] LayerMask groundMask = ~0;

    Rigidbody2D _rigidbody;
    CarlResources _resources;
    Collider2D _collider;
    bool _walkRequested;
    bool _forcedMovement;

    public bool IsGrounded { get; private set; }
    public float EffectiveWalkSpeed => BaseWalkSpeed * (_resources.IsOilLow ? 0.5f : 1f);

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _resources = GetComponent<CarlResources>();
        _collider = GetComponent<Collider2D>();
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
            _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);

        _walkRequested = false;
    }

    /// <summary>
    /// Carl's own horizontal walking (counts toward oil use).
    /// </summary>
    public void WalkHorizontally(float direction)
    {
        if (_resources.IsGameOver || _forcedMovement)
            return;

        direction = Mathf.Clamp(direction, -1f, 1f);
        if (Mathf.Approximately(direction, 0f) || !IsGrounded)
            return;

        _walkRequested = true;
        float speed = EffectiveWalkSpeed;
        _resources.RegisterSelfWalkDistance(speed * Time.fixedDeltaTime);
        _rigidbody.linearVelocity = new Vector2(direction * speed, _rigidbody.linearVelocity.y);
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
            if (hit == _collider)
                continue;

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            return true;
        }

        return false;
    }
}
