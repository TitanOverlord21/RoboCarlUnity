using UnityEngine;

/// <summary>
/// Converts the shared SampleScene base into Level 4: full floor, strip the
/// Level 1 ground spring and WinLine; Carl starts on a sideways draggable platform.
/// Props come from Level4Spawns.
/// </summary>
public static class Level4Layout
{
    const string Level4SpawnResource = "Level4Spawns";

    // Keep in sync with Level4Spawns start platform.
    const float PlatformCenterX = 0.8f;
    const float PlatformCenterY = -0.9f;
    const float PlatformHeight = 0.35f;
    // Slightly right of platform center so the side spring knocks him fully off.
    const float CarlStartX = 1.15f;

    public static void Apply()
    {
        DestroyAllByName("Spring");
        DestroyByName("FloorRight");
        DestroyAllByName("WinLine");

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
            director.ConfigureSpawnConfig(Resources.Load<LevelSpawnConfig>(Level4SpawnResource));
    }

    /// <summary>
    /// Call after the sideways platform has spawned so Carl's feet sit on its top.
    /// </summary>
    public static void PlaceCarlOnStartPlatform()
    {
        var carl = Object.FindAnyObjectByType<CarlLocomotion>();
        if (carl == null)
            return;

        float platformTop = PlatformCenterY + PlatformHeight * 0.5f;
        var wall = FindStartPlatform();
        if (wall != null)
        {
            var wallCol = wall.GetComponent<Collider2D>();
            if (wallCol != null)
                platformTop = wallCol.bounds.max.y;
        }

        var body = carl.GetComponent<Rigidbody2D>();
        var col = carl.GetComponent<Collider2D>();
        float feetFromCenter = col != null ? col.bounds.extents.y : 0.5f;
        float newY = platformTop + feetFromCenter + 0.02f;

        carl.transform.position = new Vector3(CarlStartX, newY, 0f);
        if (body != null)
        {
            body.position = new Vector2(CarlStartX, newY);
            body.linearVelocity = Vector2.zero;
        }
    }

    static DraggableWall FindStartPlatform()
    {
        var walls = Object.FindObjectsByType<DraggableWall>();
        DraggableWall best = null;
        float bestDist = float.MaxValue;
        for (var i = 0; i < walls.Length; i++)
        {
            var wall = walls[i];
            if (wall == null)
                continue;
            float dist = Mathf.Abs(wall.transform.position.x - PlatformCenterX)
                + Mathf.Abs(wall.transform.position.y - PlatformCenterY);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = wall;
            }
        }

        return best;
    }

    static void DestroyByName(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go != null)
            Object.Destroy(go);
    }

    static void DestroyAllByName(string objectName)
    {
        while (true)
        {
            var go = GameObject.Find(objectName);
            if (go == null)
                break;
            Object.Destroy(go);
            go.name = $"{objectName}__Destroyed";
        }
    }
}
