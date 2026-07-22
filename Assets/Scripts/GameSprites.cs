using UnityEngine;

/// <summary>
/// Shared white quad sprite for simple props.
/// Keeps URP's default Sprite-Lit material (do not assign null — that renders magenta).
/// </summary>
public static class GameSprites
{
    static Sprite _white;

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
}
