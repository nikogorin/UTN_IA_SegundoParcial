using UnityEngine;

public class SurfaceVisualizer : MonoBehaviour
{
    [Header("Surface size")]
    [SerializeField, Min(0.1f)] private float width = 10f;
    [SerializeField, Min(0.1f)] private float depth = 10f;

    [Header("Visualizer")]
    [SerializeField] private Transform visualizer;
    [SerializeField] private MeshFilter visualizerMesh;
    [SerializeField] private Renderer visualizerRenderer;
    [SerializeField] private float visualizerYOffset;

    [Header("Texture tiling")]
    [SerializeField, Min(0.1f)] private float worldUnitsPerTile = 2f;
    [SerializeField] private int textureSeed = 12345;
    [SerializeField] private bool randomizeTextureOffset = true;

    private MaterialPropertyBlock _materialProperties;

    public float Width => width;
    public float Depth => depth;

    private void Awake()
    {
        Refresh();
    }

    private void OnValidate()
    {
        FindReferences();
        Refresh();
    }

    private void FindReferences()
    {
        if (visualizer == null && transform.childCount > 0)
            visualizer = transform.GetChild(0);

        if (visualizer == null)
            return;

        if (visualizerMesh == null)
            visualizerMesh = visualizer.GetComponent<MeshFilter>();

        if (visualizerRenderer == null)
            visualizerRenderer = visualizer.GetComponent<Renderer>();
    }

    [ContextMenu("Refresh Surface")]
    public void Refresh()
    {
        UpdateSurfaceSize();
        UpdateTextureTiling();
    }

    private void UpdateSurfaceSize()
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
        scale.z = depth / meshSize.z;

        visualizer.localScale = scale;
    }

    private void UpdateTextureTiling()
    {
        if (visualizerRenderer == null)
            return;

        _materialProperties ??= new MaterialPropertyBlock();

        visualizerRenderer.GetPropertyBlock(_materialProperties);

        float tilingX = width / worldUnitsPerTile;
        float tilingY = depth / worldUnitsPerTile;

        Vector2 offset = GetTextureOffset();

        Vector4 textureST = new Vector4(tilingX, tilingY, offset.x, offset.y);

        // URP/Lit
        _materialProperties.SetVector("_BaseMap_ST", textureST);

        // Standard Shader
        _materialProperties.SetVector("_MainTex_ST", textureST);

        visualizerRenderer.SetPropertyBlock(_materialProperties);
    }

    private Vector2 GetTextureOffset()
    {
        if (!randomizeTextureOffset)
            return Vector2.zero;

        int seed = textureSeed;

        unchecked
        {
            seed = seed * 31 + Mathf.RoundToInt(transform.position.x * 100f);

            seed = seed * 31 + Mathf.RoundToInt(transform.position.z * 100f);
        }

        System.Random random = new(seed);

        return new Vector2((float)random.NextDouble(), (float)random.NextDouble());
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireCube(transform.position, new Vector3(width, 0.05f, depth));
    }
}
