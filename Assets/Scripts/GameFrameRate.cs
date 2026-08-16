using UnityEngine;

/// <summary>
/// Caps Update, FixedUpdate, and rendering at 60 Hz for the whole game.
/// VSync is forced off so a high-refresh monitor cannot run faster.
/// </summary>
public static class GameFrameRate
{
    public const int TargetFps = 60;
    public const float TickDuration = 1f / TargetFps;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        // VSync follows the display refresh (often 120/144). Off so the FPS cap holds.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFps;
        Time.fixedDeltaTime = TickDuration;
        Time.maximumDeltaTime = TickDuration * 4f;
        Time.maximumParticleDeltaTime = TickDuration;
    }
}
