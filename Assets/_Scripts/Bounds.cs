using UnityEngine;

public class Bounds : MonoBehaviour
{
    [Header("Bounds")]
    [SerializeField] private float height = 10f;
    [SerializeField] private float width = 10f;

    [Header("Visualizer")]
    [SerializeField] private Transform visualizer;
    [SerializeField] private MeshFilter visualizerMesh;
    [SerializeField] private float visualizerYOffset = 0f;

    [Header("Texture Tiling")]
    [SerializeField] private Renderer visualizerRenderer;
    [SerializeField, Min(0.1f)] private float worldUnitsPerTile = 2f;
    [SerializeField] private int textureSeed = 12345;
    [SerializeField] private bool randomizeTextureOffset = true;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;

    public static Bounds Instance { get; private set; }
    public float LimitHeight => height / 2;
    public float LimitWidth => width / 2;

    private MaterialPropertyBlock _materialProperties;

    public void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);

        UpdateVisualizer();
    }

    private void OnValidate()
    {
        if (visualizerRenderer == null && visualizer != null)
            visualizerRenderer = visualizer.GetComponent<Renderer>();

        if (visualizerMesh == null && visualizer != null)
            visualizerMesh = visualizer.GetComponent<MeshFilter>();

        UpdateVisualizer();
    }

    [ContextMenu("Update Visualizer")]
    private void UpdateVisualizer()
    {
        if (visualizer == null || visualizerMesh == null)
            return;

        Mesh mesh = visualizerMesh.sharedMesh;

        if (mesh == null)
            return;

        Vector3 meshSize = mesh.bounds.size;

        if (meshSize.x <= 0f || meshSize.z <= 0f)
            return;

        visualizer.localPosition = new Vector3(0f, visualizerYOffset, 0f);

        Vector3 scale = visualizer.localScale;
        scale.x = width / meshSize.x;
        scale.z = height / meshSize.z;

        visualizer.localScale = scale;

        UpdateTextureTiling();
    }

    private void UpdateTextureTiling()
    {
        if (visualizerRenderer == null)
            return;

        _materialProperties ??= new MaterialPropertyBlock();
        visualizerRenderer.GetPropertyBlock(_materialProperties);

        float tilingX = width / worldUnitsPerTile;
        float tilingY = height / worldUnitsPerTile;

        float offsetX = 0f;
        float offsetY = 0f;

        if (randomizeTextureOffset)
        {
            int seed = textureSeed;

            unchecked
            {
                seed = seed * 31 + Mathf.RoundToInt(transform.position.x * 100f);
                seed = seed * 31 + Mathf.RoundToInt(transform.position.z * 100f);
            }

            System.Random random = new System.Random(seed);

            offsetX = (float)random.NextDouble();
            offsetY = (float)random.NextDouble();
        }

        Vector4 textureST = new(tilingX, tilingY, offsetX, offsetY);
        // URP/Lit
        _materialProperties.SetVector("_BaseMap_ST", textureST);
        // Standard Shader and other commonly use shaders
        _materialProperties.SetVector("_MainTex_ST", textureST);

        visualizerRenderer.SetPropertyBlock(_materialProperties);
    }

    public Vector3 OutOfBounds(Vector3 position)
    {
        Vector3 center = transform.position;
        Vector3 result = position;

        float minX = center.x - LimitWidth;
        float maxX = center.x + LimitWidth;
        float minZ = center.z - LimitHeight;
        float maxZ = center.z + LimitHeight;

        if (position.x > maxX)
            result.x = minX;
        else if (position.x < minX)
            result.x = maxX;

        if (position.z > maxZ)
            result.z = minZ;
        else if (position.z < minZ)
            result.z = maxZ;

        return result;
    }

    public Vector3 GetRandomPosition()
    {
        Vector3 center = transform.position;

        Vector3 rndPosition = new(Random.Range(center.x - LimitWidth, center.x + LimitWidth), center.y, Random.Range(center.z - LimitHeight, center.z + LimitHeight));
        return rndPosition;
    }


    private void OnDrawGizmos()
    {
        if(!drawGizmos) 
            return;

        Gizmos.color = Color.blueViolet;
        Gizmos.DrawWireCube(transform.position, new Vector3(width, 0, height));
    }
}
