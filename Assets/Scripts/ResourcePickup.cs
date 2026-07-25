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

    const string OilSpritePath = "UI/Icon_Oil";
    const string BatterySpritePath = "UI/Icon_Battery";
    const float DisplayHeight = 0.65f;

    PickupType _pickupType;

    public PickupType Type => _pickupType;

    Rigidbody2D _body;

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
        pickup._body = body;
        pickup.Initialize(type);
        return pickup;
    }

    void Initialize(PickupType type)
    {
        _pickupType = type;
        if (_body == null)
            _body = GetComponent<Rigidbody2D>();

        var collider = gameObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.45f;

        BuildVisual();
    }

    /// <summary>Kinematic nudge used by fans / scripted push.</summary>
    public void Nudge(Vector2 delta)
    {
        if (_body == null)
            _body = GetComponent<Rigidbody2D>();
        if (_body == null)
            return;

        _body.MovePosition(_body.position + delta);
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
        var path = _pickupType == PickupType.Oil ? OilSpritePath : BatterySpritePath;
        var sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning($"ResourcePickup: missing sprite at Resources/{path}");
            return;
        }

        var visual = new GameObject("Visual");
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = Vector3.zero;

        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 10;
        GameSprites.ApplySpriteMaterial(renderer);

        float spriteHeight = sprite.bounds.size.y;
        if (spriteHeight > 0.001f)
        {
            float scale = DisplayHeight / spriteHeight;
            visual.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
