using UnityEngine;

/// <summary>
/// Shared white quad sprite for simple props.
/// Keeps URP's default Sprite-Lit material (do not assign null — that renders magenta).
/// </summary>
public static class GameSprites
{
    static Sprite _white;
    static Sprite _triangle;

    public static Sprite White
    {
        get
        {
            if (_white != null)
                return _white;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            texture.name = "GameSprites_White";

            _white = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            _white.name = "GameSprites_WhiteSprite";
            return _white;
        }
    }

    /// <summary>Unit triangle pointing +Y (tip at top). Used for arrow heads.</summary>
    public static Sprite Triangle
    {
        get
        {
            if (_triangle != null)
                return _triangle;

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var clear = new Color(0f, 0f, 0f, 0f);
            for (var y = 0; y < size; y++)
            {
                // Narrow at the top (tip), wide at the bottom (base).
                float t = 1f - (y / (float)(size - 1));
                float half = t * 0.5f;
                for (var x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    bool inside = Mathf.Abs(u - 0.5f) <= half + 0.001f;
                    texture.SetPixel(x, y, inside ? Color.white : clear);
                }
            }

            texture.Apply();
            texture.name = "GameSprites_Triangle";
            _triangle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            _triangle.name = "GameSprites_TriangleSprite";
            return _triangle;
        }
    }

    public static void ConfigureRenderer(SpriteRenderer renderer)
    {
        renderer.sprite = White;
        ApplySpriteMaterial(renderer);
    }

    /// <summary>
    /// Keep the renderer on URP's default Sprite-Lit material.
    /// Explicitly assigning null clears the material and renders magenta/purple.
    /// </summary>
    public static void ApplySpriteMaterial(SpriteRenderer renderer)
    {
        if (renderer.sharedMaterial != null)
            return;

        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (shader != null)
            renderer.sharedMaterial = new Material(shader);
    }

    /// <summary>
    /// Unlit sprite material — keeps authored PNG colors (no 2D-light washout).
    /// </summary>
    public static void ApplyUnlitSpriteMaterial(SpriteRenderer renderer)
    {
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
            renderer.sharedMaterial = new Material(shader);
    }
}
