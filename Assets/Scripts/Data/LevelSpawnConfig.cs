using System;
using UnityEngine;

/// <summary>
/// Data-driven pickup, wall, button-wall, platform, spring, and spike spawns for a level/scene.
/// Fans/power buttons are currently spawned from Level3Layout (need live wire refs).
/// </summary>
[CreateAssetMenu(fileName = "LevelSpawnConfig", menuName = "RoboCarl/Level Spawn Config")]
public class LevelSpawnConfig : ScriptableObject
{
    [Serializable]
    public struct SpawnEntry
    {
        public PickupType type;
        public float delaySeconds;
        public Vector2 position;
    }

    [Serializable]
    public struct WallEntry
    {
        public Vector2 position;
        public Vector2 size;
        [Tooltip("Max travel from start along the positive length axis.")]
        public float dragPositive;
        [Tooltip("Max travel from start along the negative length axis.")]
        public float dragNegative;
        [Tooltip("If true, length is vertical (drag on Y). If false, drag on X.")]
        public bool vertical;
    }

    [Serializable]
    public struct PlatformEntry
    {
        public Vector2 position;
        public Vector2 size;
    }

    [Serializable]
    public struct SpringEntry
    {
        public Vector2 position;
        public float width;
        public float height;
    }

    [Serializable]
    public struct SpikeEntry
    {
        public Vector2 position;
        public float width;
        public float height;
    }

    [SerializeField] SpawnEntry[] pickups =
    {
        new() { type = PickupType.Oil, delaySeconds = 0f, position = new Vector2(-1.8f, -3.52f) },
        new() { type = PickupType.Energy, delaySeconds = 0f, position = new Vector2(3.7f, -3.52f) }
    };

    [SerializeField] WallEntry[] walls = Array.Empty<WallEntry>();
    [SerializeField] WallEntry[] buttonWalls = Array.Empty<WallEntry>();
    [SerializeField] PlatformEntry[] platforms = Array.Empty<PlatformEntry>();
    [SerializeField] SpringEntry[] springs = Array.Empty<SpringEntry>();
    [SerializeField] SpikeEntry[] spikes = Array.Empty<SpikeEntry>();

    public SpawnEntry[] Pickups => pickups;
    public WallEntry[] Walls => walls;
    public WallEntry[] ButtonWalls => buttonWalls;
    public PlatformEntry[] Platforms => platforms;
    public SpringEntry[] Springs => springs;
    public SpikeEntry[] Spikes => spikes;
}
