#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Always enter Play Mode from MainMenu, even if SampleScene is open in the editor.
/// </summary>
[InitializeOnLoad]
public static class PlayModeStartScene
{
    const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

    static PlayModeStartScene()
    {
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
        if (scene != null)
            EditorSceneManager.playModeStartScene = scene;
    }

    [MenuItem("RoboCarl/Use Main Menu As Play Mode Scene")]
    static void ApplyManually()
    {
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
        if (scene == null)
        {
            Debug.LogError($"Could not find scene at {MainMenuPath}");
            return;
        }

        EditorSceneManager.playModeStartScene = scene;
        Debug.Log("Play Mode will now start on MainMenu.");
    }
}
#endif
