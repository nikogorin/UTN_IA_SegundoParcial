using System.Collections.Generic;

public struct Coordinates
{
    public int X { get; }
    public int Y { get; }

    public readonly Coordinates Up => new(X, Y + 1);
    public readonly Coordinates Down => new(X, Y - 1);
    public readonly Coordinates Left => new(X - 1, Y);
    public readonly Coordinates Right => new(X + 1, Y);

    public List<Coordinates> GetAllAdyacentDirections() => new() { Up, Down, Left, Right };

    public readonly Coordinates UpRight => new(X + 1, Y + 1);
    public readonly Coordinates UpLeft => new(X - 1, Y + 1);
    public readonly Coordinates DownRight => new(X + 1, Y - 1);
    public readonly Coordinates DownLeft => new(X - 1, Y - 1);

    public List<Coordinates> GetAllDiagonalDirections() => new() { UpRight, UpLeft, DownRight, DownLeft };

    public Coordinates(int x, int y)
    {
        X = x;
        Y = y;
    }
    public override string ToString() => $"({X}, {Y})";
}
