using UnityEngine;

/// <summary>
/// Controls background music mute/unmute and exposes simple API for UI.
/// </summary>
[DefaultExecutionOrder(-100)]
public class BackgroundMusicController : MonoBehaviour
{
    public static BackgroundMusicController Instance { get; private set; }

    public BackgroundMusicPlayer musicPlayer;

    private AudioSource source;
    private float savedVolume = 1f;
    private bool isMuted = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (musicPlayer == null)
        {
            musicPlayer = FindObjectOfType<BackgroundMusicPlayer>();
        }

        if (musicPlayer != null)
        {
            source = musicPlayer.GetComponent<AudioSource>();
            if (source != null) savedVolume = source.volume;
        }
    }

    public void SetMusicMuted(bool mute)
    {
        if (source == null)
        {
            if (musicPlayer != null) source = musicPlayer.GetComponent<AudioSource>();
            if (source == null) return;
        }

        if (mute)
        {
            if (!isMuted) savedVolume = source.volume;
            source.volume = 0f;
            isMuted = true;
        }
        else
        {
            source.volume = savedVolume;
            isMuted = false;
        }
    }

    public void ToggleMusic()
    {
        SetMusicMuted(!isMuted);
    }

    public bool IsMuted => isMuted;
}
