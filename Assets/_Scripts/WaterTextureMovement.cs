using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class WaterTextureMovement : MonoBehaviour
{
    [SerializeField] private Vector2 speed = new(0.02f, 0.01f);

    private Renderer waterRenderer;
    private MaterialPropertyBlock properties;
    private Vector2 offset;

    private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");

    private void Awake()
    {
        waterRenderer = GetComponent<Renderer>();
        properties = new MaterialPropertyBlock();
    }

    private void Update()
    {
        offset += speed * Time.deltaTime;

        waterRenderer.GetPropertyBlock(properties);

        Vector2 tiling = waterRenderer.sharedMaterial.GetTextureScale("_BaseMap");

        properties.SetVector(BaseMapST, new Vector4(tiling.x, tiling.y, offset.x, offset.y));
        waterRenderer.SetPropertyBlock(properties);
    }
}
