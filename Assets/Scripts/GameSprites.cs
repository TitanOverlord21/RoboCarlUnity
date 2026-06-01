using UnityEngine;

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

            _white = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _white;
        }
    }
}
