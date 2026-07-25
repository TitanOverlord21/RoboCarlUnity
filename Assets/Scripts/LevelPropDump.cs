using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor Play Mode helper: overwrites <c>LevelDumps/LevelN.txt</c> with live
/// world positions/bounds of level props. One file per level; each run replaces
/// the previous contents (never appends).
/// </summary>
public static class LevelPropDump
{
    const string MenuSceneName = "MainMenu";
    const string FolderName = "LevelDumps";

    /// <summary>
    /// Writes (overwrites) the dump for <see cref="LevelSession.SelectedLevel"/>.
    /// No-op outside the Editor, or on the main menu scene.
    /// </summary>
    public static void WriteActiveLevel()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return;

        var scene = SceneManager.GetActiveScene().name;
        if (scene == MenuSceneName)
            return;

        int level = Mathf.Max(1, LevelSession.SelectedLevel);
        var text = BuildDump(level, scene);
        var path = GetDumpPath(level);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, text, Encoding.UTF8);
        Debug.Log($"LevelPropDump wrote {FolderName}/Level{level}.txt (overwrite)");
#endif
    }

    public static string GetDumpPath(int level)
    {
        var projectRoot = Path.GetDirectoryName(Application.dataPath);
        return Path.Combine(projectRoot ?? ".", FolderName, $"Level{level}.txt");
    }

#if UNITY_EDITOR
    static string BuildDump(int level, string sceneName)
    {
        var sb = new StringBuilder(4096);
        sb.AppendLine($"RoboCarl level prop dump");
        sb.AppendLine($"level={level}");
        sb.AppendLine($"scene={sceneName}");
        sb.AppendLine($"time={System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"note=This file is overwritten each Play of this level (not appended).");
        sb.AppendLine();

        AppendSection(sb, "Carl");
        var carl = Object.FindAnyObjectByType<CarlLocomotion>();
        if (carl != null)
            AppendTransform(sb, carl.gameObject, extra: $"grounded={carl.IsGrounded}");
        else
            sb.AppendLine("  (none)");

        AppendSection(sb, "WinLine");
        foreach (var win in Object.FindObjectsByType<WinLine>())
            AppendTransform(sb, win.gameObject);

        AppendSection(sb, "Floors (named)");
        AppendNamed(sb, "Floor");
        AppendNamed(sb, "FloorLeft");
        AppendNamed(sb, "FloorRight");
        AppendNamed(sb, "FloorCeilingLeft");

        AppendSection(sb, "Springs");
        foreach (var spring in SortByX(Object.FindObjectsByType<SpringPad>()))
            AppendTransform(sb, spring.gameObject, extra: $"size={Fmt(spring.Size)} launchVy={SpringPad.LaunchVelocityY:0.###}");

        AppendSection(sb, "OneWayPlatforms");
        foreach (var platform in SortByX(Object.FindObjectsByType<OneWayPlatform>()))
            AppendTransform(sb, platform.gameObject);

        AppendSection(sb, "Spikes");
        foreach (var spikes in SortByX(Object.FindObjectsByType<Spikes>()))
            AppendTransform(sb, spikes.gameObject, extra: $"faceDown={spikes.FaceDown} size={Fmt(spikes.Size)}");

        AppendSection(sb, "DraggableWalls");
        foreach (var wall in SortByX(Object.FindObjectsByType<DraggableWall>()))
            AppendTransform(sb, wall.gameObject);

        AppendSection(sb, "ButtonWalls");
        foreach (var wall in SortByX(Object.FindObjectsByType<ButtonWall>()))
            AppendTransform(sb, wall.gameObject, extra: $"showButton={wall.ShowButton} size={Fmt(wall.Size)}");

        AppendSection(sb, "LinkedWallButtons");
        foreach (var button in SortByX(Object.FindObjectsByType<LinkedWallButton>()))
            AppendTransform(sb, button.gameObject);

        AppendSection(sb, "PoweredFans");
        foreach (var fan in SortByX(Object.FindObjectsByType<PoweredFan>()))
        {
            AppendTransform(
                sb,
                fan.gameObject,
                extra: $"facing={fan.BlowFacing} powered={fan.IsPowered} housingDepth={fan.HousingDepth:0.###}");
        }

        AppendSection(sb, "PowerButtons");
        foreach (var button in SortByX(Object.FindObjectsByType<PowerButton>()))
        {
            var parent = button.transform.parent != null ? button.transform.parent.name : "(none)";
            var targets = DescribeTargets(button);
            AppendTransform(
                sb,
                button.gameObject,
                extra: $"parent={parent} local={Fmt((Vector2)button.transform.localPosition)} powered={button.IsPowered} targets=[{targets}]");
        }

        AppendSection(sb, "Pickups");
        foreach (var pickup in SortByX(Object.FindObjectsByType<ResourcePickup>()))
            AppendTransform(sb, pickup.gameObject, extra: $"type={pickup.Type}");

        sb.AppendLine();
        sb.AppendLine("End of dump.");
        return sb.ToString();
    }

    static void AppendSection(StringBuilder sb, string title)
    {
        sb.AppendLine($"## {title}");
    }

    static void AppendNamed(StringBuilder sb, string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go == null)
        {
            sb.AppendLine($"  {objectName}: (not found)");
            return;
        }

        AppendTransform(sb, go);
    }

    static void AppendTransform(StringBuilder sb, GameObject go, string extra = null)
    {
        var t = go.transform;
        var pos = (Vector2)t.position;
        var line = $"  {go.name}: pos={Fmt(pos)}";

        var col = go.GetComponent<Collider2D>();
        if (col != null)
        {
            var b = col.bounds;
            line += $" boundsMin={Fmt((Vector2)b.min)} boundsMax={Fmt((Vector2)b.max)}";
        }

        if (!string.IsNullOrEmpty(extra))
            line += $" {extra}";

        sb.AppendLine(line);
    }

    static string DescribeTargets(PowerButton button)
    {
        if (button.Targets == null || button.Targets.Count == 0)
            return "";

        var parts = new List<string>(button.Targets.Count);
        for (var i = 0; i < button.Targets.Count; i++)
        {
            var target = button.Targets[i];
            if (target == null)
            {
                parts.Add("null");
                continue;
            }

            var mb = target as MonoBehaviour;
            parts.Add(mb != null ? mb.name : target.GetType().Name);
        }

        return string.Join(", ", parts);
    }

    static T[] SortByX<T>(T[] items) where T : Component
    {
        if (items == null || items.Length <= 1)
            return items ?? System.Array.Empty<T>();

        System.Array.Sort(items, (a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            int cmp = a.transform.position.x.CompareTo(b.transform.position.x);
            if (cmp != 0) return cmp;
            return a.transform.position.y.CompareTo(b.transform.position.y);
        });
        return items;
    }

    static string Fmt(Vector2 v) => $"({v.x:0.###}, {v.y:0.###})";
#endif
}
