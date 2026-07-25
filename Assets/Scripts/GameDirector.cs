using UnityEngine;

/// <summary>
/// Ensures level props exist; pickups spawn from CarlResources.
/// </summary>
public class GameDirector : MonoBehaviour
{
    [SerializeField] bool disableWinLine;
    [SerializeField] LevelSpawnConfig spawnConfig;

    void Awake()
    {
        if (LevelSession.SelectedLevel == 2)
            Level2Layout.Apply();

        if (!disableWinLine)
            WinLine.EnsureExists();

        SpawnLevelProps();
    }

    void Start()
    {
        if (FindAnyObjectByType<CarlResources>() != null)
            return;

        ResourcePickup.Spawn(PickupType.Oil, new Vector2(-1.8f, -3.52f));
        ResourcePickup.Spawn(PickupType.Energy, new Vector2(3.7f, -3.52f));
    }

    public void ConfigureForLevel2(LevelSpawnConfig level2Config)
    {
        if (level2Config != null)
            spawnConfig = level2Config;

        var carl = FindAnyObjectByType<CarlResources>();
        if (carl != null && level2Config != null)
            carl.SetSpawnConfig(level2Config);
    }

    void SpawnLevelProps()
    {
        var config = ResolveSpawnConfig();
        if (config == null)
            return;

        if (config.Walls != null)
        {
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

        if (config.ButtonWalls != null)
        {
            foreach (var entry in config.ButtonWalls)
            {
                ButtonWall.Spawn(
                    entry.position,
                    entry.size,
                    entry.dragPositive,
                    entry.dragNegative,
                    entry.vertical);
            }
        }

        if (config.Platforms != null)
        {
            foreach (var entry in config.Platforms)
                OneWayPlatform.Spawn(entry.position, entry.size);
        }

        if (config.Springs != null)
        {
            foreach (var entry in config.Springs)
            {
                float width = entry.width > 0f ? entry.width : 1.2f;
                float height = entry.height > 0f ? entry.height : 0.35f;
                SpringPad.Spawn(entry.position, width, height);
            }
        }

        if (config.Spikes != null)
        {
            foreach (var entry in config.Spikes)
            {
                float width = entry.width > 0f ? entry.width : 1.0f;
                float height = entry.height > 0f ? entry.height : 0.42f;
                Spikes.Spawn(entry.position, width, height);
            }
        }
    }

    LevelSpawnConfig ResolveSpawnConfig()
    {
        if (spawnConfig != null)
            return spawnConfig;

        var carl = FindAnyObjectByType<CarlResources>();
        return carl != null ? carl.SpawnConfig : null;
    }
}
