using System.Collections.Generic;
using UnityEngine;

public enum PickupType
{
    Oil,
    Energy
}

public class ResourcePickup : MonoBehaviour
{
    public static readonly List<ResourcePickup> ActivePickups = new();

    PickupType _pickupType;

    public PickupType Type => _pickupType;

    void OnEnable() => ActivePickups.Add(this);

    void OnDisable() => ActivePickups.Remove(this);

    public static ResourcePickup Spawn(PickupType type, Vector2 position)
    {
        var name = type == PickupType.Oil ? "OilCan" : "Battery";
        var pickupObject = new GameObject(name);
        pickupObject.transform.position = new Vector3(position.x, position.y, 0f);

        var body = pickupObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.simulated = true;

        var pickup = pickupObject.AddComponent<ResourcePickup>();
        pickup.Initialize(type);
        return pickup;
    }

    void Initialize(PickupType type)
    {
        _pickupType = type;

        var collider = gameObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.45f;

        BuildVisual();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var resources = other.GetComponent<CarlResources>();
        if (resources == null)
            resources = other.GetComponentInParent<CarlResources>();

        if (resources == null)
            return;

        if (_pickupType == PickupType.Oil)
        {
            resources.RestoreOil();
            GameSfx.PlayOilPickup();
        }
        else
        {
            resources.RestoreEnergy();
            GameSfx.PlayBatteryPickup();
        }

        Destroy(gameObject);
    }

    void BuildVisual()
    {
        var sprite = GameSprites.White;
        if (_pickupType == PickupType.Oil)
            BuildOilCan(sprite);
        else
            BuildBattery(sprite);
    }

    void BuildOilCan(Sprite sprite)
    {
        AddQuad("CanBody", sprite, new Color(0.7f, 0.22f, 0.15f), new Vector2(0.32f, 0.42f), Vector2.zero);
        AddQuad("CanLabel", sprite, new Color(0.9f, 0.85f, 0.2f), new Vector2(0.24f, 0.12f), new Vector2(0f, 0.04f));
        AddQuad("CanCap", sprite, new Color(0.45f, 0.45f, 0.48f), new Vector2(0.28f, 0.08f), new Vector2(0f, 0.24f));
    }

    void BuildBattery(Sprite sprite)
    {
        AddQuad("BatteryBody", sprite, new Color(0.2f, 0.65f, 0.28f), new Vector2(0.42f, 0.55f), Vector2.zero);
        AddQuad("BatteryTerminal", sprite, new Color(0.95f, 0.9f, 0.2f), new Vector2(0.14f, 0.16f), new Vector2(0.24f, 0.12f));
        AddQuad("BatteryBolt", sprite, Color.white, new Vector2(0.08f, 0.08f), new Vector2(-0.12f, 0.1f));
    }

    void AddQuad(string partName, Sprite sprite, Color color, Vector2 size, Vector2 localPosition)
    {
        var part = new GameObject(partName);
        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = new Vector3(size.x, size.y, 1f);

        var renderer = part.AddComponent<SpriteRenderer>();
        GameSprites.ConfigureRenderer(renderer);
        renderer.color = color;
        renderer.sortingOrder = 10;
    }
}
