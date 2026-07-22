using UnityEngine;

/// <summary>
/// Ensures level props exist; pickup spawning runs on CarlResources.
/// </summary>
public class GameDirector : MonoBehaviour
{
    [SerializeField] bool disableWinLine;
    [SerializeField] LevelSpawnConfig spawnConfig;

    void Awake()
    {
        if (LevelSession.SelectedLevel == 2)
        {
            disableWinLine = true;
            Level2Layout.Apply();
        }

        if (!disableWinLine)
            WinLine.EnsureExists();

        SpawnWalls();
    }

    void Start()
    {
        if (FindFirstObjectByType<CarlResources>() != null)
            return;

        ResourcePickup.Spawn(PickupType.Oil, new Vector2(-1.8f, -6.88f));
        ResourcePickup.Spawn(PickupType.Energy, new Vector2(3.7f, -6.88f));
    }

    public void ConfigureForLevel2(LevelSpawnConfig level2Config)
    {
        disableWinLine = true;
        if (level2Config != null)
            spawnConfig = level2Config;

        var carl = FindFirstObjectByType<CarlResources>();
        if (carl != null && level2Config != null)
            carl.SetSpawnConfig(level2Config);
    }

    void SpawnWalls()
    {
        var config = spawnConfig;
        if (config == null)
        {
            var carl = FindFirstObjectByType<CarlResources>();
            if (carl != null)
                config = carl.SpawnConfig;
        }

        if (config == null || config.Walls == null)
            return;

        foreach (var entry in config.Walls)
        {
            DraggableWall.Spawn(
                entry.position,
                entry.size,
                entry.dragPositive,
                entry.dragNegative,
                entry.vertical);
        }
    }
}
