using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent looping BGM that swaps menu vs level tracks and follows
/// <see cref="GameAudioSettings.MusicVolume"/>.
/// </summary>
public static class GameMusic
{
    const string MenuClipPath = "Audio/Music_Menu";
    const string LevelClipPath = "Audio/Music_Level";
    const string MenuSceneName = "MainMenu";

    static AudioSource _source;
    static AudioClip _menuClip;
    static AudioClip _levelClip;
    static string _currentTrack;
    static bool _hooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        EnsureHooked();
        ApplyForActiveScene();
    }

    static void EnsureHooked()
    {
        if (_hooked)
            return;

        _hooked = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameAudioSettings.MusicVolumeChanged += ApplyVolume;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyForScene(scene.name);
    }

    public static void ApplyForActiveScene()
    {
        ApplyForScene(SceneManager.GetActiveScene().name);
    }

    public static void ApplyForScene(string sceneName)
    {
        EnsureHooked();
        if (sceneName == MenuSceneName)
            PlayMenu();
        else
            PlayLevel();
    }

    public static void PlayMenu() => PlayTrack("menu", GetMenuClip());

    public static void PlayLevel() => PlayTrack("level", GetLevelClip());

    static void PlayTrack(string trackId, AudioClip clip)
    {
        if (clip == null)
            return;

        EnsureSource();
        ApplyVolume();

        if (_currentTrack == trackId && _source.isPlaying && _source.clip == clip)
            return;

        _currentTrack = trackId;
        _source.clip = clip;
        _source.loop = true;
        _source.Play();
    }

    static void ApplyVolume()
    {
        if (_source == null)
            return;

        _source.volume = GameAudioSettings.MusicVolume;
        if (_source.volume <= 0.001f)
        {
            if (_source.isPlaying)
                _source.Pause();
        }
        else if (_source.clip != null && !_source.isPlaying)
        {
            _source.Play();
        }
    }

    static AudioClip GetMenuClip()
    {
        if (_menuClip == null)
            _menuClip = Resources.Load<AudioClip>(MenuClipPath);
        return _menuClip;
    }

    static AudioClip GetLevelClip()
    {
        if (_levelClip == null)
            _levelClip = Resources.Load<AudioClip>(LevelClipPath);
        return _levelClip;
    }

    static void EnsureSource()
    {
        if (_source != null)
            return;

        var go = new GameObject("GameMusic");
        Object.DontDestroyOnLoad(go);
        _source = go.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
        _source.loop = true;
        _source.priority = 0;
    }
}
