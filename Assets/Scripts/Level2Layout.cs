using UnityEngine;

/// <summary>
/// Converts the shared SampleScene base into Level 2: full floor, no ground
/// spring; win line kept. Props come from Level2Spawns.
/// </summary>
public static class Level2Layout
{
    const string Level2SpawnResource = "Level2Spawns";

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
            director.ConfigureForLevel2(Resources.Load<LevelSpawnConfig>(Level2SpawnResource));
    }

    static void DestroyByName(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go != null)
            Object.Destroy(go);
    }
}
