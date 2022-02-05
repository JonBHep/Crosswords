namespace Crosswords;

public readonly struct GridPoint
{
    private readonly int _x;
    private readonly int _y;
    public int X => _x;
    public int Y => _y;

    public GridPoint(int x, int y)
    {
        _x = x;
        _y = y;
    }
}