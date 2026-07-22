using UnityEngine;

/// <summary>
/// Keeps a portrait play area with height:width = 19.5:9 (9 world units wide, 19.5 tall).
/// </summary>
[RequireComponent(typeof(Camera))]
public class AspectRatioCamera : MonoBehaviour
{
    public const float WorldWidth = 9f;
    public const float WorldHeight = 19.5f;

    /// <summary>Width / height of the mobile playfield (9:19.5).</summary>
    public const float TargetAspect = WorldWidth / WorldHeight;

    Camera _camera;

    void Awake()
    {
        _camera = GetComponent<Camera>();
        _camera.orthographic = true;
        _camera.orthographicSize = WorldHeight * 0.5f;
    }

    void Update()
    {
        float windowAspect = (float)Screen.width / Screen.height;

        if (windowAspect >= TargetAspect)
        {
            float scale = windowAspect / TargetAspect;
            _camera.rect = new Rect((1f - 1f / scale) * 0.5f, 0f, 1f / scale, 1f);
        }
        else
        {
            float scale = TargetAspect / windowAspect;
            _camera.rect = new Rect(0f, (1f - 1f / scale) * 0.5f, 1f, 1f / scale);
        }
    }
}
