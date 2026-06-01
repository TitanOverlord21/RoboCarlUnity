using UnityEngine;

public static class GameSprites
{
    static Sprite _white;
    static Material _spriteMaterial;

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

            _white = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _white;
        }
    }

    public static void ConfigureRenderer(SpriteRenderer renderer)
    {
        renderer.sprite = White;

        if (_spriteMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader != null)
                _spriteMaterial = new Material(shader);
        }

        if (_spriteMaterial != null)
            renderer.sharedMaterial = _spriteMaterial;
    }
}
