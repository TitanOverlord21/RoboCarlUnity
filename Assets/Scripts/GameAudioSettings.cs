using System;
using UnityEngine;

/// <summary>
/// Persistent music / SFX volume (0–1).
/// </summary>
public static class GameAudioSettings
{
    const string MusicKey = "RoboCarl.MusicVolume";
    const string SfxKey = "RoboCarl.SfxVolume";

    public static event Action MusicVolumeChanged;
    public static event Action SfxVolumeChanged;

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicKey, 0.8f);
        set
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, clamped);
            PlayerPrefs.Save();
            MusicVolumeChanged?.Invoke();
        }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxKey, 0.8f);
        set
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, clamped);
            PlayerPrefs.Save();
            SfxVolumeChanged?.Invoke();
        }
    }
}
