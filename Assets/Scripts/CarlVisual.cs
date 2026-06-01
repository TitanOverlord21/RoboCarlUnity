using UnityEngine;

/// <summary>
/// Builds a simple blocky robot look for Carl from scaled sprite quads.
/// </summary>
public class CarlVisual : MonoBehaviour
{
    static readonly Color BodyColor = new(0.42f, 0.5f, 0.56f);
    static readonly Color HeadColor = new(0.52f, 0.6f, 0.66f);
    static readonly Color AccentColor = new(0.28f, 0.33f, 0.38f);

    void Awake()
    {
        var sprite = GameSprites.White;
        AddPart("Body", sprite, BodyColor, new Vector2(0.55f, 0.65f), new Vector2(0f, -0.05f));
        AddPart("Head", sprite, HeadColor, new Vector2(0.48f, 0.42f), new Vector2(0f, 0.48f));
        AddPart("EyeLeft", sprite, Color.white, new Vector2(0.1f, 0.1f), new Vector2(-0.12f, 0.52f));
        AddPart("EyeRight", sprite, Color.white, new Vector2(0.1f, 0.1f), new Vector2(0.12f, 0.52f));
        AddPart("Antenna", sprite, AccentColor, new Vector2(0.06f, 0.22f), new Vector2(0f, 0.82f));
        AddPart("ArmLeft", sprite, AccentColor, new Vector2(0.12f, 0.35f), new Vector2(-0.38f, 0.05f));
        AddPart("ArmRight", sprite, AccentColor, new Vector2(0.12f, 0.35f), new Vector2(0.38f, 0.05f));
        AddPart("FootLeft", sprite, AccentColor, new Vector2(0.18f, 0.12f), new Vector2(-0.16f, -0.42f));
        AddPart("FootRight", sprite, AccentColor, new Vector2(0.18f, 0.12f), new Vector2(0.16f, -0.42f));
    }

    void AddPart(string partName, Sprite sprite, Color partColor, Vector2 size, Vector2 localPosition)
    {
        var part = new GameObject(partName);
        var partTransform = part.transform;
        partTransform.SetParent(transform, false);
        partTransform.localPosition = localPosition;
        partTransform.localScale = new Vector3(size.x, size.y, 1f);

        var renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = partColor;
        renderer.sortingOrder = 1;
    }
}
