using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

[System.Serializable]
public struct IntroSubtitle
{
    [TextArea(1,3)] public string text;
    public float startTime; // seconds
    public float duration; // seconds
}

public class IntroSequencePlayer : MonoBehaviour
{
    [Header("Visuals")]
    public Sprite introImage;
    public Image uiImage; // assign the UI Image component to show the sprite

    [Header("Audio")]
    public AudioClip introAudio;
    public AudioSource audioSource;
    [Tooltip("If true, the video audio will be routed to the audioSource. If false, background music can play behind the video.")]
    public bool useVideoAudio = false;

    [Header("Subtitles")]
    public IntroSubtitle[] subtitles;
    public Text subtitleText; // UI Text to show subtitles

    [Header("Playback")]
    public float totalDuration = 30f;
    public bool playOnStart = true;
    public bool showSkipButton = true;

    [Header("Skip")]
    public Button skipButton;

    [Header("Video")]
    public VideoClip introVideo;
    public RawImage videoRawImage; // UI RawImage to display video frames
    public VideoPlayer videoPlayer;
    [Header("Return To Start UI")]
    public GameObject startUI; // existing main menu root to enable after intro
    public Image startBackgroundImage; // full-screen image on the start UI
    public Sprite startBackgroundSprite; // user-provided background sprite
    public bool hideMenuDuringIntro = true;

    private Coroutine playRoutine;
    private bool introFinished;
    private RenderTexture videoRenderTexture;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        if (uiImage != null && introImage != null)
        {
            uiImage.sprite = introImage;
            uiImage.preserveAspect = true;
        }

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(showSkipButton);
            skipButton.onClick.AddListener(StopIntro);
        }

        if (subtitleText != null)
        {
            subtitleText.text = string.Empty;
        }

        if (hideMenuDuringIntro && startUI != null)
        {
            startUI.SetActive(false);
        }

        // Prepare video player reference and RenderTexture
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null && introVideo != null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
                videoPlayer.playOnAwake = false;
            }
        }

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            
            // Create a RenderTexture for the video to render to
            if (videoRenderTexture == null && introVideo != null)
            {
                videoRenderTexture = new RenderTexture(1920, 1080, 0);
                videoRenderTexture.name = "VideoPlaybackTexture";
            }
            
            // Configure video player to render to the texture
            if (videoRenderTexture != null)
            {
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.targetTexture = videoRenderTexture;
                Debug.Log("[IntroSequencePlayer] VideoPlayer configured to render to RenderTexture.");
            }
            
            if (useVideoAudio && audioSource != null)
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.SetTargetAudioSource(0, audioSource);
            }
            else
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            }
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayIntro();
        }
    }

    private void OnDestroy()
    {
        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            Destroy(videoRenderTexture);
            videoRenderTexture = null;
        }
    }

    public void PlayIntro()
    {
        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(IntroRoutine());
    }

    public void StopIntro()
    {
        if (playRoutine != null) StopCoroutine(playRoutine);
        if (audioSource != null) audioSource.Stop();
        if (subtitleText != null) subtitleText.text = string.Empty;
        if (videoPlayer != null) videoPlayer.Stop();
        EndIntroAndShowMenu();
    }

    private IEnumerator IntroRoutine()
    {
        float startTime = Time.unscaledTime;
        Debug.Log("[IntroSequencePlayer] Intro routine started.");
        Debug.Log($"[IntroSequencePlayer] Video assigned: {introVideo != null}, VideoPlayer assigned: {videoPlayer != null}, RawImage assigned: {videoRawImage != null}");
        Debug.Log($"[IntroSequencePlayer] startUI assigned: {startUI != null}");

        if (introImage != null && uiImage != null)
        {
            uiImage.sprite = introImage;
            uiImage.enabled = true;
        }

        // start audio if provided and video is not using audio
        if (introAudio != null && audioSource != null && !useVideoAudio)
        {
            audioSource.clip = introAudio;
            audioSource.Play();
        }

        // prepare and play video if present
        float videoDuration = 0f;
        if (introVideo != null && videoPlayer != null)
        {
            videoPlayer.clip = introVideo;
            Debug.Log($"[IntroSequencePlayer] Preparing video: {introVideo.name}");
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }
            Debug.Log($"[IntroSequencePlayer] Video prepared. Frame count: {videoPlayer.frameCount}");

            // Get the actual video duration
            videoDuration = (float)videoPlayer.length;
            Debug.Log($"[IntroSequencePlayer] Video duration: {videoDuration}s");

            // assign texture if we have a RawImage
            if (videoRawImage != null)
            {
                Debug.Log($"[IntroSequencePlayer] Assigning RenderTexture to RawImage. RenderTexture exists: {videoRenderTexture != null}");
                videoRawImage.texture = videoRenderTexture;
                videoRawImage.enabled = true;
                Debug.Log($"[IntroSequencePlayer] RawImage enabled. Texture assigned: {(videoRawImage.texture != null ? "yes" : "no")}");
            }
            else
            {
                Debug.LogWarning("[IntroSequencePlayer] RawImage is null! Video cannot display.");
            }

            videoPlayer.Play();
            Debug.Log($"[IntroSequencePlayer] Video started playing. isPlaying: {videoPlayer.isPlaying}");
        }
        else
        {
            if (introVideo == null) Debug.LogWarning("[IntroSequencePlayer] introVideo is null!");
            if (videoPlayer == null) Debug.LogWarning("[IntroSequencePlayer] videoPlayer is null!");
        }

        // Use video duration if available, otherwise use totalDuration, whichever is longer
        float effectiveDuration = Mathf.Max(videoDuration, totalDuration);
        Debug.Log($"[IntroSequencePlayer] Effective duration: {effectiveDuration}s (video: {videoDuration}s, total: {totalDuration}s)");

       float safetyTimer = 0f;

    while (Time.unscaledTime - startTime < effectiveDuration)
    {
        safetyTimer += Time.unscaledDeltaTime;

        if (safetyTimer > effectiveDuration + 2f)
        {
            Debug.LogWarning("Intro forced exit (safety timeout)");
            break;
        }
        float t = Time.unscaledTime - startTime;
        UpdateSubtitle(t);
        yield return null;
    }
        Debug.Log($"[IntroSequencePlayer] Intro duration ({effectiveDuration}s) complete. Ending intro.");
        EndIntroAndShowMenu();
        playRoutine = null;
    }

    private void EndIntroAndShowMenu()
    {
        if (introFinished) return;
        introFinished = true;

        Debug.Log("[IntroSequencePlayer] EndIntroAndShowMenu called.");

        if (subtitleText != null) subtitleText.text = string.Empty;
        if (audioSource != null) audioSource.Stop();
        if (videoPlayer != null) videoPlayer.Stop();

        // Clean up RenderTexture
        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            Destroy(videoRenderTexture);
            videoRenderTexture = null;
            Debug.Log("[IntroSequencePlayer] RenderTexture released.");
        }

        // Show existing main menu and apply background if provided.
        if (startUI != null)
        {
            Debug.Log($"[IntroSequencePlayer] startUI found: {startUI.name}. Activating it.");
            if (startBackgroundImage != null && startBackgroundSprite != null)
            {
                Debug.Log($"[IntroSequencePlayer] Assigning background sprite: {startBackgroundSprite.name}");
                startBackgroundImage.sprite = startBackgroundSprite;
                startBackgroundImage.preserveAspect = true;
            }
            startUI.SetActive(true);
            Debug.Log($"[IntroSequencePlayer] startUI is now active: {startUI.activeSelf}");
        }
        else
        {
            Debug.LogWarning("[IntroSequencePlayer] startUI is NULL! Main menu cannot be shown.");
        }

        gameObject.SetActive(false);
        if (transform.parent != null)
        {
            transform.parent.gameObject.SetActive(false);
        }
        Debug.Log("[IntroSequencePlayer] Intro Canvas deactivated.");
    }

    private void UpdateSubtitle(float time)
    {
        if (subtitles == null || subtitles.Length == 0 || subtitleText == null)
            return;

        for (int i = 0; i < subtitles.Length; i++)
        {
            var s = subtitles[i];
            if (time >= s.startTime && time < s.startTime + s.duration)
            {
                subtitleText.text = s.text;
                return;
            }
        }

        subtitleText.text = string.Empty;
    }
}