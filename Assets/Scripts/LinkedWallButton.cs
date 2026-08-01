using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shared red button that triggers several <see cref="ButtonWall"/> toggles at
/// once. Each wall still slides from its own current position; individual wall
/// buttons keep working independently.
/// </summary>
public class LinkedWallButton : MonoBehaviour
{
    static readonly Color PedestalColor = new(0.32f, 0.36f, 0.44f, 1f);
    static readonly Color PedestalEdge = new(0.22f, 0.55f, 0.72f, 1f);
    static readonly Color TerminalColor = new(0.95f, 0.55f, 0.2f, 1f);
    static readonly Color ButtonRimColor = new(0.35f, 0.08f, 0.08f, 1f);
    static readonly Color ButtonFaceColor = new(0.95f, 0.18f, 0.18f, 1f);
    static readonly Color ButtonBusyColor = new(0.45f, 0.12f, 0.12f, 1f);
    static readonly Color ButtonHighlight = new(1f, 0.55f, 0.55f, 1f);
    static readonly Color WireColor = new(0.95f, 0.45f, 0.2f, 0.95f);

    [SerializeField] float buttonSize = 0.6f;

    readonly List<ButtonWall> _walls = new();
    readonly List<LineRenderer> _wires = new();

    SpriteRenderer _buttonFace;
    SpriteRenderer _buttonHighlight;
    bool _busy;

    public static LinkedWallButton Spawn(Vector2 position, IEnumerable<ButtonWall> walls, float buttonSize = 0.6f)
    {
        var go = new GameObject("LinkedWallButton");
        go.SetActive(false);
        go.transform.position = new Vector3(position.x, position.y, 0f);

        var button = go.AddComponent<LinkedWallButton>();
        button.buttonSize = Mathf.Clamp(buttonSize, 0.45f, 0.9f);
        if (walls != null)
        {
            foreach (var wall in walls)
            {
                if (wall != null)
                    button._walls.Add(wall);
            }
        }

        go.SetActive(true);
        return button;
    }

    void Awake()
    {
        BuildVisual();
        RebuildWires();
    }

    void Update()
    {
        RefreshBusyFromWalls();

        if (!_busy)
        {
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
                TryPress(pointer.position.ReadValue());
        }

        UpdateWirePositions();
    }

    void TryPress(Vector2 screenPosition)
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
            if (hit.GetComponentInParent<LinkedWallButton>() != this)
                continue;
            if (hit.gameObject.name != "ButtonHit")
                continue;

            pressed = true;
            break;
        }

        if (!pressed)
            return;

        TriggerAll();
    }

    void TriggerAll()
    {
        var any = false;
        for (var i = 0; i < _walls.Count; i++)
        {
            var wall = _walls[i];
            if (wall == null)
                continue;
            if (wall.TryTriggerToggle())
                any = true;
        }

        if (any)
        {
            GameSfx.PlayPowerClick();
            RefreshBusyFromWalls();
        }
    }

    void RefreshBusyFromWalls()
    {
        var busy = false;
        for (var i = 0; i < _walls.Count; i++)
        {
            if (_walls[i] != null && _walls[i].IsMoving)
            {
                busy = true;
                break;
            }
        }

        if (_busy == busy)
            return;

        _busy = busy;
        if (_buttonFace != null)
            _buttonFace.color = busy ? ButtonBusyColor : ButtonFaceColor;
        if (_buttonHighlight != null)
            _buttonHighlight.enabled = !busy;
    }

    void RebuildWires()
    {
        for (var i = 0; i < _wires.Count; i++)
        {
            if (_wires[i] != null)
                Destroy(_wires[i].gameObject);
        }

        _wires.Clear();
        for (var i = 0; i < _walls.Count; i++)
        {
            if (_walls[i] == null)
                continue;

            var wireObject = new GameObject($"Wire{i}");
            wireObject.transform.SetParent(transform, false);
            var line = wireObject.AddComponent<LineRenderer>();
            line.positionCount = 3;
            line.useWorldSpace = true;
            line.startWidth = 0.055f;
            line.endWidth = 0.04f;
            line.numCapVertices = 4;
            line.numCornerVertices = 3;
            line.sortingOrder = 2;
            line.material = CreateWireMaterial();
            line.startColor = WireColor;
            line.endColor = WireColor;
            _wires.Add(line);
        }

        UpdateWirePositions();
    }

    void UpdateWirePositions()
    {
        var from = (Vector2)transform.position;
        var wireIndex = 0;
        for (var i = 0; i < _walls.Count && wireIndex < _wires.Count; i++)
        {
            var wall = _walls[i];
            if (wall == null)
                continue;

            var line = _wires[wireIndex++];
            if (line == null)
                continue;

            var to = (Vector2)wall.transform.position;
            var mid = new Vector2(Mathf.Lerp(from.x, to.x, 0.5f), Mathf.Max(from.y, to.y) + 0.25f);
            line.SetPosition(0, from);
            line.SetPosition(1, mid);
            line.SetPosition(2, to);
        }
    }

    void BuildVisual()
    {
        if (transform.Find("Visual") != null)
            return;

        var visualRoot = new GameObject("Visual").transform;
        visualRoot.SetParent(transform, false);

        float pedestalW = buttonSize * 1.2f;
        float pedestalH = buttonSize * 1.35f;
        AddPlate(visualRoot, "Pedestal", PedestalColor, new Vector2(pedestalW, pedestalH), Vector2.zero, 8);
        AddPlate(visualRoot, "PedestalEdge", PedestalEdge, new Vector2(pedestalW * 0.82f, pedestalH * 0.88f), Vector2.zero, 9);

        float term = buttonSize * 0.18f;
        AddPlate(visualRoot, "TermL", TerminalColor, Vector2.one * term, new Vector2(-pedestalW * 0.38f, -pedestalH * 0.32f), 10);
        AddPlate(visualRoot, "TermR", TerminalColor, Vector2.one * term, new Vector2(pedestalW * 0.38f, -pedestalH * 0.32f), 10);

        AddPlate(visualRoot, "ButtonRim", ButtonRimColor, Vector2.one * buttonSize, new Vector2(0f, buttonSize * 0.08f), 11);
        _buttonFace = AddPlate(
            visualRoot,
            "ButtonFace",
            ButtonFaceColor,
            Vector2.one * (buttonSize * 0.78f),
            new Vector2(0f, buttonSize * 0.08f),
            12);
        _buttonHighlight = AddPlate(
            visualRoot,
            "ButtonHighlight",
            ButtonHighlight,
            Vector2.one * (buttonSize * 0.22f),
            new Vector2(-buttonSize * 0.12f, buttonSize * 0.2f),
            13);

        var hit = new GameObject("ButtonHit");
        hit.transform.SetParent(transform, false);
        hit.transform.localPosition = new Vector3(0f, buttonSize * 0.08f, 0f);
        var hitCollider = hit.AddComponent<CircleCollider2D>();
        hitCollider.isTrigger = true;
        hitCollider.radius = buttonSize * 0.55f;
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

    static Material CreateWireMaterial()
    {
        var shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (shader == null)
            shader = Shader.Find("Hidden/Internal-Colored");
        return new Material(shader);
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
        world = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        return true;
    }
}
