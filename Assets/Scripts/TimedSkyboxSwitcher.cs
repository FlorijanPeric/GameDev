using UnityEngine;

public class TimedSkyboxSwitcher : MonoBehaviour
{
    [Header("Skyboxes (order matters)")]
    public Material[] skyboxes;

    [Header("Time Settings")]
    public float changeIntervalMinutes = 5f;

    [Header("Sun Lighting")]
    public Light sunLight;
    public float[] sunIntensitiesPerSkybox;

    private int currentIndex = 0;
    private float timer;

    void Start()
    {
        if (skyboxes.Length == 0) return;

        // Auto-find sun if not assigned
        if (sunLight == null)
        {
            sunLight = FindObjectOfType<Light>();
            if (sunLight != null && sunLight.type != LightType.Directional)
                sunLight = null;
        }

        // Initialize sun intensities array if empty
        if (sunIntensitiesPerSkybox == null || sunIntensitiesPerSkybox.Length == 0)
        {
            sunIntensitiesPerSkybox = new float[skyboxes.Length];
            for (int i = 0; i < skyboxes.Length; i++)
                sunIntensitiesPerSkybox[i] = 1.5f;
        }

        timer = changeIntervalMinutes * 60f;
        ApplySkybox();
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            NextSkybox();
            timer = changeIntervalMinutes * 60f;
        }
    }

    void NextSkybox()
    {
        currentIndex++;
        if (currentIndex >= skyboxes.Length)
            currentIndex = 0;

        ApplySkybox();
    }

    void ApplySkybox()
    {
        RenderSettings.skybox = skyboxes[currentIndex];
        DynamicGI.UpdateEnvironment();

        // Adjust sun intensity based on skybox
        if (sunLight != null && currentIndex < sunIntensitiesPerSkybox.Length)
        {
            sunLight.intensity = sunIntensitiesPerSkybox[currentIndex];
        }
    }
}
