using System.Collections.Generic;
using UnityEngine;

public enum HeuristicMethod { Distance, SqrMagnitude, Manhattan }

public class AStarPathfinder : MonoBehaviour
{
    [SerializeField] private NavigationGraph graph;
    [SerializeField] private HeuristicMethod heuristicMethod = HeuristicMethod.Manhattan;

    public List<NavigationNode> FindPath(Vector3 startPosition, Vector3 targetPosition)
    {
        NavigationNode startNode = graph.GetClosestWalkableNode(startPosition);
        NavigationNode targetNode = graph.GetClosestWalkableNode(targetPosition);

        return FindPath(startNode, targetNode);
    }
    
    public List<NavigationNode> FindPath(NavigationNode startNode, NavigationNode targetNode)
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
                return ReconstructPath(cameFrom, targetNode);

            foreach(NavigationNode neighbor in current.Neighbors)
            {
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
}
