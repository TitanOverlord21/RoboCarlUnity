using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Converts the shared SampleScene base into Level 3: full floor, no ground
/// spring; button-wall corridor (linked button only), side springs/one-ways,
/// left ceiling-spike trap, side fans, and a right upward fan to the win line.
/// </summary>
public static class Level3Layout
{
    const string Level3SpawnResource = "Level3Spawns";

    // Playfield half-width is 4.5; side fans sit on the ends with pickups in front.
    const float LeftFanX = -4.1f;
    const float RightFanX = 4.1f;
    const float FanY = -3.0f;

    // Past the ±1.15 corridor doors.
    const float SidePlatformX = 2.7f;
    const float UpFanX = 3.35f;
    const float OneWayY = -1.66f;
    const float CeilingFloorY = -0.45f;
    const float CeilingFloorWidth = 2.2f;
    const float CeilingFloorThickness = 0.08f;

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

        SpawnLeftCeilingFloor();
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

    static void SpawnLeftCeilingFloor()
    {
        // Solid deck the left downward spikes hang from (no floating spikes).
        var go = new GameObject("FloorCeilingLeft");
        go.transform.position = new Vector3(-SidePlatformX, CeilingFloorY, 0f);

        var renderer = go.AddComponent<SpriteRenderer>();
        GameSprites.ConfigureRenderer(renderer);
        renderer.color = Color.black;
        renderer.sortingOrder = 1;
        go.transform.localScale = new Vector3(CeilingFloorWidth, CeilingFloorThickness, 1f);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        col.offset = Vector2.zero;
        col.isTrigger = false;
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

        // Right fan on the right edge, blows left; power button just above with a short wire.
        var rightFan = PoweredFan.Spawn(new Vector2(RightFanX, FanY), faceRight: false, height: 1.55f, windRange: 5f);
        PowerButton.Spawn(
            new Vector2(RightFanX, -1.85f),
            new IPowerable[] { rightFan },
            drawWires: true,
            parent: null,
            buttonSize: 0.55f);

        // Upward fan under the right one-way — blows up through the deck to WinLine.
        // Button on the inward side so it doesn't sit in front of the right edge fan.
        float upFanY = OneWayY - 0.75f;
        var upFan = PoweredFan.Spawn(
            new Vector2(UpFanX, upFanY),
            PoweredFan.Facing.Up,
            height: 1.25f,
            windRange: 4.5f);
        PowerButton.Spawn(
            new Vector2(-0.7f, -0.05f),
            new IPowerable[] { upFan },
            drawWires: false,
            parent: upFan.transform,
            buttonSize: 0.48f);
    }

    static void DestroyByName(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go != null)
            Object.Destroy(go);
    }
}
