using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

/// <summary>
/// ML-Agents agent controlling a 2D top-down car. Observes surroundings via raycasts
/// and waypoint information, and learns to navigate toward waypoints while avoiding obstacles.
/// </summary>
[RequireComponent(typeof(CarController))]
public class CarAgent : Agent
{
    [Header("References")]

    [Tooltip("Reference to the CarController component on this GameObject. Handles physics-based movement. Assign this in the Inspector or it will be auto-detected.")]
    [SerializeField] private CarController carController;

    [Tooltip("Reference to the WaypointManager that tracks waypoint positions and progression. Must be assigned — found in the parent TrainingArea.")]
    [SerializeField] private WaypointManager waypointManager;

    [Tooltip("Reference to the TrainingArea that manages this agent's episode. All reward values are read from this component. Must be assigned.")]
    [SerializeField] private TrainingArea trainingArea;

    [Header("Raycasts")]

    [Tooltip("Number of rays evenly distributed 360° around the car for obstacle detection. Total observation count = 11 + rayCastCount. If you change this value, you MUST update the vector_observation_size in your YAML behavior configuration file. Increasing gives the agent better spatial awareness but increases observation space. Decreasing makes the agent partially blind. Recommended: 16–36.")]
    [SerializeField] private int rayCastCount = 24;

    [Tooltip("Length of each ray in world units. Increasing lets the agent see further, giving it more time to react to obstacles. Decreasing limits vision range, making the agent more reactive and less predictive. Recommended: 8–20.")]
    [SerializeField] private float rayCastLength = 12f;

    [Tooltip("LayerMask for raycast detection. Must include Obstacle and Boundary layers. WARNING: an incorrect mask means the agent cannot see walls or obstacles and will never learn to avoid them. Always verify this is set correctly in the Inspector.")]
    [SerializeField] private LayerMask obstacleLayer;

    private float prevDistToWaypoint;
    private float areaDiagonal;

    /// <summary>
    /// Called once when the agent is first enabled.
    /// </summary>
    public override void Initialize()
    {
        if (carController == null)
            carController = GetComponent<CarController>();

        if (carController == null)
            Debug.LogError($"[CarAgent] {name}: CarController not found!");
        if (waypointManager == null)
            Debug.LogError($"[CarAgent] {name}: WaypointManager is not assigned!");
        if (trainingArea == null)
            Debug.LogError($"[CarAgent] {name}: TrainingArea is not assigned!");

        if (trainingArea != null)
            areaDiagonal = trainingArea.AreaSize * Mathf.Sqrt(2f);
    }

    /// <summary>
    /// Called at the beginning of each training episode.
    /// Resets the environment and agent state.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (trainingArea == null)
        {
            Debug.LogError($"[CarAgent] {name}: TrainingArea is null in OnEpisodeBegin!");
            return;
        }
        if (waypointManager == null)
        {
            Debug.LogError($"[CarAgent] {name}: WaypointManager is null in OnEpisodeBegin!");
            return;
        }
        if (carController == null)
        {
            Debug.LogError($"[CarAgent] {name}: CarController is null in OnEpisodeBegin!");
            return;
        }

        trainingArea.ResetEpisode();
        areaDiagonal = trainingArea.AreaSize * Mathf.Sqrt(2f);

        prevDistToWaypoint = waypointManager.GetDistanceToCurrentWaypoint(transform.position);
    }

    /// <summary>
    /// Collects all observations for the agent's neural network input.
    /// Total: 11 + rayCastCount floats.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        Vector2 agentPos = (Vector2)transform.position;

        // 1-2: Direction to current waypoint (normalized Vector2)
        Vector2 dirToWaypoint = waypointManager.GetDirectionToCurrentWaypoint(agentPos);
        sensor.AddObservation(dirToWaypoint.x);
        sensor.AddObservation(dirToWaypoint.y);

        // 3: Distance to current waypoint (normalized by area diagonal)
        float distToWaypoint = waypointManager.GetDistanceToCurrentWaypoint(agentPos);
        sensor.AddObservation(distToWaypoint / areaDiagonal);

        // 4-5: Agent velocity (normalized by maxSpeed)
        Vector2 velocity = carController.Velocity;
        sensor.AddObservation(velocity.x / carController.MaxSpeed);
        sensor.AddObservation(velocity.y / carController.MaxSpeed);

        // 6: Agent speed (normalized by maxSpeed)
        sensor.AddObservation(carController.Speed / carController.MaxSpeed);

        // 7-8: Agent heading as sin/cos (avoids 0/360 discontinuity)
        float rotation = transform.eulerAngles.z * Mathf.Deg2Rad;
        sensor.AddObservation(Mathf.Sin(rotation));
        sensor.AddObservation(Mathf.Cos(rotation));

        // 9: Waypoints remaining (normalized 0-1)
        sensor.AddObservation(waypointManager.GetNormalizedRemainingWaypoints());

        // 10-11: Direction to next waypoint after current
        Vector2 dirToNext = waypointManager.GetDirectionToNextWaypoint(agentPos);
        sensor.AddObservation(dirToNext.x);
        sensor.AddObservation(dirToNext.y);

        // 12-(11+N): Raycasts - N evenly-spaced rays, normalized hit distance
        float angleStep = 360f / rayCastCount;
        for (int i = 0; i < rayCastCount; i++)
        {
            float angle = (transform.eulerAngles.z + i * angleStep) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            RaycastHit2D hit = Physics2D.Raycast(agentPos, direction, rayCastLength, obstacleLayer);

            if (hit.collider != null)
            {
                sensor.AddObservation(hit.distance / rayCastLength);
            }
            else
            {
                sensor.AddObservation(1.0f);
            }
        }
    }

    /// <summary>
    /// Called each step with the agent's action decisions.
    /// Applies movement, checks waypoints, and distributes rewards.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        float throttle = actions.ContinuousActions[0];
        float steer = actions.ContinuousActions[1];

        carController.Move(throttle, steer);

        // Time step penalty
        AddReward(trainingArea.PenaltyTimeStep);

        // Check waypoint reached
        Vector2 agentPos = (Vector2)transform.position;
        float distToWaypoint = waypointManager.GetDistanceToCurrentWaypoint(agentPos);

        if (distToWaypoint <= waypointManager.WaypointReachedRadius)
        {
            AddReward(trainingArea.RewardWaypointReached);

            bool hasMore = waypointManager.AdvanceWaypoint();
            if (!hasMore)
            {
                AddReward(trainingArea.RewardAllWaypointsReached);
                trainingArea.LogEpisodeEnd("AllWaypointsReached", GetCumulativeReward());
                EndEpisode();
                return;
            }

            prevDistToWaypoint = waypointManager.GetDistanceToCurrentWaypoint(agentPos);
        }
        else
        {
            // Approach reward
            if (distToWaypoint < prevDistToWaypoint)
            {
                AddReward(trainingArea.RewardApproachWaypoint);
            }
            prevDistToWaypoint = distToWaypoint;
        }

        // Timeout check
        if (trainingArea.UpdateTimer())
        {
            AddReward(trainingArea.PenaltyTimeout);
            trainingArea.LogEpisodeEnd("Timeout", GetCumulativeReward());
            EndEpisode();
        }
    }

    /// <summary>
    /// Allows manual control of the agent for testing via keyboard.
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Vertical");
        continuousActions[1] = Input.GetAxis("Horizontal");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int layer = collision.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Boundary"))
        {
            AddReward(trainingArea.PenaltyBoundaryHit);
            trainingArea.LogEpisodeEnd("BoundaryHit", GetCumulativeReward());
            EndEpisode();
        }
        else if (layer == LayerMask.NameToLayer("Obstacle"))
        {
            AddReward(trainingArea.PenaltyCollision);
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (trainingArea != null && !trainingArea.ShowRaycastGizmos) return;

        Color missColor = trainingArea != null ? trainingArea.GizmoRaycastMiss : Color.green;
        Color hitColor = trainingArea != null ? trainingArea.GizmoRaycastHit : Color.red;

        Vector2 agentPos = (Vector2)transform.position;
        float angleStep = 360f / rayCastCount;

        for (int i = 0; i < rayCastCount; i++)
        {
            float angle = (transform.eulerAngles.z + i * angleStep) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            RaycastHit2D hit = Physics2D.Raycast(agentPos, direction, rayCastLength, obstacleLayer);

            if (hit.collider != null)
            {
                Gizmos.color = hitColor;
                Gizmos.DrawLine((Vector3)agentPos, (Vector3)hit.point);
                Gizmos.DrawSphere((Vector3)hit.point, 0.15f);
            }
            else
            {
                Gizmos.color = missColor;
                Vector3 endPoint = (Vector3)agentPos + (Vector3)(direction * rayCastLength);
                Gizmos.DrawLine((Vector3)agentPos, endPoint);
            }
        }
    }
}
