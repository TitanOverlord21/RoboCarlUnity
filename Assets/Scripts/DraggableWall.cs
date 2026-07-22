using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Solid metal wall that click-drags only along its length axis, clamped to
/// per-instance distances from its start position (customizable per level).
/// </summary>
public class DraggableWall : MonoBehaviour
{
    static readonly Color PlateColor = new(0.55f, 0.58f, 0.62f, 1f);
    static readonly Color EdgeColor = new(0.38f, 0.4f, 0.44f, 1f);
    static readonly Color BoltColor = new(0.28f, 0.3f, 0.33f, 1f);
    static readonly Color BoltHighlight = new(0.72f, 0.74f, 0.78f, 1f);

    [SerializeField] Vector2 size = new(0.4f, 2.5f);
    [Tooltip("If true, length is vertical (drag on Y). If false, length is horizontal (drag on X).")]
    [SerializeField] bool vertical = true;
    [Tooltip("Max travel from the start position along the positive length axis.")]
    [SerializeField] float dragPositive = 2f;
    [Tooltip("Max travel from the start position along the negative length axis.")]
    [SerializeField] float dragNegative = 2.5f;

    Rigidbody2D _body;
    Vector2 _startPosition;
    bool _dragging;
    float _grabOffset;

    public static DraggableWall Spawn(
        Vector2 position,
        Vector2 size,
        float dragPositive,
        float dragNegative,
        bool vertical = true)
    {
        var wallObject = new GameObject("DraggableWall");
        wallObject.SetActive(false);
        wallObject.transform.position = new Vector3(position.x, position.y, 0f);

        var wall = wallObject.AddComponent<DraggableWall>();
        wall.size = size;
        wall.vertical = vertical;
        wall.dragPositive = Mathf.Max(0f, dragPositive);
        wall.dragNegative = Mathf.Max(0f, dragNegative);
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
        var pointer = Pointer.current;
        if (pointer == null)
        {
            if (_dragging)
                EndDrag();
            return;
        }

        if (_dragging)
        {
            if (pointer.press.isPressed)
                UpdateDrag(pointer.position.ReadValue());
            else
                EndDrag();
            return;
        }

        if (pointer.press.wasPressedThisFrame)
            TryBeginDrag(pointer.position.ReadValue());
    }

    void TryBeginDrag(Vector2 screenPosition)
    {
        if (!TryGetWorldPoint(screenPosition, out var world))
            return;

        var hit = Physics2D.OverlapPoint(world);
        if (hit == null || hit.GetComponentInParent<DraggableWall>() != this)
            return;

        _dragging = true;
        float axis = vertical ? world.y : world.x;
        float wallAxis = vertical ? transform.position.y : transform.position.x;
        _grabOffset = wallAxis - axis;
    }

    void UpdateDrag(Vector2 screenPosition)
    {
        if (!TryGetWorldPoint(screenPosition, out var world))
            return;

        float axis = (vertical ? world.y : world.x) + _grabOffset;
        float startAxis = vertical ? _startPosition.y : _startPosition.x;
        float min = startAxis - dragNegative;
        float max = startAxis + dragPositive;
        axis = Mathf.Clamp(axis, min, max);

        var pos = _body.position;
        if (vertical)
            pos.y = axis;
        else
            pos.x = axis;

        _body.position = pos;
    }

    void EndDrag()
    {
        _dragging = false;
    }

    static bool TryGetWorldPoint(Vector2 screenPosition, out Vector2 world)
    {
        world = default;
        var camera = Camera.main;
        if (camera == null)
            return false;

        // Ignore clicks on letterbox bars outside the portrait play viewport.
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
        AddPlate(visualRoot, "Edge", EdgeColor, size * new Vector2(0.92f, 0.96f), Vector2.zero, 4);

        float length = vertical ? size.y : size.x;
        float thickness = vertical ? size.x : size.y;
        float boltSize = Mathf.Clamp(thickness * 0.28f, 0.06f, 0.12f);
        float inset = thickness * 0.22f;
        float alongHalf = length * 0.5f - inset - boltSize;

        Vector2[] boltLocals;
        if (vertical)
        {
            boltLocals = new[]
            {
                new Vector2(-size.x * 0.5f + inset, alongHalf),
                new Vector2(size.x * 0.5f - inset, alongHalf),
                new Vector2(-size.x * 0.5f + inset, 0f),
                new Vector2(size.x * 0.5f - inset, 0f),
                new Vector2(-size.x * 0.5f + inset, -alongHalf),
                new Vector2(size.x * 0.5f - inset, -alongHalf)
            };
        }
        else
        {
            boltLocals = new[]
            {
                new Vector2(-alongHalf, size.y * 0.5f - inset),
                new Vector2(-alongHalf, -size.y * 0.5f + inset),
                new Vector2(0f, size.y * 0.5f - inset),
                new Vector2(0f, -size.y * 0.5f + inset),
                new Vector2(alongHalf, size.y * 0.5f - inset),
                new Vector2(alongHalf, -size.y * 0.5f + inset)
            };
        }

        for (var i = 0; i < boltLocals.Length; i++)
        {
            AddPlate(visualRoot, $"Bolt{i}", BoltColor, Vector2.one * boltSize, boltLocals[i], 5);
            AddPlate(
                visualRoot,
                $"BoltHighlight{i}",
                BoltHighlight,
                Vector2.one * (boltSize * 0.35f),
                boltLocals[i] + new Vector2(-boltSize * 0.12f, boltSize * 0.12f),
                6);
        }
    }

    static void AddPlate(Transform parent, string name, Color color, Vector2 plateSize, Vector2 localPosition, int sortingOrder)
    {
        var part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = new Vector3(plateSize.x, plateSize.y, 1f);

        var renderer = part.AddComponent<SpriteRenderer>();
        GameSprites.ConfigureRenderer(renderer);
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }
}
