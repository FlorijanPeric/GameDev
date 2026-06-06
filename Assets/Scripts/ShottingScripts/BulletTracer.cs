using UnityEngine;

public class BulletTracer : MonoBehaviour
{
    public float speed = 300f;
    public float maxLifeTime = 1f;

    private Vector3 startPoint;
    private Vector3 targetPoint;
    private float travelTime;
    private float timer;

    public void Init(Vector3 hitPoint)
    {
        startPoint = transform.position;
        targetPoint = hitPoint;

        // Rotate tracer to face target
        transform.LookAt(targetPoint);

        // Calculate how long the tracer should exist
        float distance = Vector3.Distance(startPoint, targetPoint);
        travelTime = distance / speed;

        Destroy(gameObject, Mathf.Min(travelTime, maxLifeTime));
    }

    void Update()
    {
        // Move forward constantly
        transform.position += transform.forward * speed * Time.deltaTime;
    }
}
