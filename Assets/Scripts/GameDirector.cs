using System.Collections;
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
        // Each level binds its own spawn asset; scene objects are only the shared base.
        switch (LevelSession.SelectedLevel)
        {
            case 2:
                Level2Layout.Apply();
                break;
            case 3:
                Level3Layout.Apply();
                break;
            case 4:
                Level4Layout.Apply();
                break;
            default:
                Level1Layout.Apply();
                break;
        }

        // Level 4 strips the scene WinLine for now (goal will sit much higher later).
        if (!disableWinLine && LevelSession.SelectedLevel != 4)
            WinLine.EnsureExists();

        SpawnLevelProps();

        if (LevelSession.SelectedLevel == 3)
            Level3Layout.SpawnDoorLinkButton();

        if (LevelSession.SelectedLevel == 4)
            Level4Layout.PlaceCarlOnStartPlatform();
    }

    void Start()
    {
        if (FindAnyObjectByType<CarlResources>() == null)
        {
            ResourcePickup.Spawn(PickupType.Oil, new Vector2(-1.8f, -3.52f));
            ResourcePickup.Spawn(PickupType.Energy, new Vector2(3.7f, -3.52f));
        }

        // Pickups spawn in CarlResources.Start; wait one frame so the dump is complete.
        // L4 re-places Carl after SnapFeetToGround so he stays on the start platform.
        StartCoroutine(WriteLevelDumpNextFrame());
    }

    IEnumerator WriteLevelDumpNextFrame()
    {
        if (LevelSession.SelectedLevel == 4)
            Level4Layout.PlaceCarlOnStartPlatform();

        yield return null;

        if (LevelSession.SelectedLevel == 4)
            Level4Layout.PlaceCarlOnStartPlatform();

        LevelPropDump.WriteActiveLevel();
    }

    public void ConfigureSpawnConfig(LevelSpawnConfig config)
    {
        if (config != null)
            spawnConfig = config;

        var carl = FindAnyObjectByType<CarlResources>();
        if (carl != null && config != null)
            carl.SetSpawnConfig(config);
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
                    entry.vertical,
                    showButton: !entry.hideButton);
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
                SpringPad.Spawn(entry.position, width, height, entry.facing, entry.launchSpeed);
            }
        }

        if (config.Spikes != null)
        {
            foreach (var entry in config.Spikes)
            {
                float width = entry.width > 0f ? entry.width : 1.0f;
                float height = entry.height > 0f ? entry.height : 0.42f;
                Spikes.Spawn(entry.position, width, height, faceDown: entry.faceDown);
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
