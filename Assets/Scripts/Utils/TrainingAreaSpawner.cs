using UnityEngine;

/// <summary>
/// Instantiates copies of the TrainingArea prefab in a 2D grid at startup
/// to run many agents in parallel during training.
/// </summary>
public class TrainingAreaSpawner : MonoBehaviour
{
    [Tooltip("The TrainingArea prefab to duplicate. Each copy is a fully independent training environment with its own agent, waypoints, and obstacles. Assign the TrainingArea prefab here. Recommended: one prefab reference shared by all spawned areas.")]
    [SerializeField] private GameObject trainingAreaPrefab;

    [Tooltip("Number of training areas along the X axis. Increasing this adds more parallel environments horizontally, speeding up training but using more memory. Decreasing reduces parallel environments. Recommended: 3–10 depending on hardware.")]
    [SerializeField] private int areaCountX = 5;

    [Tooltip("Number of training areas along the Y axis. Increasing this adds more parallel environments vertically, speeding up training but using more memory. Decreasing reduces parallel environments. Recommended: 3–10 depending on hardware.")]
    [SerializeField] private int areaCountY = 5;

    [Tooltip("Extra gap in world units between adjacent training areas. Increasing this separates areas further, reducing visual clutter and preventing accidental cross-area interactions. Decreasing packs areas tighter. Recommended: 5–20 world units.")]
    [SerializeField] private float areaPadding = 10f;

    private void Awake()
    {
        if (trainingAreaPrefab == null)
        {
            Debug.LogError("[TrainingAreaSpawner] trainingAreaPrefab is not assigned!");
            return;
        }

        TrainingArea templateArea = trainingAreaPrefab.GetComponent<TrainingArea>();
        if (templateArea == null)
        {
            Debug.LogError("[TrainingAreaSpawner] trainingAreaPrefab does not have a TrainingArea component!");
            return;
        }

        float areaSize = templateArea.AreaSize;
        float spacing = areaSize + areaPadding;

        float totalWidth = areaCountX * spacing;
        float totalHeight = areaCountY * spacing;
        Vector2 startOffset = new Vector2(-totalWidth / 2f + spacing / 2f, -totalHeight / 2f + spacing / 2f);

        for (int x = 0; x < areaCountX; x++)
        {
            for (int y = 0; y < areaCountY; y++)
            {
                Vector3 position = new Vector3(
                    startOffset.x + x * spacing,
                    startOffset.y + y * spacing,
                    0f
                );

                GameObject areaInstance = Instantiate(trainingAreaPrefab, position, Quaternion.identity, transform);
                areaInstance.name = $"TrainingArea_{x}_{y}";
            }
        }
    }
}
