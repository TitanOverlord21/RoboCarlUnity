using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Full-screen lose overlay with a restart button (uGUI, touch-friendly).
/// Content is constrained to the same 9:19.5 mobile play area as gameplay.
/// </summary>
public class LoseOverlay : MonoBehaviour
{
    static LoseOverlay _instance;
    static EventSystem _eventSystem;
    static bool _visible;
    static string _message = "Out of energy!";

    Text _messageLabel;

    public static void Show(string message = "Out of energy!")
    {
        EnsureInstance();
        EnsureEventSystem();

        _message = message;
        _visible = true;
        Time.timeScale = 0f;

        if (_instance._messageLabel != null)
            _instance._messageLabel.text = _message;

        _instance.gameObject.SetActive(true);
    }

    static void EnsureInstance()
    {
        if (_instance != null)
            return;

        var overlayObject = new GameObject(nameof(LoseOverlay));
        DontDestroyOnLoad(overlayObject);
        _instance = overlayObject.AddComponent<LoseOverlay>();
        _instance.BuildUi();
    }

    static void EnsureEventSystem()
    {
        if (_eventSystem != null)
            return;

        _eventSystem = FindAnyObjectByType<EventSystem>();
        if (_eventSystem != null)
        {
            DontDestroyOnLoad(_eventSystem.gameObject);
            return;
        }

        var eventSystemObject = new GameObject("EventSystem");
        DontDestroyOnLoad(eventSystemObject);
        _eventSystem = eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    void BuildUi()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(900f, 1950f);
        scaler.matchWidthOrHeight = 1f;

        gameObject.AddComponent<GraphicRaycaster>();

        var playRoot = MenuUi.CreateMobilePlayArea(transform, new Color(0f, 0f, 0f, 0.01f));

        var dim = MenuUi.Create("Dim", playRoot);
        var dimImage = MenuUi.AddImage(dim, new Color(0f, 0f, 0f, 0.6f));
        MenuUi.StretchFull(dim.GetComponent<RectTransform>());

        var messageObject = MenuUi.Create("Message", playRoot);
        _messageLabel = MenuUi.AddText(messageObject, _message, 48, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        MenuUi.SetAnchors(messageObject.GetComponent<RectTransform>(), new Vector2(0.08f, 0.55f), new Vector2(0.92f, 0.75f));

        var buttonObject = MenuUi.Create("RestartButton", playRoot);
        MenuUi.SetAnchors(buttonObject.GetComponent<RectTransform>(), new Vector2(0.2f, 0.32f), new Vector2(0.8f, 0.42f));
        var buttonImage = MenuUi.AddImage(buttonObject, new Color(0.25f, 0.55f, 0.85f, 1f));
        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(Restart);

        var labelObject = MenuUi.Create("Label", buttonObject.transform);
        MenuUi.AddText(labelObject, "Restart", 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        MenuUi.StretchFull(labelObject.GetComponent<RectTransform>());

        gameObject.SetActive(_visible);
    }

    void Restart()
    {
        Time.timeScale = 1f;
        _visible = false;
        gameObject.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
