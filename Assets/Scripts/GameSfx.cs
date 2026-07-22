using UnityEngine;

/// <summary>
/// One-shot SFX playback that respects <see cref="GameAudioSettings.SfxVolume"/>.
/// </summary>
public static class GameSfx
{
    const string OilClipPath = "Audio/Sfx_OilPickup";
    const string BatteryClipPath = "Audio/Sfx_BatteryPickup";
    const string SpringClipPath = "Audio/Sfx_SpringSproing";

    static AudioSource _source;
    static AudioClip _oilPickup;
    static AudioClip _batteryPickup;
    static AudioClip _springSproing;

    public static void PlayOilPickup() => PlayOneShot(GetOilClip());

    public static void PlayBatteryPickup() => PlayOneShot(GetBatteryClip());

    public static void PlaySpringSproing() => PlayOneShot(GetSpringClip());

    static AudioClip GetOilClip()
    {
        if (_oilPickup == null)
            _oilPickup = Resources.Load<AudioClip>(OilClipPath);
        return _oilPickup;
    }

    static AudioClip GetBatteryClip()
    {
        if (_batteryPickup == null)
            _batteryPickup = Resources.Load<AudioClip>(BatteryClipPath);
        return _batteryPickup;
    }

    static AudioClip GetSpringClip()
    {
        if (_springSproing == null)
            _springSproing = Resources.Load<AudioClip>(SpringClipPath);
        return _springSproing;
    }

    static void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
            return;

        float volume = GameAudioSettings.SfxVolume;
        if (volume <= 0.001f)
            return;

        EnsureSource();
        _source.PlayOneShot(clip, volume);
    }

    static void EnsureSource()
    {
        if (_source != null)
            return;

        var go = new GameObject("GameSfx");
        Object.DontDestroyOnLoad(go);
        _source = go.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
        _source.loop = false;
    }
}
