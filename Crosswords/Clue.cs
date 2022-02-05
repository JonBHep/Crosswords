using System;
using System.Collections.Generic;
using System.Text;

namespace Crosswords;

public class Clue
{
    private readonly char _direction;
    private readonly int _number;
    private readonly string _content;
    private readonly string _key;
    private readonly int _xstart;
    private readonly int _ystart;
    public static readonly Encoding JbhEncoding = Encoding.UTF8;
    public Clue(string clef, string word, int xpos, int ypos)
    {
        _key = clef;
        _direction = clef[0];
        _number =int.Parse(clef.Substring(1));
        _content = word;
        _xstart = xpos;
        _ystart = ypos;
    }
    public char Direction { get => _direction; }
    public int Number { get => _number; }
    public string Word { get => _content; }
    public string Key { get => _key; }
    public int Xstart { get => _xstart; }
    public int Ystart { get => _ystart; }

    public bool IncludesCell(GridPoint point)
    {
        if (_direction == 'A')
        {
            if (point.Y != _ystart) { return false; }
            int diff = point.X - _xstart;
            if ((diff < 0)||(diff>=_content.Length)) { return false; }
            return true;
        }
        else
        {
            if (point.X != _xstart) { return false; }
            int diff = point.Y - _ystart;
            if ((diff < 0) || (diff >=_content.Length)) { return false; }
            return true;
        }
    }

    public List<GridPoint> IncludedCells()
    {
        List<GridPoint> points = new();
        if (_direction == 'A')
        {
            int yCoord = _ystart;
            for (int offset = 0; offset < _content.Length; offset++)
            {
                int xCoord = _xstart + offset;
                GridPoint pt = new GridPoint(xCoord, yCoord);
                points.Add(pt);
            }
        }
        else
        {
            int xCoord = _xstart;
            for (int offset = 0; offset < _content.Length; offset++)
            {
                int yCoord = _ystart + offset;
                GridPoint pt = new GridPoint(xCoord, yCoord);
                points.Add(pt);
            }
        }

        return points;
    }

    public bool IsComplete()
    {
        return !Word.Contains(CrosswordGrid.WhiteChar);
    }
    
}