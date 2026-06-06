using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HitmarkerUI : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup canvasGroup;
    public Graphic markerGraphic;
    public Graphic crosshairGraphic;

    [Header("Colors")]
    public Color hitColor = Color.white;
    public Color killColor = Color.red;

    [Header("Timing")]
    public float visibleTime = 0.06f;
    public float fadeOutTime = 0.1f;

    [Header("Crosshair Flash")]
    public bool flashCrosshairOnShot = true;
    public Color crosshairFlashColor = new Color(1f, 0.92f, 0.45f, 1f);
    public float crosshairFlashHoldTime = 0.03f;
    public float crosshairFadeBackTime = 0.12f;

    private Coroutine routine;
    private Coroutine crosshairRoutine;
    private Color crosshairBaseColor = Color.white;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (markerGraphic == null)
        {
            markerGraphic = GetComponent<Graphic>();
        }

        if (crosshairGraphic == null)
        {
            crosshairGraphic = markerGraphic;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (crosshairGraphic != null)
        {
            crosshairBaseColor = crosshairGraphic.color;
        }
    }

    public void ShowHitmarker(bool isKill)
    {
        if (canvasGroup == null)
        {
            return;
        }

        if (markerGraphic != null)
        {
            markerGraphic.color = isKill ? killColor : hitColor;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(ShowRoutine());
    }

    public void ShowCrosshairFlash()
    {
        if (!flashCrosshairOnShot || crosshairGraphic == null)
        {
            return;
        }

        if (crosshairRoutine != null)
        {
            StopCoroutine(crosshairRoutine);
        }

        crosshairRoutine = StartCoroutine(CrosshairFlashRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        canvasGroup.alpha = 1f;
        yield return new WaitForSeconds(visibleTime);

        float t = 0f;
        float duration = Mathf.Max(0.01f, fadeOutTime);
        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            canvasGroup.alpha = 1f - normalized;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        routine = null;
    }

    private IEnumerator CrosshairFlashRoutine()
    {
        crosshairGraphic.color = crosshairFlashColor;
        yield return new WaitForSeconds(crosshairFlashHoldTime);

        float t = 0f;
        float duration = Mathf.Max(0.01f, crosshairFadeBackTime);
        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            crosshairGraphic.color = Color.Lerp(crosshairFlashColor, crosshairBaseColor, normalized);
            yield return null;
        }

        crosshairGraphic.color = crosshairBaseColor;
        crosshairRoutine = null;
    }
}
