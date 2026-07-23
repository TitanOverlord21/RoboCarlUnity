using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-left battery meter: fill amount = energy, fill color = oil (green→brown).
/// </summary>
public class ResourceHud : MonoBehaviour
{
    static readonly Color ShellColor = new(0.12f, 0.14f, 0.16f, 0.92f);
    static readonly Color WellColor = new(0.08f, 0.1f, 0.1f, 0.85f);
    static readonly Color TipColor = new(0.18f, 0.2f, 0.22f, 0.95f);
    static readonly Color OilFullColor = new(0.25f, 0.9f, 0.4f, 1f);
    static readonly Color OilEmptyColor = new(0.55f, 0.3f, 0.1f, 1f);

    static ResourceHud _instance;

    CarlResources _resources;
    Image _fill;

    public static void EnsureFor(CarlResources resources)
    {
        if (resources == null)
            return;

        if (_instance == null)
        {
            var hudObject = new GameObject(nameof(ResourceHud));
            _instance = hudObject.AddComponent<ResourceHud>();
            _instance.BuildUi();
        }

        _instance.Bind(resources);
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;

        Unbind();
    }

    void Bind(CarlResources resources)
    {
        if (_resources == resources)
        {
            Refresh();
            return;
        }

        Unbind();
        _resources = resources;
        _resources.Changed += Refresh;
        Refresh();
    }

    void Unbind()
    {
        if (_resources == null)
            return;

        _resources.Changed -= Refresh;
        _resources = null;
    }

    void Refresh()
    {
        if (_fill == null || _resources == null)
            return;

        _fill.fillAmount = Mathf.Clamp01(_resources.EnergyFraction);
        _fill.color = Color.Lerp(OilEmptyColor, OilFullColor, Mathf.Clamp01(_resources.OilFraction));
    }

    void BuildUi()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(900f, 1950f);
        scaler.matchWidthOrHeight = 1f;

        // Portrait playframe only — do NOT use CreateMobilePlayArea (its Letterbox is opaque black).
        var playArea = MenuUi.Create("MobilePlayArea", transform);
        var playRect = playArea.GetComponent<RectTransform>();
        playRect.anchorMin = new Vector2(0.5f, 0.5f);
        playRect.anchorMax = new Vector2(0.5f, 0.5f);
        playRect.pivot = new Vector2(0.5f, 0.5f);
        playRect.sizeDelta = new Vector2(900f, 1950f);

        var fitter = playArea.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = AspectRatioCamera.TargetAspect;

        var meter = MenuUi.Create("BatteryMeter", playArea.transform);
        var meterRect = meter.GetComponent<RectTransform>();
        MenuUi.SetAnchors(meterRect, new Vector2(0.04f, 0.88f), new Vector2(0.18f, 0.97f));

        var shell = MenuUi.Create("Shell", meter.transform);
        MenuUi.StretchFull(shell.GetComponent<RectTransform>());
        var shellImage = MenuUi.AddImage(shell, ShellColor);
        shellImage.raycastTarget = false;

        var tip = MenuUi.Create("Tip", meter.transform);
        MenuUi.SetAnchors(tip.GetComponent<RectTransform>(), new Vector2(0.28f, 0.92f), new Vector2(0.72f, 1.08f));
        var tipImage = MenuUi.AddImage(tip, TipColor);
        tipImage.raycastTarget = false;

        var well = MenuUi.Create("Well", meter.transform);
        MenuUi.SetAnchors(well.GetComponent<RectTransform>(), new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.88f));
        var wellImage = MenuUi.AddImage(well, WellColor);
        wellImage.raycastTarget = false;

        var fillObject = MenuUi.Create("Fill", well.transform);
        MenuUi.StretchFull(fillObject.GetComponent<RectTransform>());
        _fill = MenuUi.AddImage(fillObject, OilFullColor);
        _fill.raycastTarget = false;
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Vertical;
        _fill.fillOrigin = (int)Image.OriginVertical.Bottom;
        _fill.fillAmount = 1f;
    }
}
