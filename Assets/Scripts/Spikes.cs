using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Spikes: hitting the pointed face returns to the main menu; hitting from the side
/// knocks Carl away at a shallow angle and locks his decisions briefly.
/// Floor spikes point up; ceiling spikes (<see cref="faceDown"/>) point down.
/// </summary>
public class Spikes : MonoBehaviour
{
    const string MenuSceneName = "MainMenu";
    const float SideLaunchSpeed = 4.6f;
    const float SideLaunchAngleDegrees = 25f;
    const float DecisionLockSeconds = 1f;
    const float DeadlyNormalThreshold = 0.55f;

    static readonly Color BaseColor = new(0.22f, 0.2f, 0.22f, 1f);
    static readonly Color SpikeColor = new(0.72f, 0.72f, 0.76f, 1f);
    static readonly Color TipColor = new(0.9f, 0.9f, 0.94f, 1f);

    [SerializeField] float width = 1.0f;
    [SerializeField] float height = 0.42f;
    [SerializeField] bool faceDown;

    bool _triggered;

    public bool FaceDown => faceDown;
    public Vector2 Size => new(width, height);

    public static Spikes Spawn(Vector2 position, float width = 1.0f, float height = 0.42f, bool faceDown = false)
    {
        var spikesObject = new GameObject("Spikes");
        spikesObject.SetActive(false);
        spikesObject.transform.position = new Vector3(position.x, position.y, 0f);

        var spikes = spikesObject.AddComponent<Spikes>();
        spikes.width = width;
        spikes.height = height;
        spikes.faceDown = faceDown;
        spikesObject.SetActive(true);
        return spikes;
    }

    void Awake()
    {
        BuildVisual();

        var collider = gameObject.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width, height);
        collider.offset = Vector2.zero;
        collider.isTrigger = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (_triggered)
            return;

        var locomotion = collision.collider.GetComponentInParent<CarlLocomotion>();
        if (locomotion == null)
            return;

        if (!TryGetDeadliestContact(collision, out var normal))
            return;

        // Floor spikes: Carl above → normal into spikes points down.
        // Ceiling spikes: Carl below → normal into spikes points up.
        bool deadly = faceDown
            ? normal.y >= DeadlyNormalThreshold
            : normal.y <= -DeadlyNormalThreshold;
        if (deadly)
        {
            KillCarl();
            return;
        }

        if (!locomotion.CanMakeDecisions)
            return;

        // Side hit: knock away from the spike bed.
        float away = Mathf.Sign(locomotion.transform.position.x - transform.position.x);
        if (Mathf.Approximately(away, 0f))
            away = normal.x < 0f ? 1f : -1f;

        locomotion.SpikeKnockback(SideLaunchSpeed, SideLaunchAngleDegrees, away, DecisionLockSeconds);
    }

    bool TryGetDeadliestContact(Collision2D collision, out Vector2 normal)
    {
        normal = Vector2.zero;
        if (collision.contactCount <= 0)
            return false;

        // Prefer the strongest signal on the pointed face.
        float best = faceDown ? float.NegativeInfinity : float.PositiveInfinity;
        for (var i = 0; i < collision.contactCount; i++)
        {
            var n = collision.GetContact(i).normal;
            if (faceDown ? n.y > best : n.y < best)
            {
                best = n.y;
                normal = n;
            }
        }

        return true;
    }

    void KillCarl()
    {
        _triggered = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuSceneName);
    }

    void BuildVisual()
    {
        if (transform.Find("Visual") != null)
            return;

        var visualRoot = new GameObject("Visual").transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localPosition = Vector3.zero;
        if (faceDown)
            visualRoot.localScale = new Vector3(1f, -1f, 1f);

        AddPart(visualRoot, "Base", BaseColor, new Vector2(width, height * 0.22f), new Vector2(0f, -height * 0.39f), 0f, 2);

        int count = Mathf.Max(3, Mathf.RoundToInt(width / 0.28f));
        float spacing = width / count;
        float diamond = Mathf.Min(spacing * 0.85f, height * 0.95f);
        float y = height * 0.02f;

        for (var i = 0; i < count; i++)
        {
            float x = -width * 0.5f + spacing * (i + 0.5f);
            AddPart(visualRoot, $"Spike{i}", SpikeColor, new Vector2(diamond, diamond), new Vector2(x, y), 45f, 3);
            AddPart(
                visualRoot,
                $"Tip{i}",
                TipColor,
                new Vector2(diamond * 0.28f, diamond * 0.28f),
                new Vector2(x, y + diamond * 0.42f),
                45f,
                4);
        }
    }

    static void AddPart(
        Transform parent,
        string name,
        Color color,
        Vector2 partSize,
        Vector2 localPosition,
        float rotationZ,
        int sortingOrder)
    {
        var part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        part.transform.localScale = new Vector3(partSize.x, partSize.y, 1f);

        var renderer = part.AddComponent<SpriteRenderer>();
        GameSprites.ConfigureRenderer(renderer);
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }
}
