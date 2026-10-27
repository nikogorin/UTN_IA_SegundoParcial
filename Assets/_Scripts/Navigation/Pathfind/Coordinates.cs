using System.Collections.Generic;

public readonly struct Coordinates
{
    public int X { get; }
    public int Y { get; }

    public readonly Coordinates Up => new(X, Y + 1);
    public readonly Coordinates Down => new(X, Y - 1);
    public readonly Coordinates Left => new(X - 1, Y);
    public readonly Coordinates Right => new(X + 1, Y);

    public Coordinates(int x, int y)
    {
        X = x;
        Y = y;
    }

    public List<Coordinates> GetAllAdjacentDirections() => new() { Up, Down, Left, Right };

    public override string ToString() => $"({X}, {Y})";
}
