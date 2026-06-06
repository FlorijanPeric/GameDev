using System.Collections;
using UnityEngine;

public class DomeEntryDoor : MonoBehaviour
{
    [Header("Door Motion")]
    public Vector3 openOffset = new Vector3(0f, 5.5f, 0f);
    public float doorOpenTime = 1f;
    public float holdClosedTime = 0.5f;

    private Vector3 closedLocalPosition;
    private Coroutine routine;

    private void Awake()
    {
        closedLocalPosition = transform.localPosition;
    }

    public void ResetClosed()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        transform.localPosition = closedLocalPosition;
    }

    public float GetSequenceDuration()
    {
        return Mathf.Max(0f, holdClosedTime) + Mathf.Max(0.01f, doorOpenTime);
    }

    public void PlayOpenSequence()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        transform.localPosition = closedLocalPosition;

        if (holdClosedTime > 0f)
        {
            yield return new WaitForSeconds(holdClosedTime);
        }

        Vector3 target = closedLocalPosition + openOffset;
        float duration = Mathf.Max(0.01f, doorOpenTime);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            transform.localPosition = Vector3.Lerp(closedLocalPosition, target, normalized);
            yield return null;
        }

        transform.localPosition = target;
        routine = null;
    }
}
