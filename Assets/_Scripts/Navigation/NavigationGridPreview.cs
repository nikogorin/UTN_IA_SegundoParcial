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
    //[SerializeField, Min(0.01f)] private float nodeGizmoRadius = 0.15f;
    [SerializeField, Range(0.1f, 1f)] private float nodeGizmoScale = 0.75f;
    [SerializeField, Min(0.01f)] private float nodeGizmoHeight = 0.1f;

    private void OnDrawGizmos()
    {
        if (drawGridArea)
            DrawGridArea();

        for (int x = 0; x < columns; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                Vector3 samplePosition = GetSamplePosition(x, z);

                bool hasGround = TryGetGroundData(samplePosition, out Vector3 nodePosition, out ZoneType zoneType);

                Vector3 obstacleCheckPosition = hasGround ? nodePosition : samplePosition;

                bool blocked = IsBlocked(obstacleCheckPosition);

                if (blocked)
                {
                    if (!drawBlockedNodes)
                        continue;

                    //Gizmos.color = Color.red;
                    //Gizmos.DrawSphere(obstacleCheckPosition + Vector3.up * 0.05f, nodeGizmoRadius);
                    DrawNodeGizmo(obstacleCheckPosition, Color.red, true);

                    continue;
                }

                if (!hasGround)
                {
                    //Gizmos.color = Color.gray;
                    //Gizmos.DrawWireSphere(samplePosition, nodeGizmoRadius);
                    DrawNodeGizmo(samplePosition, Color.black, false);
                    continue;
                }

                //Gizmos.color = GetZoneColor(zoneType);
                //Gizmos.DrawSphere(nodePosition + Vector3.up * 0.05f, nodeGizmoRadius);
                DrawNodeGizmo(nodePosition, GetZoneColor(zoneType), true);
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

    private bool TryGetGroundData(Vector3 samplePosition, out Vector3 groundPosition, out ZoneType zoneType)
    {
        Vector3 rayOrigin = samplePosition + Vector3.up * groundProbeHeight;
        zoneType = ZoneType.Regular;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundPosition = hit.point;

            FloorZone zone = hit.collider.GetComponentInParent<FloorZone>();
            if (zone != null)
                zoneType = zone.ZoneType;

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

    private Vector3 GetNodeGizmoSize()
    {
        float horizontalSize = nodeSpacing * nodeGizmoScale;

        return new Vector3(horizontalSize, nodeGizmoHeight, horizontalSize);
    }

    private void DrawNodeGizmo(Vector3 position, Color color, bool solid)
    {
        Vector3 size = GetNodeGizmoSize();
        Vector3 center = position + Vector3.up * (size.y * 0.5f + 0.02f);

        Gizmos.color = color;

        if (solid)
            Gizmos.DrawCube(center, size);
        else
            Gizmos.DrawWireCube(center, size);
    }

    private Color GetZoneColor(ZoneType zoneType)
    {
        return zoneType switch
        {
            ZoneType.Initial => Color.blue,
            ZoneType.HuntingGround => Color.green,
            ZoneType.Safe => Color.cyan,
            ZoneType.Exit => Color.yellow,
            _ => Color.white
        };
    }
}
