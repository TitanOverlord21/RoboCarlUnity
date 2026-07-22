using UnityEngine;

/// <summary>
/// Sprite set for Carl's resource-driven looks (energy = tired, oil = grubby).
/// </summary>
[CreateAssetMenu(fileName = "CarlAppearance", menuName = "RoboCarl/Carl Appearance")]
public class CarlAppearance : ScriptableObject
{
    [SerializeField] Sprite normal;
    [SerializeField] Sprite tired;
    [SerializeField] Sprite grubby;
    [SerializeField] Sprite tiredGrubby;
    [SerializeField] float displayHeight = 1.15f;

    public float DisplayHeight => displayHeight;
    public Sprite Normal => normal;
    public Sprite Tired => tired;
    public Sprite Grubby => grubby;
    public Sprite TiredGrubby => tiredGrubby;

    public Sprite GetSprite(bool energyLow, bool oilLow)
    {
        if (energyLow && oilLow)
            return tiredGrubby != null ? tiredGrubby : (tired != null ? tired : normal);
        if (energyLow)
            return tired != null ? tired : normal;
        if (oilLow)
            return grubby != null ? grubby : normal;
        return normal;
    }
}
