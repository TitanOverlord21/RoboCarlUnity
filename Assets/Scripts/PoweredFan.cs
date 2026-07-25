using UnityEngine;

/// <summary>
/// Powered industrial fan. When on: blades spin, wind streaks blow outward,
/// pushes Carl and oil/battery pickups (stronger when closer). Wind (force and
/// visuals) is blocked by solid terrain. Hum plays while powered and on-camera.
/// Supports left/right (horizontal) and up (vertical) facing.
/// </summary>
[DefaultExecutionOrder(100)]
public class PoweredFan : MonoBehaviour, IPowerable
{
    public enum Facing
    {
        Right,
        Left,
        Up
    }

    const string HousingSpritePath = "Props/Prop_FanHousing";
    const string BladesSpritePath = "Props/Prop_FanBlades";

    static readonly Color WindColor = new(0.55f, 0.85f, 1f, 0.35f);
    static readonly Color FallbackHousing = new(0.38f, 0.42f, 0.5f, 1f);
    static readonly Color FallbackBlade = new(0.72f, 0.76f, 0.82f, 1f);
    static readonly RaycastHit2D[] WindHits = new RaycastHit2D[12];

    const float BladeSpinSpeed = 720f;
    const float MaxPushSpeed = 4.2f;
    const float DefaultWindRange = 4.2f;
    const float DefaultHeight = 1.55f;

    [SerializeField] float height = DefaultHeight;
    [SerializeField] Facing facing = Facing.Right;
    [SerializeField] float windRange = DefaultWindRange;
    [SerializeField] float windCross = 1.8f;

    Transform _bladeRoot;
    Transform _windRoot;
    SpriteRenderer[] _windStreaks;
    AudioSource _humSource;
    CarlLocomotion _carl;
    bool _powered;
    float _windPulse;
    float _housingDepth;

    public bool IsPowered => _powered;
    public Facing BlowFacing => facing;
    public bool FaceRight => facing == Facing.Right;
    public Vector2 WireAttachPoint => transform.position;
    public float HousingDepth => _housingDepth;

    public static PoweredFan Spawn(
        Vector2 position,
        bool faceRight = true,
        float height = DefaultHeight,
        float windRange = DefaultWindRange)
    {
        return Spawn(position, faceRight ? Facing.Right : Facing.Left, height, windRange);
    }

    public static PoweredFan Spawn(
        Vector2 position,
        Facing facing,
        float height = DefaultHeight,
        float windRange = DefaultWindRange)
    {
        var go = new GameObject("PoweredFan");
        go.SetActive(false);
        go.transform.position = new Vector3(position.x, position.y, 0f);

        var fan = go.AddComponent<PoweredFan>();
        fan.height = Mathf.Max(0.8f, height);
        fan.facing = facing;
        fan.windRange = Mathf.Max(1f, windRange);
        fan.windCross = Mathf.Max(1f, height * 1.15f);
        go.SetActive(true);
        return fan;
    }

    public void SetPowered(bool powered)
    {
        if (_powered == powered)
            return;

        _powered = powered;
        if (_windRoot != null)
            _windRoot.gameObject.SetActive(powered);

        if (!powered)
            StopHum();
    }

    void Awake()
    {
        BuildVisual();
        BuildCollider();
        EnsureHumSource();
        if (_windRoot != null)
            _windRoot.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_powered)
        {
            StopHum();
            return;
        }

        if (_bladeRoot != null)
        {
            float dir = facing == Facing.Left ? 1f : -1f;
            _bladeRoot.Rotate(0f, 0f, BladeSpinSpeed * dir * Time.deltaTime);
        }

        AnimateWind();
        UpdateHum();
    }

    void FixedUpdate()
    {
        if (!_powered)
            return;

        PushTargets();
    }

    void PushTargets()
    {
        var origin = GetNozzleOrigin();
        var windDir = GetWindDirection();
        float halfCross = windCross * 0.5f;

        if (_carl == null)
            _carl = FindAnyObjectByType<CarlLocomotion>();
        if (_carl != null)
        {
            TryPushBody(
                _carl.transform.position,
                origin,
                windDir,
                halfCross,
                speed =>
                {
                    if (facing == Facing.Up)
                        _carl.SetExternalVelocityY(speed);
                    else
                        _carl.AddExternalVelocityX(speed);
                });
        }

        for (var i = 0; i < ResourcePickup.ActivePickups.Count; i++)
        {
            var pickup = ResourcePickup.ActivePickups[i];
            if (pickup == null)
                continue;

            TryPushBody(
                pickup.transform.position,
                origin,
                windDir,
                halfCross,
                speed =>
                {
                    if (facing == Facing.Up)
                        pickup.SetWindVelocityY(speed);
                    else
                        pickup.SetWindVelocityX(speed);
                });
        }
    }

    void TryPushBody(Vector2 target, Vector2 origin, Vector2 windDir, float halfCross, System.Action<float> applySpeed)
    {
        Vector2 delta = target - origin;
        float along = Vector2.Dot(delta, windDir);
        if (along < 0f || along > windRange)
            return;

        // Perpendicular distance from the wind axis.
        float across = facing == Facing.Up
            ? Mathf.Abs(delta.x)
            : Mathf.Abs(delta.y);
        if (across > halfCross)
            return;

        if (!HasClearWindPath(origin, target))
            return;

        float falloff = 1f - (along / windRange);
        falloff *= falloff;
        float edge = 1f - Mathf.Clamp01(across / halfCross);
        float speed = MaxPushSpeed * falloff * Mathf.Lerp(0.35f, 1f, edge);
        if (facing == Facing.Left)
            speed = -speed;
        applySpeed(speed);
    }

    Vector2 GetWindDirection()
    {
        return facing switch
        {
            Facing.Left => Vector2.left,
            Facing.Up => Vector2.up,
            _ => Vector2.right
        };
    }

    Vector2 GetNozzleOrigin()
    {
        float nozzle = _housingDepth * 0.45f;
        return facing switch
        {
            Facing.Left => new Vector2(transform.position.x - nozzle, transform.position.y),
            Facing.Up => new Vector2(transform.position.x, transform.position.y + nozzle),
            _ => new Vector2(transform.position.x + nozzle, transform.position.y)
        };
    }

    /// <summary>
    /// True if no solid terrain sits between nozzle and target.
    /// Ignores triggers, Carl, pickups, and this fan's own colliders.
    /// </summary>
    bool HasClearWindPath(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.001f)
            return true;

        int count = Physics2D.Raycast(from, delta / distance, ContactFilter2D.noFilter, WindHits, distance);
        for (var i = 0; i < count; i++)
        {
            if (IsWindBlocker(WindHits[i].collider))
                return false;
        }

        return true;
    }

    /// <summary>Clear travel distance along a wind ray before hitting terrain.</summary>
    float GetClearWindDistance(Vector2 from, Vector2 direction, float maxDistance)
    {
        if (maxDistance <= 0.001f)
            return 0f;

        int count = Physics2D.Raycast(from, direction, ContactFilter2D.noFilter, WindHits, maxDistance);
        float clear = maxDistance;
        for (var i = 0; i < count; i++)
        {
            var hit = WindHits[i];
            if (!IsWindBlocker(hit.collider))
                continue;
            if (hit.distance < clear)
                clear = hit.distance;
        }

        return clear;
    }

    bool IsWindBlocker(Collider2D collider)
    {
        if (collider == null || collider.isTrigger)
            return false;
        if (collider.transform == transform || collider.transform.IsChildOf(transform))
            return false;
        if (collider.GetComponentInParent<CarlLocomotion>() != null)
            return false;
        if (collider.GetComponentInParent<ResourcePickup>() != null)
            return false;
        // One-ways let Carl rise through; wind passes the same way.
        if (collider.GetComponentInParent<OneWayPlatform>() != null)
            return false;
        // Solids like walls, floors, springs, spikes, and fan housings block.
        return true;
    }

    void AnimateWind()
    {
        if (_windStreaks == null)
            return;

        // Streaks are authored in right-facing local space; Left/Up come from
        // visualRoot scale/rotation, so keep local motion along +X.
        var origin = GetNozzleOrigin();
        var windDir = GetWindDirection();

        _windPulse += Time.deltaTime * 3.5f;
        for (var i = 0; i < _windStreaks.Length; i++)
        {
            var streak = _windStreaks[i];
            if (streak == null)
                continue;

            float across = ((i % 3) - 1) * (windCross * 0.22f);
            var rayOrigin = facing == Facing.Up
                ? origin + new Vector2(across, 0f)
                : origin + new Vector2(0f, across);
            float clear = GetClearWindDistance(rayOrigin, windDir, windRange);
            if (clear < 0.12f)
            {
                streak.enabled = false;
                continue;
            }

            streak.enabled = true;
            float phase = _windPulse + i * 0.85f;
            float u = Mathf.Repeat(phase * 0.35f, 1f);
            float alpha = (1f - u) * 0.45f;
            var c = WindColor;
            c.a = alpha;
            streak.color = c;

            var t = streak.transform;
            float maxAlong = clear * 0.92f;
            float x = Mathf.Lerp(0.1f, maxAlong, u);
            t.localPosition = new Vector3(x, across, 0f);
            float len = Mathf.Min(Mathf.Lerp(0.35f, 0.9f, 1f - u), Mathf.Max(0.12f, clear * 0.25f));
            t.localScale = new Vector3(len, 0.06f, 1f);
        }
    }

    void UpdateHum()
    {
        if (_humSource == null)
            return;

        bool onScreen = IsOnScreen();
        float vol = GameAudioSettings.SfxVolume * 0.35f;
        if (!onScreen || vol <= 0.001f)
        {
            StopHum();
            return;
        }

        _humSource.volume = vol;
        if (!_humSource.isPlaying)
        {
            _humSource.clip = GameSfx.GetFanHumClip();
            _humSource.loop = true;
            _humSource.Play();
        }
    }

    void StopHum()
    {
        if (_humSource != null && _humSource.isPlaying)
            _humSource.Stop();
    }

    bool IsOnScreen()
    {
        var camera = Camera.main;
        if (camera == null)
            return false;

        var vp = camera.WorldToViewportPoint(transform.position);
        return vp.z > 0f && vp.x > -0.05f && vp.x < 1.05f && vp.y > -0.05f && vp.y < 1.05f;
    }

    void EnsureHumSource()
    {
        _humSource = gameObject.GetComponent<AudioSource>();
        if (_humSource == null)
            _humSource = gameObject.AddComponent<AudioSource>();
        _humSource.playOnAwake = false;
        _humSource.spatialBlend = 0f;
        _humSource.loop = true;
    }

    void BuildCollider()
    {
        // Slim solid body — wind still reaches pickups in front / above.
        float depth = Mathf.Max(0.45f, _housingDepth * 0.85f);
        var collider = gameObject.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider2D>();

        if (facing == Facing.Up)
            collider.size = new Vector2(height * 0.92f, depth);
        else
            collider.size = new Vector2(depth, height * 0.92f);
        collider.offset = Vector2.zero;
        collider.isTrigger = false;
    }

    void BuildVisual()
    {
        if (transform.Find("Visual") != null)
            return;

        var visualRoot = new GameObject("Visual").transform;
        visualRoot.SetParent(transform, false);

        // Build in the Right-facing layout, then rotate the whole visual for Left/Up.
        float facingScaleX = 1f;
        var housingSprite = Resources.Load<Sprite>(HousingSpritePath);
        var bladesSprite = Resources.Load<Sprite>(BladesSpritePath);

        if (housingSprite != null)
        {
            float spriteH = Mathf.Max(0.001f, housingSprite.bounds.size.y);
            float scale = height / spriteH;
            _housingDepth = housingSprite.bounds.size.x * scale;

            var housing = new GameObject("Housing");
            housing.transform.SetParent(visualRoot, false);
            housing.transform.localScale = new Vector3(facingScaleX * scale, scale, 1f);

            var renderer = housing.AddComponent<SpriteRenderer>();
            renderer.sprite = housingSprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 4;
            GameSprites.ApplyUnlitSpriteMaterial(renderer);
        }
        else
        {
            _housingDepth = height * 0.55f;
            AddPlate(visualRoot, "Housing", FallbackHousing, new Vector2(_housingDepth, height), Vector2.zero, 4);
        }

        _bladeRoot = new GameObject("Blades").transform;
        _bladeRoot.SetParent(visualRoot, false);
        // Sit in the front opening of the side-facing housing.
        _bladeRoot.localPosition = new Vector3(facingScaleX * _housingDepth * 0.22f, 0.02f, 0f);

        if (bladesSprite != null)
        {
            float bladeWorld = height * 0.58f;
            float bladeSpriteH = Mathf.Max(0.001f, bladesSprite.bounds.size.y);
            float bladeScale = bladeWorld / bladeSpriteH;

            var blades = new GameObject("BladeSprite");
            blades.transform.SetParent(_bladeRoot, false);
            blades.transform.localScale = new Vector3(bladeScale, bladeScale, 1f);

            var renderer = blades.AddComponent<SpriteRenderer>();
            renderer.sprite = bladesSprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 5;
            GameSprites.ApplyUnlitSpriteMaterial(renderer);
        }
        else
        {
            float bladeLen = height * 0.34f;
            float bladeW = height * 0.09f;
            for (var i = 0; i < 3; i++)
            {
                var blade = AddPlate(
                    _bladeRoot,
                    $"Blade{i}",
                    FallbackBlade,
                    new Vector2(bladeW, bladeLen),
                    new Vector2(0f, bladeLen * 0.28f),
                    6);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, i * 120f);
            }
        }

        _windRoot = new GameObject("Wind").transform;
        _windRoot.SetParent(visualRoot, false);
        _windRoot.localPosition = new Vector3(facingScaleX * _housingDepth * 0.48f, 0f, 0f);

        _windStreaks = new SpriteRenderer[5];
        for (var i = 0; i < _windStreaks.Length; i++)
            _windStreaks[i] = AddPlate(_windRoot, $"Streak{i}", WindColor, new Vector2(0.5f, 0.06f), Vector2.zero, 3);

        // Orient the built right-facing art.
        if (facing == Facing.Left)
            visualRoot.localScale = new Vector3(-1f, 1f, 1f);
        else if (facing == Facing.Up)
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, 90f);
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

    void OnDisable() => StopHum();
}
