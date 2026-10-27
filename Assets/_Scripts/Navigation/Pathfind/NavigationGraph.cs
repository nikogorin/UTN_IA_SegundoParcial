using System.Collections.Generic;
using UnityEngine;

public class NavigationGraph : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField, Min(1)] private int columns = 55;
    [SerializeField, Min(1)] private int rows = 110;
    [SerializeField, Min(0.1f)] private float nodeSpacing = 4f;

    [Header("Agent dimensions")]
    [SerializeField, Min(0.05f)] private float agentRadius = 0.4f;
    [SerializeField, Min(0.1f)] private float agentHeight = 1.8f;

    [Header("Surface detection")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField, Min(0.1f)] private float groundProbeHeight = 3f;
    [SerializeField, Min(0.1f)] private float groundProbeDistance = 6f;

    [Header("Graph Gizmos")]
    [SerializeField] private bool drawGraphGizmos = true;
    [SerializeField] private bool drawGridArea = true;
    [SerializeField] private bool drawNodes = true;
    [SerializeField] private bool drawConnections = true;
    [SerializeField] private bool drawBlockedNodes = true;
    [SerializeField] private bool drawNodesWithoutGround = false;
    [SerializeField, Min(0.01f)] private float nodeGizmoSize = 0.3f;
    [SerializeField] private Color connectionColor = Color.gray;

    private NavigationNode[,] _grid;
    private readonly List<NavigationNode> _walkableNodes = new();

    public int Columns => columns;
    public int Rows => rows;
    public float NodeSpacing => nodeSpacing;
    public IReadOnlyList<NavigationNode> WalkableNodes => _walkableNodes;
    public float GridWidth => (columns - 1) * nodeSpacing;
    public float GridDepth => (rows - 1) * nodeSpacing;

    private void Awake()
    {
        BuildGraph();
    }

    [ContextMenu("Rebuild Graph")]
    public void BuildGraph()
    {
        _grid = new NavigationNode[columns, rows];
        _walkableNodes.Clear();

        BuildNodes();
        BuildConnections();
    }

    private void BuildNodes()
    {
        for (int x = 0; x < columns; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                Coordinates coordinates = new(x, z);
                Vector3 worldPosition = GetWorldPosition(x, z);
                bool hasGround = TryGetGroundData(worldPosition, out Vector3 groundPosition, out ZoneType zoneType);
                Vector3 nodePosition = hasGround ? groundPosition : worldPosition;

                bool blocked = hasGround && IsPositionBlockedForAgent(nodePosition);

                NavigationNode node = new(coordinates, nodePosition, zoneType, hasGround, blocked);
                _grid[x, z] = node;

                if (node.IsWalkable)
                    _walkableNodes.Add(node);
            }
        }

        Debug.Log($"Graph generated: {columns * rows} cells, {_walkableNodes.Count} walkable nodes.");
    }

    private void BuildConnections()
    {
        int connectionCount = 0;

        foreach (NavigationNode node in _walkableNodes)
        {
            foreach (Coordinates neighborCoordinates in node.Coordinates.GetAllAdjacentDirections())
            {
                NavigationNode neighbor = GetNode(neighborCoordinates);

                if (neighbor == null)
                    continue;

                if (!neighbor.IsWalkable)
                    continue;

                if (!HasClearConnection(node, neighbor))
                    continue;

                int previousCount = node.Neighbors.Count;

                node.AddNeighbor(neighbor);

                if (node.Neighbors.Count > previousCount)
                    connectionCount++;
            }
        }

        Debug.Log($"Graph connections generated: {connectionCount / 2}.");
    }

    public NavigationNode GetNode(Coordinates coordinates)
    {
        if (!IsInsideGrid(coordinates))
            return null;

        return _grid[coordinates.X, coordinates.Y];
    }

    public NavigationNode GetClosestWalkableNode(Vector3 worldPosition)
    {
        NavigationNode closestNode = null;
        float closestSqrDistance = float.PositiveInfinity;

        foreach (NavigationNode node in _walkableNodes)
        {
            Vector3 difference = node.Position - worldPosition;

            difference.y = 0f;

            float sqrDistance = difference.sqrMagnitude;

            if (sqrDistance >= closestSqrDistance)
                continue;

            closestSqrDistance = sqrDistance;
            closestNode = node;
        }

        return closestNode;
    }

    public NavigationNode GetClosestNodeInZone(Vector3 worldPosition, ZoneType zone)
    {
        NavigationNode closestNode = null;
        float closestSqrDistance = float.PositiveInfinity;

        foreach (NavigationNode node in _walkableNodes)
        {
            if (node.Zone != zone)
                continue;

            Vector3 difference = node.Position - worldPosition;

            difference.y = 0f;

            float sqrDistance = difference.sqrMagnitude;

            if (sqrDistance >= closestSqrDistance)
                continue;

            closestSqrDistance = sqrDistance;
            closestNode = node;
        }

        return closestNode;
    }

    private bool IsInsideGrid(Coordinates coordinates)
    {
        return coordinates.X >= 0 && coordinates.X < columns && coordinates.Y >= 0 && coordinates.Y < rows;
    }

    private Vector3 GetWorldPosition(int x, int z)
    {
        float localX = x * nodeSpacing - GridWidth * 0.5f;
        float localZ = z * nodeSpacing - GridDepth * 0.5f;

        return transform.position + transform.right * localX + transform.forward * localZ;
    }

    private bool TryGetGroundData(Vector3 nodePosition, out Vector3 groundPosition, out ZoneType zoneType)
    {
        Vector3 rayOrigin = nodePosition + Vector3.up * groundProbeHeight;
        zoneType = ZoneType.Regular;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundPosition = hit.point;

            FloorZone zone = hit.collider.GetComponentInParent<FloorZone>();
            if (zone != null)
                zoneType = zone.ZoneType;

            return true;
        }

        groundPosition = nodePosition;
        return false;
    }

    private bool IsPositionBlockedForAgent(Vector3 groundPosition)
    {
        float radius = Mathf.Min(agentRadius, agentHeight * 0.5f);

        Vector3 capsuleBottom = groundPosition + Vector3.up * radius;
        Vector3 capsuleTop = groundPosition + Vector3.up * (agentHeight - radius);

        return Physics.CheckCapsule(capsuleBottom, capsuleTop, radius, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    private bool HasClearConnection(NavigationNode from, NavigationNode to)
    {
        Vector3 heightOffset = Vector3.up * (agentHeight * 0.5f);

        Vector3 start = from.Position + heightOffset;

        Vector3 end = to.Position + heightOffset;

        return !Physics.Linecast(start, end, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmos()
    {
        if (!drawGraphGizmos)
            return;

        if (drawGridArea)
            DrawGridArea();

        if (_grid == null)
            return;

        if (drawNodes)
            DrawNodes();

        if (drawConnections)
            DrawConnections();
    }

    private void DrawGridArea()
    {
        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(GridWidth, 0.05f, GridDepth));
        Gizmos.matrix = previousMatrix;
    }

    private void DrawNodes()
    {
        Vector3 size = new(nodeGizmoSize, nodeGizmoSize * 0.25f, nodeGizmoSize);

        foreach (NavigationNode node in _grid)
        {
            if (node == null)
                continue;

            Vector3 position = node.Position + Vector3.up * 0.05f;

            if (!node.HasGround)
            {
                if (!drawNodesWithoutGround)
                    continue;

                Gizmos.color = Color.gray;
                Gizmos.DrawWireCube(position, size);
                continue;
            }

            if (node.IsBlocked)
            {
                if (!drawBlockedNodes)
                    continue;

                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = GetZoneColor(node.Zone);
            }

            Gizmos.DrawCube(position, size);
        }
    }

    private void DrawConnections()
    {
        Gizmos.color = connectionColor;
        Vector3 offset = Vector3.up * 0.15f;

        foreach (NavigationNode node in _walkableNodes)
        {
            foreach (NavigationNode neighbor in node.Neighbors)
            {
                if (ShouldSkipDuplicateConnection(node, neighbor))
                    continue;

                Gizmos.DrawLine(node.Position + offset, neighbor.Position + offset);
            }
        }
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

    private bool ShouldSkipDuplicateConnection(NavigationNode node, NavigationNode neighbor)
    {
        Coordinates a = node.Coordinates;
        Coordinates b = neighbor.Coordinates;

        return a.X > b.X || (a.X == b.X && a.Y > b.Y);
    }
}
