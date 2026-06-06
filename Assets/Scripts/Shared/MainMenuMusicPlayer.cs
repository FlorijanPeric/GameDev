using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MainMenuMusicPlayer : MonoBehaviour
{
    [Header("Intro Clips (played sequentially)")]
    public AudioClip[] introClips;

    [Header("Loop Clip (played after intro)")]
    public AudioClip loopClip;

    [Range(0f,1f)] public float volume = 0.6f;
    public bool playOnStart = true;
    public float fadeInSeconds = 0.8f;
    public bool loopAfterIntro = true;

    private AudioSource src;

    private void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.volume = 0f;
    }

    private void Start()
    {
        if (!playOnStart) return;
        if (introClips != null && introClips.Length > 0)
        {
            StartCoroutine(PlayIntroThenLoop());
        }
        else if (loopClip != null)
        {
            StartCoroutine(PlayLoop());
        }
    }

    public void Play()
    {
        Start();
    }

    public void StopMusic()
    {
        StopAllCoroutines();
        if (src != null) src.Stop();
    }

    private IEnumerator PlayIntroThenLoop()
    {
        for (int i = 0; i < introClips.Length; i++)
        {
            AudioClip clip = introClips[i];
            if (clip == null) continue;
            src.clip = clip;
            src.loop = false;
            src.Play();
            float startTime = Time.time;
            // fade in at start of first clip
            if (i == 0 && fadeInSeconds > 0.01f)
            {
                float t = 0f;
                while (t < fadeInSeconds)
                {
                    t += Time.deltaTime;
                    src.volume = Mathf.Lerp(0f, volume, Mathf.Clamp01(t / fadeInSeconds));
                    yield return null;
                }
                src.volume = volume;
            }

            while (src.isPlaying)
            {
                yield return null;
            }
        }

        if (loopAfterIntro && loopClip != null)
        {
            src.clip = loopClip;
            src.loop = true;
            src.Play();
            // ensure volume
            if (src.volume < 0.01f) src.volume = volume;
        }
    }

    private IEnumerator PlayLoop()
    {
        src.clip = loopClip;
        src.loop = loopAfterIntro;
        src.Play();
        if (fadeInSeconds > 0.01f)
        {
            float t = 0f;
            while (t < fadeInSeconds)
            {
                t += Time.deltaTime;
                src.volume = Mathf.Lerp(0f, volume, Mathf.Clamp01(t / fadeInSeconds));
                yield return null;
            }
        }
        src.volume = volume;
    }
}
