using UnityEngine;

/// <summary>
/// Thin platform Carl can rise through from below, but stands on from above.
/// </summary>
public class OneWayPlatform : MonoBehaviour
{
    static readonly Color DeckColor = new(0.28f, 0.28f, 0.32f, 1f);
    static readonly Color SupportColor = new(0.22f, 0.22f, 0.26f, 1f);

    [SerializeField] Vector2 size = new(3.2f, 0.04f);

    public static OneWayPlatform Spawn(Vector2 position, Vector2 size)
    {
        var platformObject = new GameObject("OneWayPlatform");
        platformObject.SetActive(false);
        platformObject.transform.position = new Vector3(position.x, position.y, 0f);

        var platform = platformObject.AddComponent<OneWayPlatform>();
        platform.size = size;
        platformObject.SetActive(true);
        return platform;
    }

    void Awake()
    {
        BuildVisual();

        var collider = gameObject.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.offset = Vector2.zero;
        collider.isTrigger = false;
        collider.usedByEffector = true;

        var effector = gameObject.GetComponent<PlatformEffector2D>();
        if (effector == null)
            effector = gameObject.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 170f;
        effector.useSideFriction = false;
        effector.useSideBounce = false;
    }

    void BuildVisual()
    {
        if (transform.Find("Visual") != null)
            return;

        var visualRoot = new GameObject("Visual").transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localPosition = Vector3.zero;

        // Thin top deck (bottom half of a normal floor thickness is omitted).
        AddPart(visualRoot, "Deck", DeckColor, size, Vector2.zero, 0f, 1);

        float halfW = size.x * 0.5f;
        float brace = Mathf.Clamp(size.x * 0.08f, 0.18f, 0.32f);
        float braceDrop = brace * 0.55f;

        // Triangular corner supports under each end.
        AddPart(
            visualRoot,
            "LeftSupport",
            SupportColor,
            new Vector2(brace, brace * 0.7f),
            new Vector2(-halfW + brace * 0.35f, -braceDrop * 0.45f),
            35f,
            0);
        AddPart(
            visualRoot,
            "RightSupport",
            SupportColor,
            new Vector2(brace, brace * 0.7f),
            new Vector2(halfW - brace * 0.35f, -braceDrop * 0.45f),
            -35f,
            0);
    }

    static void AddPart(
        Transform parent,
        string name,
        Color color,
        Vector2 partSize,
        Vector2 localPosition,
        float rotationZ,
        int sortingOrder)
    {
        var part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        part.transform.localScale = new Vector3(partSize.x, partSize.y, 1f);

        var renderer = part.AddComponent<SpriteRenderer>();
        GameSprites.ConfigureRenderer(renderer);
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }
}
