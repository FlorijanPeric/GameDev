using System.Collections;
using UnityEngine;

public class TeleportFlashOverlay : MonoBehaviour
{
    public static TeleportFlashOverlay Instance { get; private set; }

    [Header("Flash Settings")]
    public Color flashColor = new Color(0.8f, 0.95f, 1f, 1f);
    public float fadeInTime = 0.05f;
    public float holdTime = 0.08f;
    public float fadeOutTime = 0.35f;

    private float alpha;
    private Texture2D whiteTexture;
    private Coroutine flashRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        whiteTexture = Texture2D.whiteTexture;
    }

    public void PlayFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        alpha = 0f;

        float inDuration = Mathf.Max(0.01f, fadeInTime);
        float t = 0f;
        while (t < inDuration)
        {
            t += Time.deltaTime;
            alpha = Mathf.Clamp01(t / inDuration);
            yield return null;
        }

        alpha = 1f;
        if (holdTime > 0f)
        {
            yield return new WaitForSeconds(holdTime);
        }

        float outDuration = Mathf.Max(0.01f, fadeOutTime);
        t = 0f;
        while (t < outDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / outDuration);
            alpha = 1f - normalized;
            yield return null;
        }

        alpha = 0f;
        flashRoutine = null;
    }

    private void OnGUI()
    {
        if (alpha <= 0f)
        {
            return;
        }

        Color old = GUI.color;
        Color drawColor = flashColor;
        drawColor.a = alpha;
        GUI.color = drawColor;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture);
        GUI.color = old;
    }
}
