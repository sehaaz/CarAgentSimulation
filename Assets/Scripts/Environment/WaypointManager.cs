using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates and tracks the waypoint sequence for one episode within a training area.
/// </summary>
public class WaypointManager : MonoBehaviour
{
    [Tooltip("Visual marker prefab placed at each waypoint position. This prefab is instantiated at each generated waypoint location and destroyed on reset. It should be a small sprite with the Waypoint tag. Recommended: a simple circle or diamond sprite.")]
    [SerializeField] private GameObject waypointPrefab;

    [Tooltip("Distance in world units at which the agent is considered to have reached a waypoint. Setting this too small makes it very hard for the agent to precisely hit waypoints, slowing learning dramatically. Setting this too large makes the task trivially easy and the agent won't learn fine navigation. Recommended: 1.0–3.0 world units.")]
    [SerializeField] private float waypointReachedRadius = 1.5f;

    private List<Vector2> waypointPositions = new List<Vector2>();
    private List<GameObject> waypointObjects = new List<GameObject>();
    private int currentWaypointIndex;
    private int totalWaypointCount;
    private Vector2 areaCenter;
    private Transform agentTransform;
    private TrainingArea trainingArea;

    private const int MAX_PLACEMENT_ATTEMPTS = 30;

    /// <summary>
    /// The radius within which a waypoint is considered reached.
    /// </summary>
    public float WaypointReachedRadius => waypointReachedRadius;

    /// <summary>
    /// Override the reached radius at runtime (called by TrainingArea).
    /// </summary>
    public void SetReachedRadius(float radius)
    {
        waypointReachedRadius = radius;
    }

    /// <summary>
    /// Sets the agent transform reference for gizmo drawing (agent-to-waypoint line).
    /// </summary>
    public void SetAgentTransform(Transform agent)
    {
        agentTransform = agent;
    }

    /// <summary>
    /// Sets the TrainingArea reference for reading gizmo settings.
    /// </summary>
    public void SetTrainingArea(TrainingArea area)
    {
        trainingArea = area;
    }

    /// <summary>
    /// Place waypoints within bounds. First waypoint at firstPosition (center),
    /// second at secondPosition (opposite side of agent), rest placed randomly
    /// respecting distance constraints.
    /// </summary>
    public void GenerateWaypoints(int count, float minDist, float maxDist, Vector2 areaBounds, Vector2 center, float visualSize, Vector2 firstPosition, Vector2 secondPosition)
    {
        ClearWaypoints();

        areaCenter = center;
        currentWaypointIndex = 0;
        totalWaypointCount = count;

        float halfWidth = areaBounds.x / 2f;
        float halfHeight = areaBounds.y / 2f;

        for (int i = 0; i < count; i++)
        {
            Vector2 newPosition;

            if (i == 0)
            {
                newPosition = firstPosition;
            }
            else if (i == 1)
            {
                newPosition = secondPosition;
            }
            else
            {
                Vector2 previousPosition = waypointPositions[i - 1];
                newPosition = Vector2.zero;
                bool placed = false;

                for (int attempt = 0; attempt < MAX_PLACEMENT_ATTEMPTS; attempt++)
                {
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float distance = Random.Range(minDist, maxDist);
                    Vector2 candidate = previousPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

                    if (candidate.x >= center.x - halfWidth + 1f && candidate.x <= center.x + halfWidth - 1f &&
                        candidate.y >= center.y - halfHeight + 1f && candidate.y <= center.y + halfHeight - 1f)
                    {
                        newPosition = candidate;
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    float fallbackAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    Vector2 fallback = previousPosition + new Vector2(Mathf.Cos(fallbackAngle), Mathf.Sin(fallbackAngle)) * maxDist;
                    fallback.x = Mathf.Clamp(fallback.x, center.x - halfWidth + 1f, center.x + halfWidth - 1f);
                    fallback.y = Mathf.Clamp(fallback.y, center.y - halfHeight + 1f, center.y + halfHeight - 1f);
                    newPosition = fallback;
                }
            }

            waypointPositions.Add(newPosition);

            if (waypointPrefab != null)
            {
                GameObject wpObj = Instantiate(waypointPrefab, new Vector3(newPosition.x, newPosition.y, 0f), Quaternion.identity, transform);
                wpObj.transform.localScale = Vector3.one * visualSize;
                wpObj.name = $"Waypoint_{i}";
                waypointObjects.Add(wpObj);
            }
        }

        UpdateWaypointVisuals();
    }

    /// <summary>
    /// Returns the world-space position of the currently active waypoint.
    /// </summary>
    public Vector2 GetCurrentWaypointPosition()
    {
        if (currentWaypointIndex < waypointPositions.Count)
            return waypointPositions[currentWaypointIndex];
        return areaCenter;
    }

    /// <summary>
    /// Returns a normalized Vector2 direction from the agent to the current waypoint.
    /// </summary>
    public Vector2 GetDirectionToCurrentWaypoint(Vector2 agentPosition)
    {
        Vector2 target = GetCurrentWaypointPosition();
        Vector2 direction = target - agentPosition;
        return direction.normalized;
    }

    /// <summary>
    /// Returns a normalized Vector2 direction from the agent to the next waypoint after current.
    /// If there is no next waypoint, returns the direction to the current waypoint.
    /// </summary>
    public Vector2 GetDirectionToNextWaypoint(Vector2 agentPosition)
    {
        int nextIndex = currentWaypointIndex + 1;
        if (nextIndex < waypointPositions.Count)
        {
            Vector2 direction = waypointPositions[nextIndex] - agentPosition;
            return direction.normalized;
        }
        return GetDirectionToCurrentWaypoint(agentPosition);
    }

    /// <summary>
    /// Advance to the next waypoint in the sequence.
    /// Returns false if the last waypoint was just reached (all complete).
    /// </summary>
    public bool AdvanceWaypoint()
    {
        if (currentWaypointIndex < waypointObjects.Count)
        {
            waypointObjects[currentWaypointIndex].SetActive(false);
        }

        currentWaypointIndex++;
        UpdateWaypointVisuals();
        return currentWaypointIndex < waypointPositions.Count;
    }

    /// <summary>
    /// Returns remaining waypoint count normalized to 0–1 range.
    /// 1.0 means all waypoints remain, 0.0 means all have been reached.
    /// </summary>
    public float GetNormalizedRemainingWaypoints()
    {
        if (totalWaypointCount <= 0) return 0f;
        int remaining = waypointPositions.Count - currentWaypointIndex;
        return (float)remaining / totalWaypointCount;
    }

    /// <summary>
    /// Returns the distance from the agent to the current waypoint.
    /// </summary>
    public float GetDistanceToCurrentWaypoint(Vector2 agentPosition)
    {
        return Vector2.Distance(agentPosition, GetCurrentWaypointPosition());
    }

    /// <summary>
    /// Returns true if all waypoints have been reached.
    /// </summary>
    public bool AllWaypointsReached()
    {
        return currentWaypointIndex >= waypointPositions.Count;
    }

    private void ClearWaypoints()
    {
        foreach (GameObject wp in waypointObjects)
        {
            if (wp != null) Destroy(wp);
        }
        waypointObjects.Clear();
        waypointPositions.Clear();
        currentWaypointIndex = 0;
    }

    private void UpdateWaypointVisuals()
    {
        for (int i = 0; i < waypointObjects.Count; i++)
        {
            if (waypointObjects[i] != null)
            {
                waypointObjects[i].SetActive(i >= currentWaypointIndex);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (waypointPositions.Count == 0) return;
        if (trainingArea != null && !trainingArea.ShowWaypointGizmos) return;

        Color currentCol = trainingArea != null ? trainingArea.GizmoWaypointCurrent : Color.yellow;
        Color upcomingCol = trainingArea != null ? trainingArea.GizmoWaypointUpcoming : Color.cyan;
        Color reachedCol = trainingArea != null ? trainingArea.GizmoWaypointReached : Color.gray;
        Color pathCol = trainingArea != null ? trainingArea.GizmoWaypointPath : Color.white;
        Color agentLineCol = trainingArea != null ? trainingArea.GizmoAgentToWaypoint : Color.green;

        for (int i = 0; i < waypointPositions.Count; i++)
        {
            Vector3 pos = new Vector3(waypointPositions[i].x, waypointPositions[i].y, 0f);

            if (i < currentWaypointIndex)
                Gizmos.color = reachedCol;
            else if (i == currentWaypointIndex)
                Gizmos.color = currentCol;
            else
                Gizmos.color = upcomingCol;

            Gizmos.DrawWireSphere(pos, waypointReachedRadius);

            if (i > 0)
            {
                Vector3 prevPos = new Vector3(waypointPositions[i - 1].x, waypointPositions[i - 1].y, 0f);
                Gizmos.color = i <= currentWaypointIndex ? reachedCol : pathCol;
                Gizmos.DrawLine(prevPos, pos);
            }
        }

        if (agentTransform != null && currentWaypointIndex < waypointPositions.Count)
        {
            Vector3 agentPos = agentTransform.position;
            Vector3 wpPos = new Vector3(waypointPositions[currentWaypointIndex].x, waypointPositions[currentWaypointIndex].y, 0f);
            Gizmos.color = agentLineCol;
            Gizmos.DrawLine(agentPos, wpPos);
        }
    }
}
