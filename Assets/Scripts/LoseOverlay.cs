using UnityEngine;

/// <summary>
/// Full-screen lose message using OnGUI so it works without canvas setup.
/// </summary>
public class LoseOverlay : MonoBehaviour
{
    static LoseOverlay _instance;
    static bool _visible;
    static string _message = "Out of energy!";

    public static void Show(string message = "Out of energy!")
    {
        EnsureInstance();
        _message = message;
        _visible = true;
        Time.timeScale = 0f;
    }

    static void EnsureInstance()
    {
        if (_instance != null)
            return;

        var overlayObject = new GameObject(nameof(LoseOverlay));
        _instance = overlayObject.AddComponent<LoseOverlay>();
    }

    void OnGUI()
    {
        if (!_visible)
            return;

        var fullScreen = new Rect(0f, 0f, Screen.width, Screen.height);

        var previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(fullScreen, Texture2D.whiteTexture);
        GUI.color = Color.white;

        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Max(28, Mathf.RoundToInt(Screen.height * 0.055f)),
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        style.normal.textColor = Color.white;

        GUI.Label(fullScreen, _message, style);
        GUI.color = previousColor;
    }
}
