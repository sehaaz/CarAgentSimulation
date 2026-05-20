using UnityEngine;

public class WaypointFollower : MonoBehaviour
{
    [Header("Waypoint'ler")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float reachRadius = 0.5f;
    [SerializeField] private bool loop = false;

    [Header("Hareket")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxRotationSpeed = 250f;
    [Range(0.05f, 1f)]
    [SerializeField] private float turnSpeedFactor = 0.25f;

    [Header("AI Gorunumu (her baslangicta rastgele secilir)")]
    [SerializeField] private float steeringSmoothMin = 0.15f;
    [SerializeField] private float steeringSmoothMax = 0.35f;
    [SerializeField] private float steeringNoiseMin = 1.5f;
    [SerializeField] private float steeringNoiseMax = 4.5f;
    [SerializeField] private float noiseSpeedMin = 1f;
    [SerializeField] private float noiseSpeedMax = 2.5f;
    [SerializeField] private float speedVariationMin = 0.06f;
    [SerializeField] private float speedVariationMax = 0.2f;
    [SerializeField] private float lookAheadMin = 2f;
    [SerializeField] private float lookAheadMax = 4.5f;

    [Header("Kargo")]
    [SerializeField] private int pickupIndex = -1;
    [SerializeField] private int deliveryIndex = -1;
    [SerializeField] private float waitTime = 2f;
    [SerializeField] private GameObject cargoVisual;

    [Header("Nokta Objeleri (Sahnedeki GameObject'ler)")]
    [SerializeField] private GameObject pickupObject;
    [SerializeField] private GameObject deliveryObject;

    private int current;
    private bool hasCargo;
    private bool waiting;
    private float waitTimer;
    private bool done;
    private bool started;
    private float rotationVelocity;
    private float noiseSeed;

    private float steeringSmoothTime;
    private float steeringNoise;
    private float noiseSpeed;
    private float speedVariation;
    private float lookAheadDistance;

    private void Start()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector2.zero;
        }

        var controller = GetComponent<CarController>();
        if (controller != null) controller.enabled = false;

        if (cargoVisual != null) cargoVisual.SetActive(false);

        RandomizeAIParameters();
    }

    private void RandomizeAIParameters()
    {
        noiseSeed = Random.Range(0f, 1000f);
        steeringSmoothTime = Random.Range(steeringSmoothMin, steeringSmoothMax);
        steeringNoise = Random.Range(steeringNoiseMin, steeringNoiseMax);
        noiseSpeed = Random.Range(noiseSpeedMin, noiseSpeedMax);
        speedVariation = Random.Range(speedVariationMin, speedVariationMax);
        lookAheadDistance = Random.Range(lookAheadMin, lookAheadMax);
    }

    private void Update()
    {
        if (!started)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                started = true;
            return;
        }

        if (done || waypoints == null || waypoints.Length == 0) return;

        if (current >= waypoints.Length)
        {
            if (loop) { current = 0; hasCargo = false; }
            else { done = true; return; }
        }

        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0) waiting = false;
            return;
        }

        Transform target = waypoints[current];
        if (target == null) { current++; return; }

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        float dist = toTarget.magnitude;

        if (dist <= reachRadius)
        {
            OnReached();
            return;
        }

        // --- ROTATION ---

        float desiredAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;

        if (current + 1 < waypoints.Length && waypoints[current + 1] != null && dist < lookAheadDistance)
        {
            Vector2 nextDir = (Vector2)waypoints[current + 1].position - (Vector2)waypoints[current].position;
            float nextAngle = Mathf.Atan2(nextDir.y, nextDir.x) * Mathf.Rad2Deg - 90f;
            float blend = 1f - (dist / lookAheadDistance);
            desiredAngle = Mathf.LerpAngle(desiredAngle, nextAngle, blend * 0.45f);
        }

        float noise = (Mathf.PerlinNoise(Time.time * noiseSpeed, noiseSeed) - 0.5f) * 2f * steeringNoise;
        desiredAngle += noise;

        float currentAngle = transform.eulerAngles.z;
        float newAngle = Mathf.SmoothDampAngle(
            currentAngle, desiredAngle, ref rotationVelocity,
            steeringSmoothTime, maxRotationSpeed, Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);

        // --- SPEED ---

        float remaining = Mathf.Abs(Mathf.DeltaAngle(newAngle, desiredAngle));
        float speedMul = 1f;
        if (remaining > 10f)
            speedMul = Mathf.Lerp(1f, turnSpeedFactor, Mathf.Clamp01(remaining / 80f));

        float speedNoise = 1f + (Mathf.PerlinNoise(Time.time * 0.7f, noiseSeed + 50f) - 0.5f) * 2f * speedVariation;

        float speed = moveSpeed * speedMul * speedNoise;
        transform.position += transform.up * (speed * Time.deltaTime);
    }

    private void OnReached()
    {
        if (current == pickupIndex)
        {
            hasCargo = true;
            if (cargoVisual != null) cargoVisual.SetActive(true);
            if (pickupObject != null) pickupObject.SetActive(false);
            waiting = true;
            waitTimer = waitTime;
        }
        else if (current == deliveryIndex && hasCargo)
        {
            hasCargo = false;
            if (cargoVisual != null) cargoVisual.SetActive(false);
            if (deliveryObject != null) deliveryObject.SetActive(false);
            waiting = true;
            waitTimer = waitTime;
        }

        current++;
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null) return;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            if (i == pickupIndex) Gizmos.color = Color.green;
            else if (i == deliveryIndex) Gizmos.color = Color.red;
            else Gizmos.color = Color.cyan;

            Gizmos.DrawWireSphere(waypoints[i].position, reachRadius);

            if (i > 0 && waypoints[i - 1] != null)
            {
                Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.5f);
                Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
            }
        }

        if (pickupIndex >= 0 && pickupIndex < waypoints.Length && waypoints[pickupIndex] != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(waypoints[pickupIndex].position, reachRadius * 1.8f);
        }
        if (deliveryIndex >= 0 && deliveryIndex < waypoints.Length && waypoints[deliveryIndex] != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(waypoints[deliveryIndex].position, reachRadius * 1.8f);
        }

        if (Application.isPlaying && !done && current < waypoints.Length && waypoints[current] != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, waypoints[current].position);
        }
    }
}
