using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Graph : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float nodeOffset = 1f;
    [SerializeField] private Node nodePrefab;
    [SerializeField] private bool allowDiagonals = false;

    private Node[,] _grid;

    public List<Node> GetAllNodes => _grid.Cast<Node>().ToList();

    void Start()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        _grid = new Node[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Node node = Instantiate(nodePrefab, transform);
                _grid[x, y] = node;
                node.transform.position = new Vector3(x * nodeOffset, 0f, y * nodeOffset);
                node.name = $"Node ({x},{y})";
                node.SetupCoordinates(new Coordinates(x, y));
            }
        }
    }

    public List<Node> GetNeighbors(Node current)
    {
        var neighbors = new List<Node>();

        foreach(var coordinate in current.Coordinates.GetAllAdyacentDirections())
        {
            if (!IsInsideGridBoundaries(coordinate))
                continue;

            neighbors.Add(_grid[coordinate.X, coordinate.Y]);
        }

        if(!allowDiagonals)
            return neighbors;

        foreach (var coordinate in current.Coordinates.GetAllDiagonalDirections())
        {
            if (!IsInsideGridBoundaries(coordinate))
                continue;

            neighbors.Add(_grid[coordinate.X, coordinate.Y]);
        }

        return neighbors;
    }

    public void ResetColors()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                _grid[x, y].ResetColor();
            }
        }
    }

    private bool IsInsideGridBoundaries(Coordinates coordinate)
    {
        return coordinate.X >= 0 && coordinate.X < gridWidth && coordinate.Y >= 0 && coordinate.Y < gridHeight;
    }

    
}
