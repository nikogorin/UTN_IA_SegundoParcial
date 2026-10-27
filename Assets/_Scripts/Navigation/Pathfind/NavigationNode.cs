using System.Collections.Generic;
using UnityEngine;

public class NavigationNode 
{
    public Coordinates Coordinates { get; private set; }
    public Vector3 Position { get; private set; }
    public ZoneType Zone { get; private set; }

    public bool HasGround { get; private set; }
    public bool IsBlocked { get; private set; }
    public bool IsWalkable => HasGround && !IsBlocked;

    public List<NavigationNode> Neighbors { get; } = new();

    public string Name => $"Node {Coordinates}";

    public NavigationNode(Coordinates coordinates, Vector3 position, ZoneType zone, bool hasGround, bool isBlocked)
    {
        Coordinates = coordinates;
        Position = position;
        Zone = zone;
        HasGround = hasGround;
        IsBlocked = isBlocked;
    }

    public void AddNeighbor(NavigationNode neighbor)
    {
        if (neighbor == null || neighbor == this)
            return;

        if (!Neighbors.Contains(neighbor))
            Neighbors.Add(neighbor);
    }

    public override string ToString()
    {
        return Name;
    }
}
