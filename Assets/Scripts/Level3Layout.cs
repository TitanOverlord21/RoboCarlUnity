using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Converts the shared SampleScene base into Level 3: full floor, no ground
/// spring; test layout with button-wall corridor, pickups, and powered fans.
/// </summary>
public static class Level3Layout
{
    const string Level3SpawnResource = "Level3Spawns";

    // Playfield half-width is 4.5; fans sit on the ends with pickups in front.
    const float LeftFanX = -4.1f;
    const float RightFanX = 4.1f;
    const float FanY = -3.0f;

    public static void Apply()
    {
        DestroyByName("Spring");
        DestroyByName("FloorRight");

        var floorLeft = GameObject.Find("FloorLeft");
        if (floorLeft != null)
        {
            floorLeft.name = "Floor";
            floorLeft.transform.position = new Vector3(0f, -3.84f, 0f);
            floorLeft.transform.localScale = new Vector3(8.5f, 0.08f, 1f);
            var col = floorLeft.GetComponent<BoxCollider2D>();
            if (col != null)
                col.size = new Vector2(8.5f, 0.08f);

            var renderer = floorLeft.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.color = Color.black;
        }

        var director = Object.FindAnyObjectByType<GameDirector>();
        if (director != null)
            director.ConfigureSpawnConfig(Resources.Load<LevelSpawnConfig>(Level3SpawnResource));

        SpawnFansAndPowerButtons();
    }

    /// <summary>
    /// Call after button walls are spawned so both doors can be wired.
    /// </summary>
    public static void SpawnDoorLinkButton()
    {
        var walls = Object.FindObjectsByType<ButtonWall>();
        if (walls == null || walls.Length == 0)
            return;

        // Prefer the two L3 corridor doors (nearest |x|~1.15).
        var linked = new List<ButtonWall>(2);
        System.Array.Sort(walls, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        for (var i = 0; i < walls.Length; i++)
        {
            if (walls[i] != null)
                linked.Add(walls[i]);
        }

        if (linked.Count == 0)
            return;

        LinkedWallButton.Spawn(new Vector2(0f, -0.55f), linked, buttonSize: 0.62f);
    }

    static void SpawnFansAndPowerButtons()
    {
        // Left fan on the left edge, blows right; power button on top (back edge is off-screen).
        var leftFan = PoweredFan.Spawn(new Vector2(LeftFanX, FanY), faceRight: true, height: 1.55f, windRange: 5f);
        PowerButton.Spawn(
            new Vector2(0.2f, 0.95f),
            new IPowerable[] { leftFan },
            drawWires: false,
            parent: leftFan.transform,
            buttonSize: 0.52f);

        // Right fan on the right edge, blows left; power button above with a wire.
        var rightFan = PoweredFan.Spawn(new Vector2(RightFanX, FanY), faceRight: false, height: 1.55f, windRange: 5f);
        PowerButton.Spawn(
            new Vector2(RightFanX, -0.55f),
            new IPowerable[] { rightFan },
            drawWires: true,
            parent: null,
            buttonSize: 0.55f);
    }

    static void DestroyByName(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go != null)
            Object.Destroy(go);
    }
}
