using System.Collections.Generic;
using UnityEngine;

public enum HeuristicMethod { Distance, SqrMagnitude, Manhattan }

public class AStarPathfinder : MonoBehaviour
{
    [SerializeField] private NavigationGraph graph;
    [SerializeField] private HeuristicMethod heuristicMethod = HeuristicMethod.Manhattan;
    [SerializeField] private bool smoothPath = true;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField, Min(0f)] private float agentRadius = 0.4f;
    [SerializeField, Min(0f)] private float castHeight = 0.5f;

    public List<NavigationNode> FindPath(Vector3 startPosition, Vector3 targetPosition, bool avoidSafeZones)
    {
        NavigationNode startNode = graph.GetClosestWalkableNode(startPosition);
        NavigationNode targetNode = graph.GetClosestWalkableNode(targetPosition);

        return FindPath(startNode, targetNode, smoothPath, avoidSafeZones);
    }

    public List<NavigationNode> FindPathToClosestZone(Vector3 startPosition, ZoneType targetZone, bool avoidSafeZones)
    {
        NavigationNode startNode = graph.GetClosestWalkableNode(startPosition);

        if (startNode == null)
            return new List<NavigationNode>();

        List<NavigationNode> bestPath = null;
        float bestCost = float.PositiveInfinity;

        foreach (NavigationNode candidate in graph.WalkableNodes)
        {
            if (candidate.Zone != targetZone)
                continue;

            if (!IsZoneEntry(candidate, targetZone))
                continue;

            List<NavigationNode> candidatePath = FindPath(startNode, candidate, smooth: false, avoidSafeZones: avoidSafeZones); //turn off smooth only in this case to make a real comparison

            if (candidatePath == null || candidatePath.Count == 0)
                continue;

            float candidateCost = CalculatePathCost(candidatePath);

            if (candidateCost >= bestCost)
                continue;

            bestCost = candidateCost;
            bestPath = candidatePath;
        }

        return SmoothPath(bestPath) ?? new List<NavigationNode>();
    }

    private List<NavigationNode> FindPath(NavigationNode startNode, NavigationNode targetNode, bool smooth, bool avoidSafeZones)
    {
        if (startNode == null || targetNode == null)
        {
            Debug.LogError("AStar: Invalid node params");
            return new List<NavigationNode>();
        }

        PriorityQueue<NavigationNode> frontier = new();
        frontier.Enqueue(startNode, 0);
        Dictionary<NavigationNode, NavigationNode> cameFrom = new()
        {
            { startNode, null }
        };
        Dictionary<NavigationNode, float> costSoFar = new()
        {
            { startNode, 0 }
        };
        HashSet<NavigationNode> processed = new();

        NavigationNode current;

        while(frontier.Count > 0)
        {
            current = frontier.Dequeue();
            if (!processed.Add(current))
                continue;

            if (current == targetNode)
            {
                List<NavigationNode> path = ReconstructPath(cameFrom, targetNode);

                if (smooth)
                    path = SmoothPath(path);

                return path;
            }

            foreach (NavigationNode neighbor in current.Neighbors)
            {
                if (avoidSafeZones && neighbor.Zone == ZoneType.Safe)
                    continue;

                float newCost = costSoFar[current] + GetMovementCost(current, neighbor);
                bool wasVisited = costSoFar.TryGetValue(neighbor, out float previousCost);
                if (wasVisited && newCost >= previousCost)
                    continue;

                costSoFar[neighbor] = newCost;
                cameFrom[neighbor] = current;
                float priority = newCost + GetHeuristic(neighbor.Coordinates, targetNode.Coordinates, heuristicMethod);

                frontier.Enqueue(neighbor, priority);
                processed.Remove(neighbor);
            }
        }

        return new List<NavigationNode>();
    }

    private float GetMovementCost(NavigationNode from, NavigationNode to)
    {
        return Vector3.Distance(from.Position, to.Position);
    }

    private float GetHeuristic(Coordinates node, Coordinates target, HeuristicMethod method)
    {
        int deltaX = Mathf.Abs(node.X - target.X);
        int deltaY = Mathf.Abs(node.Y - target.Y);

        return method switch
        {
            HeuristicMethod.Distance => Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY) * graph.NodeSpacing,
            HeuristicMethod.SqrMagnitude => (deltaX * deltaX + deltaY * deltaY) * graph.NodeSpacing,
            HeuristicMethod.Manhattan => (deltaX + deltaY) * graph.NodeSpacing,
            _ => Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY) * graph.NodeSpacing
        };
    }

    private List<NavigationNode> SmoothPath(List<NavigationNode> path)
    {
        if (path == null || path.Count <= 3)
            return path;

        List<NavigationNode> smoothPath = new() { path[0] };
        int anchorIndex = 0;

        while (anchorIndex < path.Count - 1)
        {
            int nextIndex = anchorIndex + 1;

            for (int i = path.Count - 1; i > anchorIndex + 1; i--)
            {
                if (HasLineOfSight(path[anchorIndex].Position, path[i].Position))
                {
                    nextIndex = i;
                    break;
                }
            }

            smoothPath.Add(path[nextIndex]);
            anchorIndex = nextIndex;
        }

        return smoothPath;
    }

    private bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        from += Vector3.up * castHeight;
        to += Vector3.up * castHeight;

        Vector3 direction = to - from;
        float distance = direction.magnitude;

        if (distance < 0.0001f)
            return true;

        return !Physics.SphereCast(from, agentRadius, direction.normalized, out _, distance, obstacleMask);
    }

    private List<NavigationNode> ReconstructPath(Dictionary<NavigationNode, NavigationNode> cameFrom, NavigationNode targetNode)
    {
        List<NavigationNode> path = new();
        NavigationNode current = targetNode;

        while (current != null)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Reverse();
        return path;
    }

    private bool IsZoneEntry(NavigationNode node, ZoneType zone)
    {
        foreach (NavigationNode neighbor in node.Neighbors)
        {
            if (neighbor.Zone != zone)
                return true;
        }

        return false;
    }

    private float CalculatePathCost(IReadOnlyList<NavigationNode> path)
    {
        float cost = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            cost += GetMovementCost(path[i - 1], path[i]);
        }

        return cost;
    }
}
