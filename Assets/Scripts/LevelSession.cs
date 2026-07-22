using UnityEngine;

/// <summary>
/// Which level the menu asked to play. Levels may share a base scene and
/// apply layout differences at runtime.
/// </summary>
public static class LevelSession
{
    public static int SelectedLevel { get; set; } = 1;
}
