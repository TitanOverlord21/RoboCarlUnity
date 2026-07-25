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
    static AudioClip _powerClick;
    static AudioClip _fanHum;

    public static void PlayOilPickup() => PlayOneShot(GetOilClip());

    public static void PlayBatteryPickup() => PlayOneShot(GetBatteryClip());

    public static void PlaySpringSproing() => PlayOneShot(GetSpringClip());

    public static void PlayPowerClick() => PlayOneShot(GetPowerClickClip(), 0.7f);

    public static AudioClip GetFanHumClip()
    {
        if (_fanHum == null)
            _fanHum = BuildFanHumClip();
        return _fanHum;
    }

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

    static AudioClip GetPowerClickClip()
    {
        if (_powerClick == null)
            _powerClick = BuildPowerClickClip();
        return _powerClick;
    }

    static AudioClip BuildPowerClickClip()
    {
        const int sampleRate = 22050;
        const float duration = 0.06f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        var data = new float[samples];
        for (var i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float env = 1f - (t / duration);
            env *= env;
            float click = Mathf.Sin(t * 1800f * Mathf.PI * 2f) * 0.55f
                          + Mathf.Sin(t * 4200f * Mathf.PI * 2f) * 0.25f;
            // Tiny noise tick for a mechanical feel.
            click += (Random.value * 2f - 1f) * 0.15f * env;
            data[i] = click * env;
        }

        var clip = AudioClip.Create("Sfx_PowerClick", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip BuildFanHumClip()
    {
        const int sampleRate = 22050;
        const float duration = 1f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        var data = new float[samples];
        for (var i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float hum = Mathf.Sin(t * 95f * Mathf.PI * 2f) * 0.22f
                        + Mathf.Sin(t * 190f * Mathf.PI * 2f) * 0.12f
                        + Mathf.Sin(t * 280f * Mathf.PI * 2f) * 0.06f;
            // Soft broadband whoosh.
            float whoosh = (Random.value * 2f - 1f) * 0.08f;
            data[i] = hum + whoosh;
        }

        var clip = AudioClip.Create("Sfx_FanHum", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static void PlayOneShot(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        float volume = GameAudioSettings.SfxVolume * volumeScale;
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
