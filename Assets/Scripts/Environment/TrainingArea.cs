using UnityEngine;

/// <summary>
/// Master controller for one training area. Manages the agent, waypoints, and obstacles
/// for a single episode. All reward values live here and are read by CarAgent.
/// </summary>
public class TrainingArea : MonoBehaviour
{
    [Header("Area")]

    [Tooltip("Width and height of the training area in world units. Increasing creates a larger playground giving the agent more room but requiring longer episodes. Decreasing makes a tighter space that's faster to learn but harder to navigate. Recommended: 30–60.")]
    [SerializeField] private float areaSize = 40f;

    [Header("Agent Spawn")]

    [Tooltip("The CarAgent prefab to instantiate in this training area. Must have CarAgent, CarController, and Rigidbody2D components.")]
    [SerializeField] private GameObject carAgentPrefab;

    [Tooltip("Minimum distance from the border where the agent can spawn. 0 means right at the wall, higher values push the agent further inward. Must be less than agentBorderMaxDistance. Recommended: 1–3.")]
    [SerializeField] private float agentBorderMinDistance = 1f;

    [Tooltip("Maximum distance from the border where the agent can spawn. Increasing allows the agent to spawn further from the wall. Must be greater than agentBorderMinDistance and less than areaSize/2. Recommended: 3–8.")]
    [SerializeField] private float agentBorderMaxDistance = 5f;

    [Tooltip("Minimum angular difference in degrees between the agent's initial facing direction and the direction toward the first waypoint. Increasing forces the agent to always start somewhat misaligned. Recommended: 0.")]
    [Range(0f, 180f)]
    [SerializeField] private float agentSpawnMinAngleOffset = 0f;

    [Tooltip("Maximum angular difference in degrees between the agent's initial facing direction and the direction toward the first waypoint. 0 means the agent always faces the waypoint directly. 180 means the agent may face completely away. Increasing this makes initial orientation harder. Recommended: 30 for stage 1, up to 180 for stage 4.")]
    [Range(0f, 180f)]
    [SerializeField] private float agentSpawnMaxAngleOffset = 30f;

    [Header("Waypoints")]

    [Tooltip("Number of waypoints placed per episode. Increasing extends episode complexity and length. Decreasing makes episodes simpler and faster. Overridden per curriculum stage. Recommended: 1 for stage 1, 3–8 for later stages.")]
    [SerializeField] private int waypointCount = 3;

    [Tooltip("Maximum radius from the area center where the first waypoint can spawn. 0 means exactly at center, higher values allow it to be further from center. Recommended: 0–5.")]
    [SerializeField] private float firstWaypointCenterRadius = 3f;

    [Tooltip("Minimum distance from the border where the second waypoint spawns (on the opposite side of the agent). 0 means right at the wall. Higher values push it further inward from the opposite border. Recommended: 2–6.")]
    [SerializeField] private float secondWaypointBorderMinDistance = 2f;

    [Tooltip("Maximum distance from the border where the second waypoint spawns (on the opposite side of the agent). Higher values allow more variation in how far from the opposite wall it appears. Recommended: 4–10.")]
    [SerializeField] private float secondWaypointBorderMaxDistance = 6f;

    [Tooltip("How much the second waypoint can deviate sideways from the exact opposite point of the agent. 0 means directly opposite, higher values add lateral randomness. Recommended: 0–10.")]
    [SerializeField] private float secondWaypointLateralSpread = 5f;

    [Tooltip("Minimum distance in world units between consecutive waypoints (3rd onward). Increasing spreads waypoints further apart requiring longer drives between them. Decreasing clusters waypoints closer together. Recommended: 5–10.")]
    [SerializeField] private float waypointMinDistance = 5f;

    [Tooltip("Maximum distance in world units between consecutive waypoints (3rd onward). Increasing allows much wider spacing, making navigation harder. Decreasing keeps waypoints close. Recommended: 10–20.")]
    [SerializeField] private float waypointMaxDistance = 15f;

    [Tooltip("Cosmetic scale of the waypoint marker prefab. No effect on training or collision — purely visual. Increasing makes waypoints easier to see. Decreasing makes them more subtle. Recommended: 0.5–2.0.")]
    [SerializeField] private float waypointVisualSize = 1f;

    [Tooltip("Distance in world units at which the agent is considered to have reached a waypoint. Too small makes it very hard for the agent to hit waypoints, slowing learning. Too large makes the task trivially easy. Recommended: 1.0–3.0.")]
    [SerializeField] private float waypointReachedRadius = 1.5f;

    [Header("Episode")]

    [Tooltip("Maximum episode duration in seconds. Increasing gives the agent more time to complete all waypoints but slows training if episodes run to timeout. Decreasing pressures the agent to be efficient but may be too short for complex layouts. Recommended: 30–120.")]
    [SerializeField] private float maxEpisodeTime = 60f;

    [Header("Rewards")]

    [Tooltip("Reward given each time the agent reaches the next waypoint in sequence. If too high, the agent may rush recklessly toward waypoints ignoring obstacles. If too low, the agent lacks motivation to seek waypoints. Good starting value: +1.0.")]
    [SerializeField] private float rewardWaypointReached = 1.0f;

    [Tooltip("Small reward added every FixedUpdate step when the agent is closer to the current waypoint than the previous step. If too high, the agent learns to inch forward without actually reaching waypoints. If too low, provides insufficient guidance toward waypoints. Good starting value: +0.001 to +0.01.")]
    [SerializeField] private float rewardApproachWaypoint = 0.005f;

    [Tooltip("Terminal reward given when the agent reaches ALL waypoints. This is the primary objective signal. If too high relative to per-waypoint reward, the agent may not value intermediate waypoints. If too low, completing all waypoints isn't sufficiently incentivized. Good starting value: +2.0 to +5.0.")]
    [SerializeField] private float rewardAllWaypointsReached = 3.0f;

    [Header("Penalties")]

    [Tooltip("Penalty applied each time the agent collides with an obstacle. If too harsh, the agent becomes overly cautious and stops moving. If too mild, the agent ignores obstacles entirely. Good starting value: -0.5 to -1.0.")]
    [SerializeField] private float penaltyCollision = -0.5f;

    [Tooltip("Penalty applied each time the agent collides with a boundary wall. Separate from obstacle collision so you can punish wall hits harder to keep the agent away from edges. If too harsh, the agent avoids edges but may get stuck. If too mild, the agent slides along walls. Good starting value: -0.5 to -2.0.")]
    [SerializeField] private float penaltyBoundaryHit = -1.0f;

    [Tooltip("Tiny penalty applied every FixedUpdate step regardless of agent behavior. Encourages the agent to complete objectives quickly rather than idling. If too high, the agent panics and makes poor decisions. If too low, provides no time pressure. Good starting value: -0.0005 to -0.002.")]
    [SerializeField] private float penaltyTimeStep = -0.001f;

    [Tooltip("Penalty applied when the episode ends due to timeout. Discourages the agent from wasting time. If too harsh, the agent may take dangerous shortcuts. If too mild, the agent doesn't care about timeouts. Good starting value: -1.0 to -3.0.")]
    [SerializeField] private float penaltyTimeout = -2.0f;

    [Header("Visuals — Boundary")]

    [Tooltip("Color of the boundary walls. Recommended: a clearly visible color like red or white.")]
    [SerializeField] private Color boundaryColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Tooltip("Thickness of the visible boundary walls in world units. Increasing makes walls more visible. Recommended: 0.3–1.0.")]
    [SerializeField] private float boundaryThickness = 0.5f;

    [Header("Gizmos — Area")]

    [Tooltip("Show the area boundary rectangle gizmo in Scene view.")]
    [SerializeField] private bool showAreaBoundaryGizmo = true;

    [Tooltip("Color of the area boundary rectangle gizmo.")]
    [SerializeField] private Color areaBoundaryGizmoColor = Color.green;

    [Tooltip("Show the agent border spawn zone as inner rectangles in Scene view.")]
    [SerializeField] private bool showSpawnZoneGizmo = true;

    [Tooltip("Color of the agent border spawn zone gizmo.")]
    [SerializeField] private Color spawnZoneGizmoColor = new Color(1f, 0.5f, 0f, 0.5f);

    [Tooltip("Show the first waypoint center radius circle in Scene view.")]
    [SerializeField] private bool showCenterRadiusGizmo = true;

    [Tooltip("Color of the first waypoint center radius gizmo.")]
    [SerializeField] private Color centerRadiusGizmoColor = new Color(0.2f, 0.8f, 1f, 0.5f);

    [Header("Gizmos — Waypoints")]

    [Tooltip("Show waypoint circles, path lines, and agent-to-waypoint line in Scene view.")]
    [SerializeField] private bool showWaypointGizmos = true;

    [Tooltip("Color of the current active waypoint circle.")]
    [SerializeField] private Color waypointCurrentColor = Color.yellow;

    [Tooltip("Color of upcoming waypoint circles.")]
    [SerializeField] private Color waypointUpcomingColor = Color.cyan;

    [Tooltip("Color of already reached waypoint circles.")]
    [SerializeField] private Color waypointReachedColor = Color.gray;

    [Tooltip("Color of the path lines connecting waypoints.")]
    [SerializeField] private Color waypointPathColor = Color.white;

    [Tooltip("Color of the line drawn from agent to current waypoint.")]
    [SerializeField] private Color agentToWaypointLineColor = Color.green;

    [Header("Gizmos — Raycasts")]

    [Tooltip("Show raycast lines from the agent in Scene view during play mode.")]
    [SerializeField] private bool showRaycastGizmos = true;

    [Tooltip("Color of raycast lines that did not hit anything.")]
    [SerializeField] private Color raycastMissColor = Color.green;

    [Tooltip("Color of raycast lines that hit an obstacle or boundary.")]
    [SerializeField] private Color raycastHitColor = Color.red;

    [Header("Obstacles")]

    [Tooltip("Parent GameObject containing all obstacle children. Toggle the GameObject active/inactive yourself to control whether obstacles are present during training.")]
    [SerializeField] private GameObject obstaclesRoot;

    // References set at runtime
    private CarAgent carAgentInstance;
    private WaypointManager waypointManager;
    private float episodeTimer;
    private bool episodeActive;

    /// <summary>
    /// Width and height of this training area in world units.
    /// </summary>
    public float AreaSize => areaSize;

    // Reward/penalty accessors for CarAgent
    public float RewardWaypointReached => rewardWaypointReached;
    public float RewardApproachWaypoint => rewardApproachWaypoint;
    public float RewardAllWaypointsReached => rewardAllWaypointsReached;
    public float PenaltyCollision => penaltyCollision;
    public float PenaltyBoundaryHit => penaltyBoundaryHit;
    public float PenaltyTimeStep => penaltyTimeStep;
    public float PenaltyTimeout => penaltyTimeout;
    public float MaxEpisodeTime => maxEpisodeTime;
    public float WaypointVisualSize => waypointVisualSize;
    public float WaypointMinDistance => waypointMinDistance;
    public float WaypointMaxDistance => waypointMaxDistance;
    public float WaypointReachedRadius => waypointReachedRadius;

    // Gizmo accessors
    public bool ShowWaypointGizmos => showWaypointGizmos;
    public Color GizmoWaypointCurrent => waypointCurrentColor;
    public Color GizmoWaypointUpcoming => waypointUpcomingColor;
    public Color GizmoWaypointReached => waypointReachedColor;
    public Color GizmoWaypointPath => waypointPathColor;
    public Color GizmoAgentToWaypoint => agentToWaypointLineColor;
    public bool ShowRaycastGizmos => showRaycastGizmos;
    public Color GizmoRaycastMiss => raycastMissColor;
    public Color GizmoRaycastHit => raycastHitColor;

    /// <summary>
    /// Returns the CarAgent instance in this area.
    /// </summary>
    public CarAgent Agent => carAgentInstance;

    /// <summary>
    /// Returns the WaypointManager for this area.
    /// </summary>
    public WaypointManager Waypoints => waypointManager;

    private void Awake()
    {
        waypointManager = GetComponentInChildren<WaypointManager>();
        if (waypointManager == null)
        {
            Debug.LogError($"[TrainingArea] {name}: No WaypointManager found in children!");
        }

        carAgentInstance = GetComponentInChildren<CarAgent>();
        if (carAgentInstance == null && carAgentPrefab != null)
        {
            GameObject agentObj = Instantiate(carAgentPrefab, transform.position, Quaternion.identity, transform);
            carAgentInstance = agentObj.GetComponent<CarAgent>();
        }

        if (carAgentInstance == null)
        {
            Debug.LogError($"[TrainingArea] {name}: No CarAgent found or instantiated!");
        }

        ValidateLayers();
        CreateBoundaryWalls();
    }

    private void ValidateLayers()
    {
        int boundaryLayer = LayerMask.NameToLayer("Boundary");
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        int agentLayer = LayerMask.NameToLayer("Agent");

        if (boundaryLayer == -1)
            Debug.LogError("[TrainingArea] Layer 'Boundary' does not exist! Go to Edit → Project Settings → Tags and Layers and add it.");
        if (obstacleLayer == -1)
            Debug.LogError("[TrainingArea] Layer 'Obstacle' does not exist! Go to Edit → Project Settings → Tags and Layers and add it.");
        if (agentLayer == -1)
            Debug.LogError("[TrainingArea] Layer 'Agent' does not exist! Go to Edit → Project Settings → Tags and Layers and add it.");

        if (carAgentInstance != null && agentLayer != -1)
        {
            if (carAgentInstance.gameObject.layer != agentLayer)
                Debug.LogError($"[TrainingArea] CarAgent '{carAgentInstance.name}' is on layer '{LayerMask.LayerToName(carAgentInstance.gameObject.layer)}' but must be on 'Agent' layer!");
        }

        if (boundaryLayer != -1 && agentLayer != -1)
        {
            if (Physics2D.GetIgnoreLayerCollision(agentLayer, boundaryLayer))
                Debug.LogError("[TrainingArea] Agent ↔ Boundary collision is DISABLED in Physics2D! Go to Edit → Project Settings → Physics 2D → Layer Collision Matrix and enable it.");
        }

        if (obstacleLayer != -1 && agentLayer != -1)
        {
            if (Physics2D.GetIgnoreLayerCollision(agentLayer, obstacleLayer))
                Debug.LogError("[TrainingArea] Agent ↔ Obstacle collision is DISABLED in Physics2D! Go to Edit → Project Settings → Physics 2D → Layer Collision Matrix and enable it.");
        }
    }

    /// <summary>
    /// Resets the episode: spawns the agent near a border and generates waypoints.
    /// </summary>
    public void ResetEpisode()
    {
        episodeTimer = 0f;
        episodeActive = true;

        Vector2 areaCenter = (Vector2)transform.position;
        float halfSize = areaSize / 2f;

        // 1) Spawn agent close to a random border
        Vector2 agentSpawnPos = GetBorderSpawnPosition(areaCenter, halfSize);

        // 2) First waypoint near the center
        float centerOffset = Random.Range(0f, firstWaypointCenterRadius);
        float centerAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 firstWaypointPos = areaCenter + new Vector2(Mathf.Cos(centerAngle), Mathf.Sin(centerAngle)) * centerOffset;

        // 3) Second waypoint on the opposite side of the agent
        Vector2 secondWaypointPos = GetOppositeWaypointPosition(agentSpawnPos, areaCenter, halfSize);

        // 4) Compute agent facing toward first waypoint with angle offset
        Vector2 dirToFirstWaypoint = (firstWaypointPos - agentSpawnPos).normalized;
        float baseAngle = Mathf.Atan2(dirToFirstWaypoint.y, dirToFirstWaypoint.x) * Mathf.Rad2Deg - 90f;
        float angleOffset = Random.Range(agentSpawnMinAngleOffset, agentSpawnMaxAngleOffset);
        if (Random.value > 0.5f) angleOffset = -angleOffset;
        float finalAngle = baseAngle + angleOffset;

        if (carAgentInstance != null)
        {
            carAgentInstance.transform.position = new Vector3(agentSpawnPos.x, agentSpawnPos.y, 0f);
            carAgentInstance.transform.rotation = Quaternion.Euler(0f, 0f, finalAngle);
            CarController controller = carAgentInstance.GetComponent<CarController>();
            if (controller != null) controller.ResetPhysics();
        }

        // 5) Generate waypoints: first at center, second opposite, rest random
        if (waypointManager != null)
        {
            if (carAgentInstance != null)
                waypointManager.SetAgentTransform(carAgentInstance.transform);

            waypointManager.SetTrainingArea(this);
            waypointManager.SetReachedRadius(waypointReachedRadius);
            waypointManager.GenerateWaypoints(
                waypointCount,
                waypointMinDistance,
                waypointMaxDistance,
                new Vector2(areaSize, areaSize),
                areaCenter,
                waypointVisualSize,
                firstWaypointPos,
                secondWaypointPos
            );
        }
    }

    private Vector2 GetBorderSpawnPosition(Vector2 center, float halfSize)
    {
        // Pick a random border: 0=top, 1=bottom, 2=left, 3=right
        int border = Random.Range(0, 4);
        float borderDist = Random.Range(agentBorderMinDistance, agentBorderMaxDistance);
        float lateral = Random.Range(-halfSize + 3f, halfSize - 3f);

        switch (border)
        {
            case 0: return center + new Vector2(lateral, halfSize - borderDist);
            case 1: return center + new Vector2(lateral, -halfSize + borderDist);
            case 2: return center + new Vector2(-halfSize + borderDist, lateral);
            case 3: return center + new Vector2(halfSize - borderDist, lateral);
            default: return center;
        }
    }

    private Vector2 GetOppositeWaypointPosition(Vector2 agentPos, Vector2 center, float halfSize)
    {
        // Direction from agent to center, extended to the opposite side
        Vector2 agentToCenter = (center - agentPos).normalized;
        float borderDist = Random.Range(secondWaypointBorderMinDistance, secondWaypointBorderMaxDistance);

        // Project to opposite border
        float oppositeX = agentPos.x + agentToCenter.x * areaSize;
        float oppositeY = agentPos.y + agentToCenter.y * areaSize;

        // Clamp to area bounds with border distance
        oppositeX = Mathf.Clamp(oppositeX, center.x - halfSize + borderDist, center.x + halfSize - borderDist);
        oppositeY = Mathf.Clamp(oppositeY, center.y - halfSize + borderDist, center.y + halfSize - borderDist);

        // Add lateral spread perpendicular to the agent-to-center direction
        Vector2 perpendicular = new Vector2(-agentToCenter.y, agentToCenter.x);
        float lateralOffset = Random.Range(-secondWaypointLateralSpread, secondWaypointLateralSpread);
        oppositeX += perpendicular.x * lateralOffset;
        oppositeY += perpendicular.y * lateralOffset;

        // Final clamp
        oppositeX = Mathf.Clamp(oppositeX, center.x - halfSize + 2f, center.x + halfSize - 2f);
        oppositeY = Mathf.Clamp(oppositeY, center.y - halfSize + 2f, center.y + halfSize - 2f);

        return new Vector2(oppositeX, oppositeY);
    }

    /// <summary>
    /// Called every FixedUpdate by the agent to check timeout.
    /// Returns true if the episode has timed out.
    /// </summary>
    public bool UpdateTimer()
    {
        if (!episodeActive) return false;

        episodeTimer += Time.fixedDeltaTime;
        return episodeTimer >= maxEpisodeTime;
    }

    /// <summary>
    /// Logs the end-of-episode summary with reason and cumulative reward.
    /// </summary>
    public void LogEpisodeEnd(string reason, float cumulativeReward)
    {
        Debug.Log($"[TrainingArea] {name} | Episode End: {reason} | Cumulative Reward: {cumulativeReward:F3}");
        episodeActive = false;
    }

    private void CreateBoundaryWalls()
    {
        float half = areaSize / 2f;
        float t = boundaryThickness;
        Vector2 center = (Vector2)transform.position;

        CreateWall("BoundaryTop", center + new Vector2(0, half + t / 2f), new Vector2(areaSize + t * 2f, t));
        CreateWall("BoundaryBottom", center + new Vector2(0, -half - t / 2f), new Vector2(areaSize + t * 2f, t));
        CreateWall("BoundaryLeft", center + new Vector2(-half - t / 2f, 0), new Vector2(t, areaSize + t * 2f));
        CreateWall("BoundaryRight", center + new Vector2(half + t / 2f, 0), new Vector2(t, areaSize + t * 2f));
    }

    private void CreateWall(string wallName, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.parent = transform;
        wall.transform.position = new Vector3(position.x, position.y, 0f);
        wall.transform.localScale = new Vector3(size.x, size.y, 1f);

        int layer = LayerMask.NameToLayer("Boundary");
        if (layer != -1)
            wall.layer = layer;
        else
            Debug.LogError($"[TrainingArea] Cannot set wall '{wallName}' to Boundary layer — layer does not exist! Collisions will NOT work.");

        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
        sr.sprite = MakeWhiteSquareSprite();
        sr.color = boundaryColor;
        sr.sortingOrder = -1;
    }

    private static Sprite cachedWhiteSprite;

    private static Sprite MakeWhiteSquareSprite()
    {
        if (cachedWhiteSprite != null) return cachedWhiteSprite;

        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return cachedWhiteSprite;
    }

    private void OnDrawGizmos()
    {
        Vector3 center = transform.position;

        if (showAreaBoundaryGizmo)
        {
            Gizmos.color = areaBoundaryGizmoColor;
            Gizmos.DrawWireCube(center, new Vector3(areaSize, areaSize, 0f));
        }

        if (showSpawnZoneGizmo)
        {
            // Draw two rectangles showing min and max border spawn distance
            Gizmos.color = spawnZoneGizmoColor;
            float innerMin = agentBorderMinDistance * 2f;
            float innerMax = agentBorderMaxDistance * 2f;
            Gizmos.DrawWireCube(center, new Vector3(areaSize - innerMin, areaSize - innerMin, 0f));
            Gizmos.color = new Color(spawnZoneGizmoColor.r, spawnZoneGizmoColor.g, spawnZoneGizmoColor.b, spawnZoneGizmoColor.a * 0.5f);
            Gizmos.DrawWireCube(center, new Vector3(areaSize - innerMax, areaSize - innerMax, 0f));
        }

        if (showCenterRadiusGizmo)
        {
            Gizmos.color = centerRadiusGizmoColor;
            DrawGizmoCircle(center, firstWaypointCenterRadius);
        }
    }

    private void DrawGizmoCircle(Vector3 center, float radius)
    {
        int segments = 36;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}
