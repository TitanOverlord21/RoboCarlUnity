using UnityEngine;

/// <summary>
/// Spring pad: compresses when Carl presses into the launch face, then launches
/// him along that facing with a fixed velocity (not a physics impulse).
/// </summary>
public class SpringPad : MonoBehaviour
{
    public enum Facing
    {
        Up = 0,
        Right = 1,
        Left = 2
    }

    public const float DefaultLaunchSpeed = 7.7f;
    /// <summary>Alias for older call sites; prefer <see cref="DefaultLaunchSpeed"/>.</summary>
    public const float LaunchSpeed = DefaultLaunchSpeed;
    public const float LaunchVelocityY = DefaultLaunchSpeed;
    const float ContractDuration = 0.18f;
    const float RecoverDuration = 0.35f;
    const float MinScaleY = 0.35f;

    enum State
    {
        Idle,
        Compressing,
        Recovering
    }

    [SerializeField] float width = 1.2f;
    [SerializeField] float height = 0.35f;
    [SerializeField] Facing facing = Facing.Up;
    [SerializeField] float launchSpeed = DefaultLaunchSpeed;

    public Vector2 Size => new(width, height);
    public Facing PadFacing => facing;
    public float LaunchPower => launchSpeed > 0f ? launchSpeed : DefaultLaunchSpeed;

    State _state = State.Idle;
    float _timer;
    Transform _visualRoot;
    Vector3 _visualBaseScale;
    BoxCollider2D _collider;
    CarlLocomotion _heldCarl;

    public static SpringPad Spawn(
        Vector2 position,
        float width = 1.2f,
        float height = 0.35f,
        Facing facing = Facing.Up,
        float launchSpeed = 0f)
    {
        var springObject = new GameObject("Spring");
        springObject.SetActive(false);
        springObject.transform.position = new Vector3(position.x, position.y, 0f);
        springObject.transform.rotation = Quaternion.Euler(0f, 0f, FacingZRotation(facing));

        var spring = springObject.AddComponent<SpringPad>();
        spring.width = width;
        spring.height = height;
        spring.facing = facing;
        spring.launchSpeed = launchSpeed > 0f ? launchSpeed : DefaultLaunchSpeed;
        springObject.SetActive(true);
        return spring;
    }

    public static float FacingZRotation(Facing facing)
    {
        return facing switch
        {
            Facing.Right => -90f,
            Facing.Left => 90f,
            _ => 0f
        };
    }

    void Awake()
    {
        // Scene / older spawns may not set rotation from facing yet.
        transform.rotation = Quaternion.Euler(0f, 0f, FacingZRotation(facing));
        BuildVisual();

        _collider = gameObject.AddComponent<BoxCollider2D>();
        _collider.size = new Vector2(width, height);
        _collider.offset = Vector2.zero;
    }

    void FixedUpdate()
    {
        switch (_state)
        {
            case State.Compressing:
                UpdateCompressing();
                break;
            case State.Recovering:
                UpdateRecovering();
                break;
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (_state != State.Idle)
            return;

        var locomotion = collision.collider.GetComponentInParent<CarlLocomotion>();
        if (locomotion == null)
            return;

        // Normals are world-space; convert to local so "into the pad" is always -Y.
        for (var i = 0; i < collision.contactCount; i++)
        {
            var localNormal = transform.InverseTransformDirection(collision.GetContact(i).normal);
            if (localNormal.y > -0.4f)
                continue;

            BeginCompress(locomotion);
            return;
        }

        // Side-stuck against a flush upward spring reports horizontal normals only.
        if (facing == Facing.Up && ShouldMountFromSide(locomotion))
            BeginCompress(locomotion);
    }

    bool ShouldMountFromSide(CarlLocomotion carl)
    {
        if (!carl.IsGrounded)
            return false;

        var carlCollider = carl.GetComponent<Collider2D>();
        if (carlCollider == null)
            return false;

        float feetY = carlCollider.bounds.min.y;
        float top = GetSurfaceWorldPoint().y;
        // Feet must be at/near the walkable top (flush platform/floor springs).
        if (feetY < top - 0.08f || feetY > top + 0.12f)
            return false;

        // Require horizontal overlap with the pad (not a glancing far contact).
        float padHalf = width * 0.5f;
        float carlX = carlCollider.bounds.center.x;
        float padX = transform.position.x;
        float reach = carlCollider.bounds.extents.x + padHalf + 0.05f;
        return Mathf.Abs(carlX - padX) <= reach;
    }

    void BeginCompress(CarlLocomotion carl)
    {
        _state = State.Compressing;
        _timer = 0f;
        _heldCarl = carl;
    }

    void UpdateCompressing()
    {
        _timer += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(_timer / ContractDuration);
        float scaleY = Mathf.Lerp(1f, MinScaleY, t);
        ApplyVisualScale(scaleY);
        SetColliderScaleY(scaleY);

        if (_heldCarl != null)
        {
            // Hold Carl on the pad while it coils down (set velocity, not AddForce).
            var body = _heldCarl.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                var surface = GetSurfaceWorldPoint();
                var launchDir = (Vector2)transform.up;
                var carlCollider = _heldCarl.GetComponent<Collider2D>();
                float extentAlongLaunch = ExtentAlongAxis(carlCollider, launchDir);
                var pos = body.position;
                var target = surface + launchDir * extentAlongLaunch;
                // Keep the non-launch axis from the current body so sideways holds
                // don't yank Carl along the pad face.
                if (facing == Facing.Up)
                    pos.y = target.y;
                else
                    pos.x = target.x;

                body.MovePosition(pos);
                // Kill velocity into the pad so he stays seated while coiling.
                var v = body.linearVelocity;
                float intoPad = Vector2.Dot(v, -launchDir);
                if (intoPad > 0f)
                    body.linearVelocity = v + launchDir * intoPad;
            }
        }

        if (t < 1f)
            return;

        Pop();
    }

    void Pop()
    {
        ApplyVisualScale(1f);
        SetColliderScaleY(1f);
        GameSfx.PlaySpringSproing();

        if (_heldCarl != null)
        {
            var launch = (Vector2)transform.up * LaunchPower;
            _heldCarl.Launch(launch);
        }

        _heldCarl = null;
        _state = State.Recovering;
        _timer = 0f;
    }

    void UpdateRecovering()
    {
        _timer += Time.fixedDeltaTime;
        if (_timer < RecoverDuration)
            return;

        _state = State.Idle;
        _timer = 0f;
    }

    void ApplyVisualScale(float scaleY)
    {
        if (_visualRoot == null)
            return;

        // Scale toward the base so the launch face stays put while coiling.
        _visualRoot.localScale = new Vector3(_visualBaseScale.x, _visualBaseScale.y * scaleY, _visualBaseScale.z);
        _visualRoot.localPosition = new Vector3(0f, (scaleY - 1f) * height * 0.5f, 0f);
    }

    void SetColliderScaleY(float scaleY)
    {
        if (_collider == null)
            return;

        // Keep the launch face fixed while the spring compresses toward the base.
        float topLocal = height * 0.5f;
        float scaledHeight = height * scaleY;
        _collider.size = new Vector2(width, scaledHeight);
        _collider.offset = new Vector2(0f, topLocal - scaledHeight * 0.5f);
    }

    Vector2 GetSurfaceWorldPoint()
    {
        // Launch face stays fixed in local space while coiling.
        float topLocal = height * 0.5f;
        return transform.TransformPoint(new Vector3(0f, topLocal, 0f));
    }

    static float ExtentAlongAxis(Collider2D col, Vector2 axis)
    {
        if (col == null)
            return 0.5f;

        axis.Normalize();
        var e = col.bounds.extents;
        // Conservative AABB projection onto the launch axis.
        return Mathf.Abs(axis.x) * e.x + Mathf.Abs(axis.y) * e.y;
    }

    void BuildVisual()
    {
        _visualRoot = new GameObject("Visual").transform;
        _visualRoot.SetParent(transform, false);
        _visualRoot.localPosition = Vector3.zero;
        _visualBaseScale = Vector3.one;

        var sprite = GameSprites.White;
        // Coil stack (brown/copper spring look). Local +Y is the launch face.
        AddCoil(sprite, "Base", new Color(0.45f, 0.28f, 0.12f), new Vector2(width * 0.95f, 0.06f), new Vector2(0f, -height * 0.42f));
        AddCoil(sprite, "Coil1", new Color(0.7f, 0.45f, 0.18f), new Vector2(width * 0.85f, 0.07f), new Vector2(0f, -height * 0.18f));
        AddCoil(sprite, "Coil2", new Color(0.78f, 0.52f, 0.22f), new Vector2(width * 0.8f, 0.07f), new Vector2(0f, 0.02f));
        AddCoil(sprite, "Coil3", new Color(0.7f, 0.45f, 0.18f), new Vector2(width * 0.85f, 0.07f), new Vector2(0f, height * 0.22f));
        AddCoil(sprite, "Top", new Color(0.35f, 0.55f, 0.75f), new Vector2(width * 0.9f, 0.08f), new Vector2(0f, height * 0.42f));
    }

    void AddCoil(Sprite sprite, string name, Color color, Vector2 size, Vector2 localPosition)
    {
        var part = new GameObject(name);
        part.transform.SetParent(_visualRoot, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = new Vector3(size.x, size.y, 1f);

        var renderer = part.AddComponent<SpriteRenderer>();
        GameSprites.ConfigureRenderer(renderer);
        renderer.color = color;
        renderer.sortingOrder = 2;
    }
}
