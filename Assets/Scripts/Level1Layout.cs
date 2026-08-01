using UnityEngine;

/// <summary>
/// Level 1 on the shared SampleScene base: keep scene floors / ground spring /
/// WinLine / Carl; bind <c>Level1Spawns</c> (pickups, one-way, platform spring).
/// </summary>
public static class Level1Layout
{
    const string Level1SpawnResource = "Level1Spawns";

    public static void Apply()
    {
        var director = Object.FindAnyObjectByType<GameDirector>();
        if (director != null)
            director.ConfigureSpawnConfig(Resources.Load<LevelSpawnConfig>(Level1SpawnResource));
    }
}
