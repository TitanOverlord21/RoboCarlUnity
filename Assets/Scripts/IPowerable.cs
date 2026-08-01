using UnityEngine;

/// <summary>
/// Something a <see cref="PowerButton"/> can turn on/off (fans, future props).
/// </summary>
public interface IPowerable
{
    bool IsPowered { get; }
    Vector2 WireAttachPoint { get; }
    void SetPowered(bool powered);
}
