using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    [Header("Grid Ayarlari (Inspector'dan girin)")]
    [Tooltip("Yatayda kac blok (bina) olacak")]
    [SerializeField] private int gridColumns = 5;
    [Tooltip("Dikeyde kac blok (bina) olacak")]
    [SerializeField] private int gridRows = 5;

    [Header("Boyutlar")]
    [Tooltip("Her bina blogunun boyutu (world unit)")]
    [SerializeField] private float blockSize = 4f;
    [Tooltip("Cadde genisligi (world unit)")]
    [SerializeField] private float roadWidth = 2.5f;

    [Header("Renkler")]
    [SerializeField] private Color roadColor = new Color(0.18f, 0.18f, 0.22f);
    [SerializeField] private Color buildingColor = new Color(0.38f, 0.38f, 0.43f);
    [SerializeField] private Color boundaryColor = new Color(0.55f, 0.25f, 0.25f);
    [SerializeField] private Color roadLineColor = new Color(1f, 1f, 1f, 0.45f);

    [Header("Sinir Duvarlari")]
    [SerializeField] private float boundaryThickness = 0.5f;

    [Header("Yol Cizgileri")]
    [SerializeField] private bool showRoadLines = true;
    [SerializeField] private float lineWidth = 0.07f;
    [SerializeField] private float dashLength = 0.5f;
    [SerializeField] private float dashGap = 0.4f;

    public float TotalWidth => gridColumns * blockSize + (gridColumns + 1) * roadWidth;
    public float TotalHeight => gridRows * blockSize + (gridRows + 1) * roadWidth;

    private void Start()
    {
        Generate();
    }

    public void Generate()
    {
        ClearChildren();

        Vector2 origin = (Vector2)transform.position;
        float totalW = TotalWidth;
        float totalH = TotalHeight;

        MakeSprite("Roads", origin, new Vector2(totalW, totalH), roadColor, -10);

        for (int c = 0; c < gridColumns; c++)
        {
            for (int r = 0; r < gridRows; r++)
            {
                float x = origin.x - totalW / 2f + roadWidth + c * (blockSize + roadWidth) + blockSize / 2f;
                float y = origin.y - totalH / 2f + roadWidth + r * (blockSize + roadWidth) + blockSize / 2f;

                Color bColor = buildingColor * Random.Range(0.88f, 1.12f);
                bColor.a = 1f;

                GameObject bld = MakeSprite($"Building_{c}_{r}", new Vector2(x, y),
                    new Vector2(blockSize, blockSize), bColor, -5);
                bld.AddComponent<BoxCollider2D>().size = Vector2.one;

                int layer = LayerMask.NameToLayer("Obstacle");
                if (layer != -1) bld.layer = layer;
            }
        }

        if (showRoadLines)
            MakeRoadLines(origin, totalW, totalH);

        MakeBoundary(origin, totalW, totalH);
    }

    private void MakeRoadLines(Vector2 origin, float totalW, float totalH)
    {
        GameObject linesRoot = new GameObject("RoadLines");
        linesRoot.transform.parent = transform;
        linesRoot.transform.localPosition = Vector3.zero;

        for (int r = 0; r <= gridRows; r++)
        {
            float y = origin.y - totalH / 2f + r * (blockSize + roadWidth) + roadWidth / 2f;
            float startX = origin.x - totalW / 2f;

            GameObject roadParent = new GameObject($"HRoad_{r}");
            roadParent.transform.parent = linesRoot.transform;
            MakeDashes(roadParent.transform, y, startX, totalW, true);
        }

        for (int c = 0; c <= gridColumns; c++)
        {
            float x = origin.x - totalW / 2f + c * (blockSize + roadWidth) + roadWidth / 2f;
            float startY = origin.y - totalH / 2f;

            GameObject roadParent = new GameObject($"VRoad_{c}");
            roadParent.transform.parent = linesRoot.transform;
            MakeDashes(roadParent.transform, x, startY, totalH, false);
        }
    }

    private void MakeDashes(Transform parent, float fixedCoord, float start, float length, bool horizontal)
    {
        float pos = 0;
        int idx = 0;
        while (pos < length)
        {
            float dLen = Mathf.Min(dashLength, length - pos);
            float center = start + pos + dLen / 2f;

            Vector2 p = horizontal ? new Vector2(center, fixedCoord) : new Vector2(fixedCoord, center);
            Vector2 s = horizontal ? new Vector2(dLen, lineWidth) : new Vector2(lineWidth, dLen);

            MakeSprite($"D{idx}", p, s, roadLineColor, -4, parent);
            pos += dashLength + dashGap;
            idx++;
        }
    }

    private void MakeBoundary(Vector2 center, float totalW, float totalH)
    {
        float hw = totalW / 2f;
        float hh = totalH / 2f;
        float t = boundaryThickness;

        MakeBoundaryWall("Top", center + new Vector2(0, hh + t / 2f), new Vector2(totalW + t * 2, t));
        MakeBoundaryWall("Bottom", center + new Vector2(0, -hh - t / 2f), new Vector2(totalW + t * 2, t));
        MakeBoundaryWall("Left", center + new Vector2(-hw - t / 2f, 0), new Vector2(t, totalH + t * 2));
        MakeBoundaryWall("Right", center + new Vector2(hw + t / 2f, 0), new Vector2(t, totalH + t * 2));
    }

    private void MakeBoundaryWall(string name, Vector2 pos, Vector2 size)
    {
        GameObject wall = MakeSprite($"Boundary_{name}", pos, size, boundaryColor, -1);
        wall.AddComponent<BoxCollider2D>().size = Vector2.one;

        int layer = LayerMask.NameToLayer("Boundary");
        if (layer != -1) wall.layer = layer;
    }

    private GameObject MakeSprite(string name, Vector2 pos, Vector2 size, Color color, int order, Transform parent = null)
    {
        GameObject obj = new GameObject(name);
        obj.transform.parent = parent ?? transform;
        obj.transform.position = new Vector3(pos.x, pos.y, 0);
        obj.transform.localScale = new Vector3(size.x, size.y, 1);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSprite();
        sr.color = color;
        sr.sortingOrder = order;

        return obj;
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    private static Sprite _whiteSprite;
    private static Sprite WhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;
        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return _whiteSprite;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(TotalWidth, TotalHeight, 0));

        float totalW = TotalWidth;
        float totalH = TotalHeight;
        Vector2 origin = (Vector2)transform.position;

        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        for (int c = 0; c < gridColumns; c++)
        {
            for (int r = 0; r < gridRows; r++)
            {
                float x = origin.x - totalW / 2f + roadWidth + c * (blockSize + roadWidth) + blockSize / 2f;
                float y = origin.y - totalH / 2f + roadWidth + r * (blockSize + roadWidth) + blockSize / 2f;
                Gizmos.DrawCube(new Vector3(x, y, 0), new Vector3(blockSize, blockSize, 0));
            }
        }
    }
}
