using System;
using UnityEngine;

/// <summary>
/// Data-driven pickup and wall spawns for a level/scene.
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

    [SerializeField] SpawnEntry[] pickups =
    {
        new() { type = PickupType.Oil, delaySeconds = 0f, position = new Vector2(-1.8f, -6.88f) },
        new() { type = PickupType.Energy, delaySeconds = 0f, position = new Vector2(3.7f, -6.88f) }
    };

    [SerializeField] WallEntry[] walls = Array.Empty<WallEntry>();

    public SpawnEntry[] Pickups => pickups;
    public WallEntry[] Walls => walls;
}
