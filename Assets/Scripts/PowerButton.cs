using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Standalone red power button. Toggle powers every connected <see cref="IPowerable"/>.
/// Connections can be wired (LineRenderer) or implied by mounting on/next to a device.
/// </summary>
public class PowerButton : MonoBehaviour
{
    static readonly Color PedestalColor = new(0.32f, 0.36f, 0.44f, 1f);
    static readonly Color PedestalEdge = new(0.22f, 0.55f, 0.72f, 1f);
    static readonly Color TerminalColor = new(0.2f, 0.85f, 0.75f, 1f);
    static readonly Color ButtonRimColor = new(0.35f, 0.08f, 0.08f, 1f);
    static readonly Color ButtonFaceColor = new(0.95f, 0.18f, 0.18f, 1f);
    static readonly Color ButtonOnColor = new(0.55f, 0.08f, 0.08f, 1f);
    static readonly Color ButtonHighlight = new(1f, 0.55f, 0.55f, 1f);
    static readonly Color WireColor = new(0.15f, 0.9f, 0.55f, 0.95f);

    [SerializeField] float buttonSize = 0.55f;
    [SerializeField] bool drawWires = true;

    readonly List<IPowerable> _targets = new();
    readonly List<LineRenderer> _wires = new();

    SpriteRenderer _buttonFace;
    SpriteRenderer _buttonHighlight;
    bool _powered;

    public bool IsPowered => _powered;
    public IReadOnlyList<IPowerable> Targets => _targets;

    public static PowerButton Spawn(
        Vector2 position,
        IEnumerable<IPowerable> targets,
        bool drawWires = true,
        Transform parent = null,
        float buttonSize = 0.55f)
    {
        var go = new GameObject("PowerButton");
        go.SetActive(false);
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(position.x, position.y, 0f);
        }
        else
        {
            go.transform.position = new Vector3(position.x, position.y, 0f);
        }

        var button = go.AddComponent<PowerButton>();
        button.buttonSize = Mathf.Clamp(buttonSize, 0.4f, 0.85f);
        button.drawWires = drawWires;
        if (targets != null)
        {
            foreach (var target in targets)
            {
                if (target != null)
                    button._targets.Add(target);
            }
        }

        go.SetActive(true);
        return button;
    }

    /// <summary>Connect another powerable later (supports multi-wire setups).</summary>
    public void Connect(IPowerable target, bool rebuildWire = true)
    {
        if (target == null || _targets.Contains(target))
            return;

        _targets.Add(target);
        if (rebuildWire && drawWires)
            RebuildWires();
        if (_powered)
            target.SetPowered(true);
    }

    void Awake()
    {
        BuildVisual();
        if (drawWires)
            RebuildWires();
    }

    void Update()
    {
        var pointer = Pointer.current;
        if (pointer != null && pointer.press.wasPressedThisFrame)
            TryPress(pointer.position.ReadValue());

        if (drawWires)
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
            if (hit.GetComponentInParent<PowerButton>() != this)
                continue;
            if (hit.gameObject.name != "ButtonHit")
                continue;

            pressed = true;
            break;
        }

        if (!pressed)
            return;

        SetPowered(!_powered);
    }

    public void SetPowered(bool powered)
    {
        if (_powered == powered)
            return;

        _powered = powered;
        if (_buttonFace != null)
            _buttonFace.color = powered ? ButtonOnColor : ButtonFaceColor;
        if (_buttonHighlight != null)
            _buttonHighlight.enabled = !powered;

        GameSfx.PlayPowerClick();

        for (var i = 0; i < _targets.Count; i++)
        {
            var target = _targets[i];
            if (target != null)
                target.SetPowered(powered);
        }
    }

    void RebuildWires()
    {
        for (var i = 0; i < _wires.Count; i++)
        {
            if (_wires[i] != null)
                Destroy(_wires[i].gameObject);
        }

        _wires.Clear();
        if (!drawWires)
            return;

        for (var i = 0; i < _targets.Count; i++)
        {
            if (_targets[i] == null)
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
        for (var i = 0; i < _targets.Count && wireIndex < _wires.Count; i++)
        {
            var target = _targets[i];
            if (target == null)
                continue;

            var line = _wires[wireIndex++];
            if (line == null)
                continue;

            var to = target.WireAttachPoint;
            var mid = (from + to) * 0.5f;
            // Slight elbow so stacked wires don't fully overlap.
            mid.x += (i % 2 == 0 ? -0.12f : 0.12f) * (i + 1) * 0.15f;
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

        float pedestalW = buttonSize * 1.15f;
        float pedestalH = buttonSize * 1.35f;
        AddPlate(visualRoot, "Pedestal", PedestalColor, new Vector2(pedestalW, pedestalH), Vector2.zero, 8);
        AddPlate(visualRoot, "PedestalEdge", PedestalEdge, new Vector2(pedestalW * 0.82f, pedestalH * 0.88f), Vector2.zero, 9);

        // Distinct cyan terminals — marks this as a power junction, not a slide wall.
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
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            return new Material(Shader.Find("Hidden/Internal-Colored"));
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
        var world3 = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        world = world3;
        return true;
    }
}
