using System.Collections.Generic;
using System.Text;

namespace Crosswords;

public class Clue
{
    private readonly char _direction;
    private readonly int _number;
    private readonly string _key;
    private readonly int _length;
    private readonly int _xstart;
    private readonly int _ystart;
    
    private ClueContent _content;
    public const char UnknownLetterChar = '.';
    
    public static readonly Encoding JbhEncoding = Encoding.UTF8;

    public Clue(string clef, int length, int xpos, int ypos)
    {
        _key = clef;
        _direction = clef[0];
        _number =int.Parse(clef[1..]);
        _length = length;
        _xstart = xpos;
        _ystart = ypos;
        _content = new ClueContent(length);
    }
    public char Direction { get => _direction; }
    public int Number { get => _number; }
    public int WordLength { get => _length; }
    public string Key { get => _key; }
    public int Xstart { get => _xstart; }
    public int Ystart { get => _ystart; }

    public void ClearLetters()
    {
        _content.Letters = CrosswordWordTemplate.Stringy(_length, UnknownLetterChar);
    }
    public ClueContent Content { get => _content; }

    public void AddContent(string spec)
    {
        _content=new ClueContent(spec);
    }
    
    public char? IncludesCell(GridPoint point)
    {
        if (_direction == 'A')
        {
            if (point.Y != _ystart) { return null; }
            int diff = point.X - _xstart;
            if ((diff < 0)||(diff>=_length)) { return null; }
            return _content.Letters[diff];
        }
        else
        {
            if (point.X != _xstart) { return null; }
            int diff = point.Y - _ystart;
            if ((diff < 0) || (diff >=_length)) { return null; }
            return _content.Letters[diff];
        }
    }

    public List<GridPoint> IncludedCells()
    {
        List<GridPoint> points = new();
        if (_direction == 'A')
        {
            int yCoord = _ystart;
            for (int offset = 0; offset < _length; offset++)
            {
                int xCoord = _xstart + offset;
                GridPoint pt = new GridPoint(xCoord, yCoord);
                points.Add(pt);
            }
        }
        else
        {
            int xCoord = _xstart;
            for (int offset = 0; offset < _length; offset++)
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
        if (_content.Letters.Length != _length)
        {
            return false;
        }
        return !_content.Letters.Contains(UnknownLetterChar);
    }

    public string PatternedWord
    {
        get
        {
            int letterPointer = 0;
            List<string> elements = ClueContent.FormatList(_content.Format);
            StringBuilder builder = new StringBuilder();
            foreach (string element in elements)
            {
                switch (element[0])
                {
                    case '-':
                    {
                        builder.Append('-'); break;
                    }
                    case ',':
                    {
                        builder.Append(' '); break;
                    }
                }

                string numeric = element[1..];
                if (int.TryParse(numeric, out int i))
                {
                    for (int a = 0; a < i; a++)
                    {
                        builder.Append(_content.Letters[letterPointer]);
                        letterPointer++;
                    }
                }
            }

            return builder.ToString();
        }
    }
    
}