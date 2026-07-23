using UnityEngine;

/// <summary>
/// Vertically chases Carl with smoothing so he settles ~1/3 up the screen.
/// Horizontal framing is left unchanged.
/// </summary>
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(100)]
public class CarlCameraFollow : MonoBehaviour
{
    /// <summary>Viewport Y where Carl settles (0 = bottom, 1 = top).</summary>
    const float CarlViewportY = 1f / 3f;
    const float SmoothTime = 0.45f;

    float _smoothVelocityY;
    Transform _carl;

    void LateUpdate()
    {
        if (!TryGetCarl(out var carl))
            return;

        float halfHeight = AspectRatioCamera.WorldHeight * 0.5f;
        // worldY = cameraY + (viewportY - 0.5) * worldHeight
        // → cameraY = carlY - (CarlViewportY - 0.5) * worldHeight
        float targetY = carl.position.y - (CarlViewportY - 0.5f) * AspectRatioCamera.WorldHeight;

        var pos = transform.position;
        pos.y = Mathf.SmoothDamp(pos.y, targetY, ref _smoothVelocityY, SmoothTime);
        transform.position = pos;
    }

    bool TryGetCarl(out Transform carl)
    {
        if (_carl != null)
        {
            carl = _carl;
            return true;
        }

        var locomotion = FindFirstObjectByType<CarlLocomotion>();
        if (locomotion == null)
        {
            carl = null;
            return false;
        }

        _carl = locomotion.transform;
        carl = _carl;
        return true;
    }
}
