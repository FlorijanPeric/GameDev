using System.Collections;
using UnityEngine;

public class SpecialRoundDomeDirector : MonoBehaviour
{
    public static event System.Action<bool> SpecialRoundStateChanged;

    [Header("Dome Setup")]
    public GameObject domePrefab;
    public Transform player;
    public TeleportFlashOverlay teleportFlash;

    [Header("Random Placement")]
    public Vector3 areaCenter = Vector3.zero;
    public Vector2 areaSize = new Vector2(180f, 180f);
    public LayerMask groundMask = ~0;
    public float raycastStartHeight = 220f;
    public bool placeDomeOnGround = true;
    public float domeHeightAboveGround = 18f;
    public int maxPlacementAttempts = 20;

    [Header("Special Round Timing")]
    public bool autoStart = true;
    public float initialDelay = 20f;
    public float minTimeBetweenChecks = 35f;
    public float maxTimeBetweenChecks = 65f;
    [Range(0f, 1f)] public float triggerChancePerCheck = 0.35f;
    public float specialRoundDuration = 30f;

    private GameObject activeDome;
    private Coroutine loopRoutine;

    private void OnEnable()
    {
        if (autoStart)
        {
            StartDirector();
        }
    }

    private void OnDisable()
    {
        StopDirector();
    }

    public void StartDirector()
    {
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
        }

        loopRoutine = StartCoroutine(DirectorLoop());
    }

    public void StopDirector()
    {
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }

        if (activeDome != null)
        {
            Destroy(activeDome);
            activeDome = null;
        }
    }

    private IEnumerator DirectorLoop()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, initialDelay));

        while (true)
        {
            float waitTime = Random.Range(minTimeBetweenChecks, maxTimeBetweenChecks);
            yield return new WaitForSeconds(Mathf.Max(0.1f, waitTime));

            if (activeDome != null)
            {
                continue;
            }

            if (Random.value > triggerChancePerCheck)
            {
                continue;
            }

            yield return StartCoroutine(RunSpecialRound());
        }
    }

    private IEnumerator RunSpecialRound()
    {
        if (domePrefab == null)
        {
            yield break;
        }

        Vector3 groundPoint;
        if (!TryFindGroundPoint(out groundPoint))
        {
            yield break;
        }

        Vector3 domePosition = groundPoint;
        PlayTeleportFlash();
        activeDome = Instantiate(domePrefab, domePosition, Quaternion.identity);
        SnapDomeToGround(activeDome, groundPoint);
        SpecialRoundStateChanged?.Invoke(true);

        float spawnDelay = 0f;
        DomeEntryDoor door = activeDome.GetComponentInChildren<DomeEntryDoor>();
        if (door != null)
        {
            door.ResetClosed();
            spawnDelay = door.GetSequenceDuration();
            door.PlayOpenSequence();
        }

        DomeZombieSpawner domeSpawner = activeDome.GetComponentInChildren<DomeZombieSpawner>();
        if (domeSpawner != null)
        {
            if (domeSpawner.player == null)
            {
                domeSpawner.player = ResolvePlayer();
            }

            domeSpawner.BeginSpecialRound(specialRoundDuration, spawnDelay);
        }

        yield return new WaitForSeconds(Mathf.Max(0.1f, specialRoundDuration));

        if (activeDome != null)
        {
            Destroy(activeDome);
            activeDome = null;
        }

        SpecialRoundStateChanged?.Invoke(false);
    }

    private void PlayTeleportFlash()
    {
        if (teleportFlash == null)
        {
            teleportFlash = FindObjectOfType<TeleportFlashOverlay>();
        }

        if (teleportFlash == null)
        {
            GameObject flashObject = new GameObject("TeleportFlashOverlay");
            teleportFlash = flashObject.AddComponent<TeleportFlashOverlay>();
        }

        teleportFlash.PlayFlash();
    }

    private bool TryFindGroundPoint(out Vector3 groundPoint)
    {
        for (int i = 0; i < Mathf.Max(1, maxPlacementAttempts); i++)
        {
            float randomX = Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f);
            float randomZ = Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f);

            Vector3 sample = areaCenter + new Vector3(randomX, raycastStartHeight, randomZ);
            Ray ray = new Ray(sample, Vector3.down);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, raycastStartHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            {
                groundPoint = hit.point;
                return true;
            }
        }

        groundPoint = areaCenter;
        return false;
    }

    private void SnapDomeToGround(GameObject dome, Vector3 groundPoint)
    {
        if (dome == null)
        {
            return;
        }

        Transform floor = dome.transform.Find("DomeFloor");
        if (floor == null)
        {
            return;
        }

        float deltaY = groundPoint.y - floor.position.y;
        if (Mathf.Abs(deltaY) > 0.001f)
        {
            dome.transform.position += Vector3.up * deltaY;
        }
    }

    private Transform ResolvePlayer()
    {
        if (player != null)
        {
            return player;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            player = taggedPlayer.transform;
            return player;
        }

        if (Camera.main != null)
        {
            player = Camera.main.transform;
            return player;
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Vector3 size = new Vector3(areaSize.x, 1f, areaSize.y);
        Gizmos.DrawCube(areaCenter, size);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
        Gizmos.DrawWireCube(areaCenter, size);
    }
}
