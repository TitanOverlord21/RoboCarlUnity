using System.Collections.Generic;
using UnityEngine;

public enum PickupType
{
    Oil,
    Energy
}

/// <summary>
/// Oil / battery pickup. Dynamic body so floors, walls, fans, and one-way
/// platforms block it the same way they block Carl. Carl still collects via
/// his trigger radius (solid-solid with Carl is ignored).
/// </summary>
public class ResourcePickup : MonoBehaviour
{
    public static readonly List<ResourcePickup> ActivePickups = new();

    const string OilSpritePath = "UI/Icon_Oil";
    const string BatterySpritePath = "UI/Icon_Battery";
    const float DisplayHeight = 0.65f;
    const float ColliderRadius = 0.28f;

    static PhysicsMaterial2D _sharedMaterial;

    PickupType _pickupType;
    Rigidbody2D _body;
    CircleCollider2D _collider;

    public PickupType Type => _pickupType;

    void OnEnable() => ActivePickups.Add(this);

    void OnDisable() => ActivePickups.Remove(this);

    public static ResourcePickup Spawn(PickupType type, Vector2 position)
    {
        var name = type == PickupType.Oil ? "OilCan" : "Battery";
        var pickupObject = new GameObject(name);
        pickupObject.transform.position = new Vector3(position.x, position.y, 0f);

        var body = pickupObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.simulated = true;
        body.freezeRotation = true;
        body.gravityScale = 1f;
        body.mass = 0.35f;
        body.linearDamping = 0.4f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

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

        _collider = gameObject.AddComponent<CircleCollider2D>();
        _collider.isTrigger = false;
        _collider.radius = ColliderRadius;
        _collider.sharedMaterial = SharedMaterial();

        BuildVisual();
        IgnoreCarlSolidCollision();
    }

    void Start()
    {
        // Carl may spawn in the same frame; retry ignore once scene bodies exist.
        IgnoreCarlSolidCollision();
    }

    /// <summary>
    /// Fan wind: set horizontal velocity (does not teleport through solids).
    /// </summary>
    public void SetWindVelocityX(float velocityX)
    {
        if (_body == null)
            _body = GetComponent<Rigidbody2D>();
        if (_body == null)
            return;

        var v = _body.linearVelocity;
        _body.linearVelocity = new Vector2(velocityX, v.y);
    }

    /// <summary>
    /// Fan wind: set vertical velocity (upward / downward fans).
    /// </summary>
    public void SetWindVelocityY(float velocityY)
    {
        if (_body == null)
            _body = GetComponent<Rigidbody2D>();
        if (_body == null)
            return;

        var v = _body.linearVelocity;
        _body.linearVelocity = new Vector2(v.x, velocityY);
    }

    void OnTriggerEnter2D(Collider2D other) => TryCollect(other);

    void OnTriggerStay2D(Collider2D other) => TryCollect(other);

    void TryCollect(Collider2D other)
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

    void IgnoreCarlSolidCollision()
    {
        if (_collider == null)
            _collider = GetComponent<CircleCollider2D>();
        if (_collider == null)
            return;

        var carl = FindAnyObjectByType<CarlLocomotion>();
        if (carl == null)
            return;

        var carlColliders = carl.GetComponents<Collider2D>();
        for (var i = 0; i < carlColliders.Length; i++)
        {
            var other = carlColliders[i];
            if (other == null || other.isTrigger)
                continue;
            Physics2D.IgnoreCollision(_collider, other, true);
        }
    }

    static PhysicsMaterial2D SharedMaterial()
    {
        if (_sharedMaterial != null)
            return _sharedMaterial;

        _sharedMaterial = new PhysicsMaterial2D("ResourcePickupMaterial")
        {
            friction = 0.35f,
            bounciness = 0f
        };
        return _sharedMaterial;
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
