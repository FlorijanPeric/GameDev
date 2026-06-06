using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates and assigns a sun (directional light) to the scene
/// </summary>
public static class CreateSunLight
{
    private const string SunName = "Sun_Auto";

    [MenuItem("Tools/Map/Survival/Add Sun")]
    public static void AddSun()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("No active scene found.");
            return;
        }

        // Check if sun already exists
        Light existingSun = FindSunInScene();
        if (existingSun != null)
        {
            Debug.LogWarning($"Sun already exists: {existingSun.gameObject.name}. Removing old sun first...");
            Object.DestroyImmediate(existingSun.gameObject);
        }

        // Create sun GameObject
        GameObject sunObj = new GameObject(SunName);
        Light sunLight = sunObj.AddComponent<Light>();

        // Configure as directional light (sun)
        sunLight.type = LightType.Directional;
        sunLight.intensity = 1.5f;
        sunLight.color = new Color(1f, 0.95f, 0.8f); // Warm sun color
        sunLight.shadows = LightShadows.Soft;
        sunLight.shadowStrength = 0.8f;
        sunLight.renderingLayerMask = -1;

        // Position and rotate sun for dramatic lighting
        sunObj.transform.position = new Vector3(100f, 150f, 100f);
        sunObj.transform.rotation = Quaternion.Euler(45f, -45f, 0f);

        // Mark scene as dirty
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"Sun created and assigned! Position: {sunObj.transform.position}, Rotation: {sunObj.transform.eulerAngles}");
    }

    [MenuItem("Tools/Map/Survival/Remove Sun")]
    public static void RemoveSun()
    {
        Light sunLight = FindSunInScene();
        if (sunLight != null)
        {
            Object.DestroyImmediate(sunLight.gameObject);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Sun removed from scene.");
        }
        else
        {
            Debug.LogWarning("No sun found in scene.");
        }
    }

    private static Light FindSunInScene()
    {
        Light[] allLights = Object.FindObjectsOfType<Light>();
        foreach (Light light in allLights)
        {
            if (light.type == LightType.Directional && 
                (light.gameObject.name.Contains("Sun") || light.gameObject.name == SunName))
            {
                return light;
            }
        }
        return null;
    }
}
