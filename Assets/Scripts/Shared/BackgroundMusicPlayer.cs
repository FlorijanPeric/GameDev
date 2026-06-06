using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusicPlayer : MonoBehaviour
{
    [Header("Music")]
    public AudioClip musicClip;
    public AudioClip[] playlist;
    [Range(0f,1f)] public float volume = 0.5f;
    public bool loop = true; // loop current clip or entire playlist depending on usePlaylist
    public bool playOnStart = true;
    public float fadeInSeconds = 1.0f;
    public bool usePlaylist = true;
    public bool shufflePlaylist = false;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false; // we handle looping when needed
        audioSource.volume = 0f;
        audioSource.clip = musicClip;
    }

    private void Start()
    {
        if (!playOnStart) return;

        if (usePlaylist && playlist != null && playlist.Length > 0)
        {
            StartCoroutine(PlaylistRoutine());
        }
        else if (audioSource.clip != null)
        {
            audioSource.loop = loop;
            audioSource.Play();
            if (fadeInSeconds > 0.01f)
            {
                StartCoroutine(FadeInRoutine(fadeInSeconds));
            }
            else
            {
                audioSource.volume = volume;
            }
        }
    }

    private System.Collections.IEnumerator PlaylistRoutine()
    {
        int count = playlist.Length;
        int index = 0;
        System.Collections.Generic.List<int> order = new System.Collections.Generic.List<int>();
        for (int i = 0; i < count; i++) order.Add(i);
        if (shufflePlaylist)
        {
            for (int i = 0; i < order.Count; i++)
            {
                int j = Random.Range(i, order.Count);
                int tmp = order[i]; order[i] = order[j]; order[j] = tmp;
            }
        }

        while (true)
        {
            AudioClip clip = playlist[order[index]];
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.loop = false;
                audioSource.Play();
                // fade in
                if (fadeInSeconds > 0.01f)
                {
                    yield return StartCoroutine(FadeInRoutine(fadeInSeconds));
                }
                else
                {
                    audioSource.volume = volume;
                }

                // wait for clip to finish
                while (audioSource.isPlaying)
                {
                    yield return null;
                }
            }

            index = (index + 1) % count;
            if (index == 0 && !loop)
            {
                // reached end and not looping playlist
                break;
            }
            yield return null;
        }
    }

    public void Play()
    {
        if (audioSource.clip == null) return;
        audioSource.Play();
        StartCoroutine(FadeInRoutine(fadeInSeconds));
    }

    private IEnumerator FadeInRoutine(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, volume, Mathf.Clamp01(t / seconds));
            yield return null;
        }
        audioSource.volume = volume;
    }
}
