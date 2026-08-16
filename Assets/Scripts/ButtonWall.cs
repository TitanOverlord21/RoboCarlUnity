using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Techno wall with a large center button. Pressing the button slides it the
/// full travel range (same positive/negative clamps as DraggableWall). Input is
/// locked until the ease-in move finishes.
/// </summary>
[DefaultExecutionOrder(-20)]
public class ButtonWall : MonoBehaviour
{
    static readonly Color PlateColor = new(0.42f, 0.48f, 0.58f, 1f);
    static readonly Color EdgeColor = new(0.22f, 0.55f, 0.72f, 1f);
    static readonly Color PanelColor = new(0.28f, 0.32f, 0.40f, 1f);
    static readonly Color AccentColor = new(0.35f, 0.85f, 0.95f, 1f);
    static readonly Color RivetColor = new(0.18f, 0.22f, 0.28f, 1f);
    static readonly Color ButtonRimColor = new(0.35f, 0.08f, 0.08f, 1f);
    static readonly Color ButtonFaceColor = new(0.95f, 0.18f, 0.18f, 1f);
    static readonly Color ButtonBusyColor = new(0.45f, 0.12f, 0.12f, 1f);
    static readonly Color ButtonHighlight = new(1f, 0.55f, 0.55f, 1f);

    const float MinMoveDuration = 0.55f;
    const float MaxMoveDuration = 1.35f;
    const float MoveUnitsPerSecond = 3.2f;

    [SerializeField] Vector2 size = new(0.45f, 2.5f);
    [Tooltip("If true, length is vertical (slide on Y). If false, slide on X.")]
    [SerializeField] bool vertical = true;
    [Tooltip("Max travel from the start position along the positive length axis.")]
    [SerializeField] float dragPositive = 2f;
    [Tooltip("Max travel from the start position along the negative length axis.")]
    [SerializeField] float dragNegative = 2.5f;
    [Tooltip("If false, no on-wall red button (linked / external trigger only).")]
    [SerializeField] bool showButton = true;

    Rigidbody2D _body;
    Vector2 _startPosition;
    SpriteRenderer _buttonFace;
    SpriteRenderer _buttonHighlight;
    bool _moving;
    bool _nextTargetIsMax = true;
    float _moveFrom;
    float _moveTo;
    float _moveTimer;
    float _moveDuration;

    public bool IsMoving => _moving;
    public bool ShowButton => showButton;
    public Vector2 Size => size;

    /// <summary>
    /// Starts a toggle slide from the wall's current position. No-op while busy.
    /// Used by linked multi-door buttons; the wall's own button still works alone when shown.
    /// </summary>
    public bool TryTriggerToggle()
    {
        if (_moving)
            return false;

        return BeginToggleMove();
    }

    public static ButtonWall Spawn(
        Vector2 position,
        Vector2 size,
        float dragPositive,
        float dragNegative,
        bool vertical = true,
        bool showButton = true)
    {
        var wallObject = new GameObject("ButtonWall");
        wallObject.SetActive(false);
        wallObject.transform.position = new Vector3(position.x, position.y, 0f);

        var wall = wallObject.AddComponent<ButtonWall>();
        wall.size = size;
        wall.vertical = vertical;
        wall.dragPositive = Mathf.Max(0f, dragPositive);
        wall.dragNegative = Mathf.Max(0f, dragNegative);
        wall.showButton = showButton;
        wallObject.SetActive(true);
        return wall;
    }

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        _startPosition = transform.position;
        BuildVisual();

        _body = gameObject.GetComponent<Rigidbody2D>();
        if (_body == null)
            _body = gameObject.AddComponent<Rigidbody2D>();
        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.simulated = true;
        _body.freezeRotation = true;

        var collider = gameObject.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.offset = Vector2.zero;
        collider.isTrigger = false;
    }

    void Update()
    {
        if (_moving || !showButton)
            return;

        var pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame)
            return;

        TryPressButton(pointer.position.ReadValue());
    }

    void FixedUpdate()
    {
        if (!_moving)
            return;

        _moveTimer += Time.fixedDeltaTime;
        float u = Mathf.Clamp01(_moveTimer / _moveDuration);
        // Ease-in: slow start, accelerates through the end of the travel.
        float eased = u * u;
        SetAxis(Mathf.Lerp(_moveFrom, _moveTo, eased));

        if (u < 1f)
            return;

        SetAxis(_moveTo);
        _moving = false;
        SetButtonBusy(false);
    }

    void TryPressButton(Vector2 screenPosition)
    {
        if (!TryGetWorldPoint(screenPosition, out var world))
            return;

        var hits = Physics2D.OverlapPointAll(world);
        var pressed = false;
        for (var i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit == null)
                continue;
            if (hit.GetComponentInParent<ButtonWall>() != this)
                continue;
            if (hit.gameObject.name != "ButtonHit")
                continue;

            pressed = true;
            break;
        }

        if (!pressed)
            return;

        BeginToggleMove();
    }

    bool BeginToggleMove()
    {
        float startAxis = vertical ? _startPosition.y : _startPosition.x;
        float min = startAxis - dragNegative;
        float max = startAxis + dragPositive;
        if (Mathf.Abs(max - min) < 0.001f)
            return false;

        float current = GetAxis();
        float target = _nextTargetIsMax ? max : min;
        if (Mathf.Abs(current - target) < 0.02f)
        {
            _nextTargetIsMax = !_nextTargetIsMax;
            target = _nextTargetIsMax ? max : min;
            if (Mathf.Abs(current - target) < 0.02f)
                return false;
        }

        _moveFrom = current;
        _moveTo = target;
        float distance = Mathf.Abs(_moveTo - _moveFrom);
        _moveDuration = Mathf.Clamp(distance / MoveUnitsPerSecond, MinMoveDuration, MaxMoveDuration);
        _moveTimer = 0f;
        _moving = true;
        _nextTargetIsMax = !_nextTargetIsMax;
        SetButtonBusy(true);
        return true;
    }

    float GetAxis() => vertical ? _body.position.y : _body.position.x;

    void SetAxis(float axis)
    {
        var pos = _body.position;
        if (vertical)
            pos.y = axis;
        else
            pos.x = axis;
        // Immediate pose so riders see this tick's delta (same as DraggableWall).
        _body.position = pos;
    }

    void SetButtonBusy(bool busy)
    {
        if (_buttonFace != null)
            _buttonFace.color = busy ? ButtonBusyColor : ButtonFaceColor;
        if (_buttonHighlight != null)
            _buttonHighlight.enabled = !busy;
    }

    static bool TryGetWorldPoint(Vector2 screenPosition, out Vector2 world)
    {
        world = default;
        var camera = Camera.main;
        if (camera == null)
            return false;

        if (!camera.pixelRect.Contains(screenPosition))
            return false;

        float depth = Mathf.Abs(camera.transform.position.z);
        var world3 = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        world = world3;
        return true;
    }

    void BuildVisual()
    {
        if (transform.Find("Visual") != null)
            return;

        var visualRoot = new GameObject("Visual").transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localPosition = Vector3.zero;

        AddPlate(visualRoot, "Plate", PlateColor, size, Vector2.zero, 3);
        AddPlate(visualRoot, "Edge", EdgeColor, size * new Vector2(0.88f, 0.94f), Vector2.zero, 4);
        AddPlate(visualRoot, "Panel", PanelColor, size * new Vector2(0.62f, 0.78f), Vector2.zero, 5);

        float length = vertical ? size.y : size.x;
        float thickness = vertical ? size.x : size.y;

        // Tech accent rails along the length.
        if (vertical)
        {
            float railW = Mathf.Clamp(thickness * 0.12f, 0.04f, 0.07f);
            float railH = length * 0.72f;
            float railX = thickness * 0.28f;
            AddPlate(visualRoot, "RailL", AccentColor, new Vector2(railW, railH), new Vector2(-railX, 0f), 6);
            AddPlate(visualRoot, "RailR", AccentColor, new Vector2(railW, railH), new Vector2(railX, 0f), 6);
        }
        else
        {
            float railH = Mathf.Clamp(thickness * 0.12f, 0.04f, 0.07f);
            float railW = length * 0.72f;
            float railY = thickness * 0.28f;
            AddPlate(visualRoot, "RailB", AccentColor, new Vector2(railW, railH), new Vector2(0f, -railY), 6);
            AddPlate(visualRoot, "RailT", AccentColor, new Vector2(railW, railH), new Vector2(0f, railY), 6);
        }

        float rivet = Mathf.Clamp(thickness * 0.18f, 0.05f, 0.09f);
        float inset = thickness * 0.18f;
        float alongHalf = length * 0.5f - inset - rivet;
        Vector2[] rivetLocals = vertical
            ? new[]
            {
                new Vector2(-size.x * 0.5f + inset, alongHalf),
                new Vector2(size.x * 0.5f - inset, alongHalf),
                new Vector2(-size.x * 0.5f + inset, -alongHalf),
                new Vector2(size.x * 0.5f - inset, -alongHalf)
            }
            : new[]
            {
                new Vector2(-alongHalf, size.y * 0.5f - inset),
                new Vector2(alongHalf, size.y * 0.5f - inset),
                new Vector2(-alongHalf, -size.y * 0.5f + inset),
                new Vector2(alongHalf, -size.y * 0.5f + inset)
            };

        for (var i = 0; i < rivetLocals.Length; i++)
            AddPlate(visualRoot, $"Rivet{i}", RivetColor, Vector2.one * rivet, rivetLocals[i], 7);

        if (!showButton)
            return;

        // Large center button — sized for easy mobile taps.
        float button = Mathf.Clamp(Mathf.Max(thickness * 1.15f, 0.55f), 0.5f, 0.85f);
        AddPlate(visualRoot, "ButtonRim", ButtonRimColor, Vector2.one * button, Vector2.zero, 8);
        _buttonFace = AddPlate(visualRoot, "ButtonFace", ButtonFaceColor, Vector2.one * (button * 0.78f), Vector2.zero, 9);
        _buttonHighlight = AddPlate(
            visualRoot,
            "ButtonHighlight",
            ButtonHighlight,
            Vector2.one * (button * 0.22f),
            new Vector2(-button * 0.12f, button * 0.12f),
            10);

        var hit = new GameObject("ButtonHit");
        hit.transform.SetParent(transform, false);
        hit.transform.localPosition = Vector3.zero;
        var hitCollider = hit.AddComponent<CircleCollider2D>();
        hitCollider.isTrigger = true;
        hitCollider.radius = button * 0.55f;
    }

    static SpriteRenderer AddPlate(
        Transform parent,
        string name,
        Color color,
        Vector2 plateSize,
        Vector2 localPosition,
        int sortingOrder)
    {
        var part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = new Vector3(plateSize.x, plateSize.y, 1f);

        var renderer = part.AddComponent<SpriteRenderer>();
        GameSprites.ConfigureRenderer(renderer);
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }
}
