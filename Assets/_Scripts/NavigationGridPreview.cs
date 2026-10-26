using UnityEngine;

public class NavigationGridPreview : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField, Min(1)] private int columns = 20;
    [SerializeField, Min(1)] private int rows = 20;
    [SerializeField, Min(0.1f)] private float nodeSpacing = 2f;

    [Header("Agent dimensions")]
    [SerializeField, Min(0.05f)] private float agentRadius = 0.4f;
    [SerializeField, Min(0.1f)] private float agentHeight = 1.8f;

    [Header("Surface detection")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField, Min(0.1f)] private float groundProbeHeight = 3f;
    [SerializeField, Min(0.1f)] private float groundProbeDistance = 6f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGridArea = true;
    [SerializeField] private bool drawBlockedNodes = true;
    [SerializeField, Min(0.01f)] private float nodeGizmoRadius = 0.15f;

    private void OnDrawGizmos()
    {
        if (drawGridArea)
            DrawGridArea();

        for (int x = 0; x < columns; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                Vector3 samplePosition = GetSamplePosition(x, z);

                if (!TryGetGroundPosition(samplePosition, out Vector3 nodePosition))
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawWireSphere(samplePosition, nodeGizmoRadius);
                    continue;
                }

                bool blocked = IsBlocked(nodePosition);

                if (blocked)
                {
                    if (!drawBlockedNodes)
                        continue;

                    Gizmos.color = Color.red;
                }
                else
                {
                    Gizmos.color = Color.green;
                }

                Gizmos.DrawSphere(nodePosition + Vector3.up * 0.05f, nodeGizmoRadius);
            }
        }
    }

    private Vector3 GetSamplePosition(int x, int z)
    {
        float gridWidth = (columns - 1) * nodeSpacing;
        float gridDepth = (rows - 1) * nodeSpacing;

        float localX = x * nodeSpacing - gridWidth * 0.5f;
        float localZ = z * nodeSpacing - gridDepth * 0.5f;

        return transform.position + transform.right * localX + transform.forward * localZ;
    }

    private bool TryGetGroundPosition(Vector3 samplePosition, out Vector3 groundPosition)
    {
        Vector3 rayOrigin = samplePosition + Vector3.up * groundProbeHeight;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundPosition = hit.point;
            return true;
        }

        groundPosition = samplePosition;
        return false;
    }

    private bool IsBlocked(Vector3 groundPosition)
    {
        float radius = Mathf.Min(agentRadius,agentHeight * 0.5f);

        Vector3 capsuleBottom = groundPosition + Vector3.up * radius;
        Vector3 capsuleTop = groundPosition + Vector3.up * (agentHeight - radius);

        return Physics.CheckCapsule(capsuleBottom, capsuleTop, radius, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    private void DrawGridArea()
    {
        float width = (columns - 1) * nodeSpacing;
        float depth = (rows - 1) * nodeSpacing;

        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, 0.05f, depth));

        Gizmos.matrix = previousMatrix;
    }
}
