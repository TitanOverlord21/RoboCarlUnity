using UnityEngine;

/// <summary>
/// Swaps Carl's full-body sprites based on low energy (tired) and low oil (grubby).
/// </summary>
[RequireComponent(typeof(CarlResources))]
public class CarlVisual : MonoBehaviour
{
    const string VisualChildName = "Visual";
    const string DefaultAppearanceResource = "CarlAppearance";

    [SerializeField] CarlAppearance appearance;

    CarlResources _resources;
    SpriteRenderer _renderer;
    CarlSparkEffect _sparks;
    Transform _visualRoot;
    bool _isGrubby;

    void Awake()
    {
        _resources = GetComponent<CarlResources>();

        if (appearance == null)
            appearance = Resources.Load<CarlAppearance>(DefaultAppearanceResource);

        EnsureVisualRenderer();
        _sparks = gameObject.GetComponent<CarlSparkEffect>();
        if (_sparks == null)
            _sparks = gameObject.AddComponent<CarlSparkEffect>();
    }

    void OnEnable()
    {
        _resources.Changed += OnResourcesChanged;
        OnResourcesChanged();
    }

    void OnDisable()
    {
        _resources.Changed -= OnResourcesChanged;
    }

    void OnResourcesChanged()
    {
        bool energyLow = _resources.IsEnergyLow;
        bool oilLow = _resources.IsOilLow;

        if (appearance != null && _renderer != null)
        {
            _renderer.sprite = appearance.GetSprite(energyLow, oilLow);
            FitDisplayHeight();
        }

        if (oilLow != _isGrubby)
        {
            _isGrubby = oilLow;
            _sparks.SetActive(oilLow);
        }
    }

    void EnsureVisualRenderer()
    {
        _visualRoot = transform.Find(VisualChildName);
        if (_visualRoot == null)
        {
            var visualObject = new GameObject(VisualChildName);
            _visualRoot = visualObject.transform;
            _visualRoot.SetParent(transform, false);
            _visualRoot.localPosition = Vector3.zero;
            _visualRoot.localRotation = Quaternion.identity;
            _visualRoot.localScale = Vector3.one;
        }

        _renderer = _visualRoot.GetComponent<SpriteRenderer>();
        if (_renderer == null)
            _renderer = _visualRoot.gameObject.AddComponent<SpriteRenderer>();

        _renderer.sortingOrder = 1;
        _renderer.color = Color.white;
        GameSprites.ApplySpriteMaterial(_renderer);
    }

    void FitDisplayHeight()
    {
        if (_renderer.sprite == null || appearance == null)
            return;

        float spriteHeight = _renderer.sprite.bounds.size.y;
        if (spriteHeight < 0.001f)
            return;

        float scale = appearance.DisplayHeight / spriteHeight;
        _visualRoot.localScale = new Vector3(scale, scale, 1f);
    }
}
