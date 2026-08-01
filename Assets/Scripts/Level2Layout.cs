using UnityEngine;

/// <summary>
/// Converts the shared SampleScene base into Level 2: full floor, strip the
/// Level 1 ground spring; win line kept. All L2 props come from Level2Spawns
/// (not from Level1Spawns / scene leftovers).
/// </summary>
public static class Level2Layout
{
    const string Level2SpawnResource = "Level2Spawns";

    public static void Apply()
    {
        DestroyAllByName("Spring");
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
            director.ConfigureSpawnConfig(Resources.Load<LevelSpawnConfig>(Level2SpawnResource));
    }

    static void DestroyByName(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go != null)
            Object.Destroy(go);
    }

    static void DestroyAllByName(string objectName)
    {
        // Scene may only have one, but clear every match before L2 spawns its own.
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
