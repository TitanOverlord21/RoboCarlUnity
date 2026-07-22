using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Shared runtime uGUI helpers for portrait mobile menus.
/// </summary>
public static class MenuUi
{
    static Sprite _whiteSprite;
    static Font _font;

    public static Font Font
    {
        get
        {
            if (_font != null)
                return _font;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _font;
        }
    }

    public static Sprite WhiteSprite
    {
        get
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            var texture = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            _whiteSprite.name = "MenuUi_White";
            return _whiteSprite;
        }
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    public static Canvas CreateCanvas(string name, int sortingOrder = 0)
    {
        var canvasObject = new GameObject(name, typeof(RectTransform));
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // Same reference as the gameplay playfield (900x1950 world-mapped).
        scaler.referenceResolution = new Vector2(900f, 1950f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f; // Prefer height — phone is tall.
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    /// <summary>
    /// Creates black letterbox bars plus a centered play root locked to the same
    /// 9:19.5 aspect as <see cref="AspectRatioCamera"/>. Put all menu UI under the returned transform.
    /// </summary>
    public static RectTransform CreateMobilePlayArea(Transform canvasRoot, Color playBackground)
    {
        var letterbox = Create("Letterbox", canvasRoot);
        AddImage(letterbox, Color.black);
        StretchFull(letterbox.GetComponent<RectTransform>());

        var playArea = Create("MobilePlayArea", canvasRoot);
        var playRect = playArea.GetComponent<RectTransform>();
        playRect.anchorMin = new Vector2(0.5f, 0.5f);
        playRect.anchorMax = new Vector2(0.5f, 0.5f);
        playRect.pivot = new Vector2(0.5f, 0.5f);
        playRect.sizeDelta = new Vector2(900f, 1950f);

        var fitter = playArea.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = AspectRatioCamera.TargetAspect;

        AddImage(playArea, playBackground);
        return playRect;
    }

    public static GameObject Create(string name, Transform parent)
    {
        var uiObject = new GameObject(name, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    public static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2? offsetMin = null, Vector2? offsetMax = null)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin ?? Vector2.zero;
        rect.offsetMax = offsetMax ?? Vector2.zero;
    }

    public static Image AddImage(GameObject go, Color color, Sprite sprite = null)
    {
        var image = go.AddComponent<Image>();
        image.sprite = sprite != null ? sprite : WhiteSprite;
        image.color = color;
        image.type = Image.Type.Simple;
        return image;
    }

    public static Text AddText(
        GameObject go,
        string content,
        int fontSize,
        Color color,
        TextAnchor align = TextAnchor.MiddleCenter,
        FontStyle style = FontStyle.Normal)
    {
        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = Font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = align;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = false;
        return text;
    }

    public static Button CreateFilledButton(
        Transform parent,
        string name,
        string label,
        Color color,
        UnityEngine.Events.UnityAction onClick,
        int fontSize = 36)
    {
        var buttonObject = Create(name, parent);
        var image = AddImage(buttonObject, color);
        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var labelObject = Create("Label", buttonObject.transform);
        AddText(labelObject, label, fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        StretchFull(labelObject.GetComponent<RectTransform>());
        return button;
    }

    /// <summary>
    /// Compact top-left Back control for secondary menu screens.
    /// </summary>
    public static Button CreateTopLeftBack(Transform parent, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = Create("BackButton", parent);
        var rect = buttonObject.GetComponent<RectTransform>();
        SetAnchors(rect, new Vector2(0.03f, 0.92f), new Vector2(0.28f, 0.985f));

        var image = AddImage(buttonObject, new Color(0.35f, 0.4f, 0.48f, 1f));
        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var labelObject = Create("Label", buttonObject.transform);
        AddText(labelObject, "Back", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        StretchFull(labelObject.GetComponent<RectTransform>());
        return button;
    }

    public static Text CreateTopTitle(Transform parent, string title)
    {
        var header = Create("Header", parent);
        SetAnchors(header.GetComponent<RectTransform>(), new Vector2(0.3f, 0.92f), new Vector2(0.97f, 0.985f));
        return AddText(header, title, 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
    }

    public static GridLayoutGroup CreateGrid(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        int columns,
        int rows,
        Vector2 spacing)
    {
        var gridObject = Create(name, parent);
        SetAnchors(gridObject.GetComponent<RectTransform>(), anchorMin, anchorMax);

        var grid = gridObject.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.spacing = spacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.padding = new RectOffset(6, 6, 6, 6);

        var fitter = gridObject.AddComponent<FitGridToRect>();
        fitter.Columns = columns;
        fitter.Rows = rows;
        return grid;
    }

    /// <summary>
    /// Vertical ScrollRect whose content is a fixed-column grid that grows with more cards.
    /// Returns the content transform to parent grid children under.
    /// </summary>
    public static Transform CreateScrollableGrid(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        int columns,
        Vector2 spacing,
        float cellHeightRatio = 1.05f)
    {
        var scrollObject = Create(name, parent);
        SetAnchors(scrollObject.GetComponent<RectTransform>(), anchorMin, anchorMax);

        var scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        scroll.scrollSensitivity = 45f;

        var viewport = Create("Viewport", scrollObject.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        var viewportImage = AddImage(viewport, new Color(1f, 1f, 1f, 0.01f));
        viewportImage.raycastTarget = true;
        viewport.AddComponent<RectMask2D>();

        var content = Create("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        var grid = content.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.spacing = spacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.padding = new RectOffset(6, 6, 6, 20);

        var sizeFitter = content.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var columnFitter = content.AddComponent<FitScrollGridColumns>();
        columnFitter.Columns = columns;
        columnFitter.CellHeightRatio = cellHeightRatio;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        return content.transform;
    }
}

/// <summary>
/// Sizes a GridLayoutGroup so all cells fit inside the rect (portrait-safe).
/// </summary>
public class FitGridToRect : MonoBehaviour
{
    public int Columns = 2;
    public int Rows = 5;

    int _fitFrames = 3;
    Vector2 _lastSize;

    void OnEnable()
    {
        _fitFrames = 3;
        Fit();
    }

    void Start() => Fit();

    void LateUpdate()
    {
        var rect = transform as RectTransform;
        if (rect == null)
            return;

        var size = rect.rect.size;
        if (_fitFrames > 0 || size != _lastSize)
        {
            _fitFrames--;
            Fit();
        }
    }

    public void Fit()
    {
        var grid = GetComponent<GridLayoutGroup>();
        var rect = transform as RectTransform;
        if (grid == null || rect == null || Columns < 1 || Rows < 1)
            return;

        float width = rect.rect.width;
        float height = rect.rect.height;
        if (width < 1f || height < 1f)
            return;

        _lastSize = new Vector2(width, height);
        float innerW = width - grid.padding.horizontal - grid.spacing.x * (Columns - 1);
        float innerH = height - grid.padding.vertical - grid.spacing.y * (Rows - 1);
        grid.cellSize = new Vector2(Mathf.Max(1f, innerW / Columns), Mathf.Max(1f, innerH / Rows));
    }
}

/// <summary>
/// Sizes scrollable grid cells from content width so extra rows extend the scroll area.
/// </summary>
public class FitScrollGridColumns : MonoBehaviour
{
    public int Columns = 2;
    public float CellHeightRatio = 1.05f;

    int _fitFrames = 3;
    Vector2 _lastSize;

    void OnEnable()
    {
        _fitFrames = 3;
        Fit();
    }

    void Start() => Fit();

    void LateUpdate()
    {
        var rect = transform as RectTransform;
        if (rect == null)
            return;

        float width = ResolveWidth(rect);
        var size = new Vector2(width, rect.rect.height);
        if (_fitFrames > 0 || size != _lastSize)
        {
            _fitFrames--;
            Fit();
        }
    }

    public void Fit()
    {
        var grid = GetComponent<GridLayoutGroup>();
        var rect = transform as RectTransform;
        if (grid == null || rect == null || Columns < 1)
            return;

        float width = ResolveWidth(rect);
        if (width < 1f)
            return;

        _lastSize = new Vector2(width, rect.rect.height);
        float innerW = width - grid.padding.horizontal - grid.spacing.x * (Columns - 1);
        float cellW = Mathf.Max(1f, innerW / Columns);
        float cellH = Mathf.Max(1f, cellW * CellHeightRatio);
        grid.cellSize = new Vector2(cellW, cellH);
    }

    static float ResolveWidth(RectTransform rect)
    {
        float width = rect.rect.width;
        if (width >= 1f)
            return width;

        var parent = rect.parent as RectTransform;
        return parent != null ? parent.rect.width : 0f;
    }
}
