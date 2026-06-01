using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds Carl's look and reacts to low energy (drooped posture) and low oil (grubby + sparks).
/// </summary>
[RequireComponent(typeof(CarlResources))]
public class CarlVisual : MonoBehaviour
{
    static readonly Color BodyColor = new(0.42f, 0.5f, 0.56f);
    static readonly Color HeadColor = new(0.52f, 0.6f, 0.66f);
    static readonly Color AccentColor = new(0.28f, 0.33f, 0.38f);
    static readonly Color GrubbyTint = new(0.38f, 0.34f, 0.28f);

    readonly Dictionary<string, Part> _parts = new();
    readonly List<SpriteRenderer> _stainRenderers = new();

    CarlResources _resources;
    CarlSparkEffect _sparks;
    bool _isDrooped;
    bool _isGrubby;

    struct Part
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Vector3 BaseLocalPosition;
        public Quaternion BaseLocalRotation;
        public Vector3 BaseLocalScale;
        public Color BaseColor;
    }

    void Awake()
    {
        _resources = GetComponent<CarlResources>();
        var sprite = GameSprites.White;

        RegisterPart("Body", sprite, BodyColor, new Vector2(0.55f, 0.65f), new Vector2(0f, -0.05f));
        RegisterPart("Head", sprite, HeadColor, new Vector2(0.48f, 0.42f), new Vector2(0f, 0.48f));
        RegisterPart("EyeLeft", sprite, Color.white, new Vector2(0.1f, 0.1f), new Vector2(-0.12f, 0.52f));
        RegisterPart("EyeRight", sprite, Color.white, new Vector2(0.1f, 0.1f), new Vector2(0.12f, 0.52f));
        RegisterPart("Antenna", sprite, AccentColor, new Vector2(0.06f, 0.22f), new Vector2(0f, 0.82f));
        RegisterPart("ArmLeft", sprite, AccentColor, new Vector2(0.12f, 0.35f), new Vector2(-0.38f, 0.05f));
        RegisterPart("ArmRight", sprite, AccentColor, new Vector2(0.12f, 0.35f), new Vector2(0.38f, 0.05f));
        RegisterPart("FootLeft", sprite, AccentColor, new Vector2(0.18f, 0.12f), new Vector2(-0.16f, -0.42f));
        RegisterPart("FootRight", sprite, AccentColor, new Vector2(0.18f, 0.12f), new Vector2(0.16f, -0.42f));

        CreateStains(sprite);
        _sparks = gameObject.AddComponent<CarlSparkEffect>();
    }

    void OnEnable()
    {
        _resources.Changed += OnResourcesChanged;
        OnResourcesChanged();
    }

    void OnDisable()
    {
        _resources.Changed -= OnResourcesChanged;
    }

    void OnResourcesChanged()
    {
        bool droop = _resources.IsEnergyLow;
        bool grubby = _resources.IsOilLow;

        if (droop != _isDrooped)
        {
            _isDrooped = droop;
            ApplyPosture(droop);
        }

        if (grubby != _isGrubby)
        {
            _isGrubby = grubby;
            ApplyGrubby(grubby);
            _sparks.SetActive(grubby);
        }
    }

    void RegisterPart(string partName, Sprite sprite, Color partColor, Vector2 size, Vector2 localPosition)
    {
        var partObject = new GameObject(partName);
        var partTransform = partObject.transform;
        partTransform.SetParent(transform, false);
        partTransform.localPosition = localPosition;
        partTransform.localScale = new Vector3(size.x, size.y, 1f);

        var renderer = partObject.AddComponent<SpriteRenderer>();
        GameSprites.ConfigureRenderer(renderer);
        renderer.color = partColor;
        renderer.sortingOrder = 1;

        _parts[partName] = new Part
        {
            Transform = partTransform,
            Renderer = renderer,
            BaseLocalPosition = partTransform.localPosition,
            BaseLocalRotation = partTransform.localRotation,
            BaseLocalScale = partTransform.localScale,
            BaseColor = partColor
        };
    }

    void CreateStains(Sprite sprite)
    {
        Vector2[] positions =
        {
            new(-0.14f, 0.1f),
            new(0.1f, -0.08f),
            new(0.2f, 0.25f),
            new(-0.22f, 0.3f)
        };

        foreach (var position in positions)
        {
            var stain = new GameObject("Stain");
            stain.transform.SetParent(transform, false);
            stain.transform.localPosition = position;
            stain.transform.localScale = new Vector3(0.14f, 0.1f, 1f);

            var renderer = stain.AddComponent<SpriteRenderer>();
            GameSprites.ConfigureRenderer(renderer);
            renderer.color = new Color(0.22f, 0.18f, 0.12f, 0.85f);
            renderer.sortingOrder = 3;
            renderer.enabled = false;
            _stainRenderers.Add(renderer);
        }
    }

    void ApplyPosture(bool droop)
    {
        if (droop)
        {
            SetPartPose("Body", new Vector3(0f, -0.18f, 0f), Quaternion.Euler(0f, 0f, 18f), null);
            SetPartPose("Head", new Vector3(0.04f, 0.3f, 0f), Quaternion.Euler(0f, 0f, 14f), null);
            SetPartPose("Antenna", new Vector3(-0.04f, 0.68f, 0f), Quaternion.Euler(0f, 0f, 24f), null);
            SetPartPose("EyeLeft", new Vector3(-0.1f, 0.46f, 0f), Quaternion.identity, null);
            SetPartPose("EyeRight", new Vector3(0.14f, 0.44f, 0f), Quaternion.identity, null);
            SetPartPose("ArmLeft", new Vector3(-0.34f, -0.08f, 0f), Quaternion.Euler(0f, 0f, 28f), null);
            SetPartPose("ArmRight", new Vector3(0.36f, -0.04f, 0f), Quaternion.Euler(0f, 0f, 8f), null);
        }
        else
        {
            RestoreAllParts();
        }
    }

    void ApplyGrubby(bool grubby)
    {
        foreach (var part in _parts.Values)
        {
            part.Renderer.color = grubby
                ? MultiplyColor(part.BaseColor, GrubbyTint)
                : part.BaseColor;
        }

        foreach (var stain in _stainRenderers)
            stain.enabled = grubby;
    }

    void RestoreAllParts()
    {
        foreach (var pair in _parts)
            SetPartPose(pair.Key, pair.Value.BaseLocalPosition, pair.Value.BaseLocalRotation, pair.Value.BaseLocalScale);
    }

    void SetPartPose(string partName, Vector3 position, Quaternion rotation, Vector3? scale)
    {
        if (!_parts.TryGetValue(partName, out var part))
            return;

        part.Transform.localPosition = position;
        part.Transform.localRotation = rotation;
        if (scale.HasValue)
            part.Transform.localScale = scale.Value;
    }

    static Color MultiplyColor(Color baseColor, Color tint)
    {
        return new Color(
            baseColor.r * tint.r,
            baseColor.g * tint.g,
            baseColor.b * tint.b,
            baseColor.a);
    }
}
