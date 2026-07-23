using UnityEngine;

/// <summary>
/// Converts the shared SampleScene base into Level 2: blank floor, no
/// spring/win line/pickups, plus the raised draggable wall.
/// </summary>
public static class Level2Layout
{
    const string Level2SpawnResource = "Level2Spawns";

    public static void Apply()
    {
        DestroyByName("Spring");
        DestroyByName("WinLine");
        DestroyByName("FloorRight");

        var floorLeft = GameObject.Find("FloorLeft");
        if (floorLeft != null)
        {
            floorLeft.name = "Floor";
            floorLeft.transform.position = new Vector3(0f, -3.84f, 0f);
            // SpriteQuad already applied size in Awake; rescale visual + collider.
            floorLeft.transform.localScale = new Vector3(8.5f, 0.08f, 1f);
            var col = floorLeft.GetComponent<BoxCollider2D>();
            if (col != null)
                col.size = new Vector2(8.5f, 0.08f);

            var renderer = floorLeft.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.color = Color.black;
        }

        var director = Object.FindFirstObjectByType<GameDirector>();
        if (director != null)
            director.ConfigureForLevel2(Resources.Load<LevelSpawnConfig>(Level2SpawnResource));
    }

    static void DestroyByName(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go != null)
            Object.Destroy(go);
    }
}
