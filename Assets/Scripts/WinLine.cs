using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Green horizontal goal line. Touching it exits the level back to the main menu.
/// </summary>
public class WinLine : MonoBehaviour
{
    public const float DefaultHeight = -2.05f;

    const string MenuSceneName = "MainMenu";

    static readonly Color LineColor = new(0.2f, 1f, 0.35f, 1f);

    [SerializeField] float width = AspectRatioCamera.WorldWidth * 0.92f;
    [SerializeField] float thickness = 0.12f;
    [SerializeField] float triggerHeight = 0.4f;

    bool _triggered;

    public static WinLine EnsureExists(float y = DefaultHeight)
    {
        var existing = FindFirstObjectByType<WinLine>();
        if (existing != null)
            return existing;

        var winLineObject = new GameObject("WinLine");
        winLineObject.transform.position = new Vector3(0f, y, 0f);
        return winLineObject.AddComponent<WinLine>();
    }

    void Awake()
    {
        BuildVisual();

        var collider = gameObject.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        // Taller than the drawn line so a fast spring bounce still registers.
        collider.size = new Vector2(width, triggerHeight);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered)
            return;

        if (other.GetComponent<CarlLocomotion>() == null &&
            other.GetComponentInParent<CarlLocomotion>() == null)
            return;

        _triggered = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuSceneName);
    }

    void BuildVisual()
    {
        if (transform.Find("Line") != null)
            return;

        var line = new GameObject("Line");
        line.transform.SetParent(transform, false);
        line.transform.localPosition = Vector3.zero;
        line.transform.localScale = new Vector3(width, thickness, 1f);

        var renderer = line.AddComponent<SpriteRenderer>();
        GameSprites.ConfigureRenderer(renderer);
        renderer.color = LineColor;
        renderer.sortingOrder = 20;
    }
}
