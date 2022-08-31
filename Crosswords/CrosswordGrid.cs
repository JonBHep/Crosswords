using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crosswords;

public class CrosswordGrid
{
    /*
         Shape And Size
         
         A typical cryptic crossword grid is square, and 15 x 15 in size. Grids used by The Hindu, Economic Times and
         most daily publications from UK fit that description. There could be different-sized grids too - the New Indian
         Express crossword is a smaller 13 x 13, while the Times Jumbo as large as 23 x 23. Smaller grids tend to be 
         used for easier puzzles (I'm not sure why!).
         
         The standard cryptic grid is a blocked grid – that is, within the grid there is a pattern of black-and-white 
         squares. Each square is called a cell. Series of white squares into which answers are entered are called lights;
         the black squares are called darks, blacks, or blocks.
         
         All mainstream crossword grids have 180° rotational symmetry, also called two-way symmetry or half-turn symmetry.
         This means that when the grid is rotated 180° the black squares and white squares are in the same locations. 
         Some grids also have 90° symmetry, which is four-way symmetry or quarter-turn symmetry.
         
         The rotational symmetry doesn't affect the solving in any way, just adds to visual appeal (except if you have a
         Poirot-esque obsession with order then it will help by not distracting you from the solving). It does help the 
         setter in keeping other aspects of the grid consistent across the puzzle.
         
         */

    public const char BlackChar = '#';
    public const char WhiteChar = '~';
    private readonly char[,] _cells;
    private readonly int[,] _indices;
    private int _width;
    private int _height;
    private readonly Dictionary<string, Clue> _clus;

    public CrosswordGrid(string specifn)
    {
        _clus = new Dictionary<string, Clue>();
        _cells = new char[1, 1];
        _indices = new int[1, 1];
        Specification = specifn;
    }

    public string Specification
    {
        get
        {
            string q = _width.ToString();
            q = q.PadRight(2);
            for (int y = 0; y < _height; ++y)
            {
                for (int x = 0; x < _width; ++x)
                {
                    q += _cells[x, y];
                }
            }

            return q;
        }
        private init
        {
            // read width from first two characters
            string leader = value.Substring(0, 2);
            _width = int.Parse(leader);
            string spec = value.Substring(2);
            int sq = spec.Length;
            // deduce height
            _height = sq / _width;
            // read off cell values
            _cells = new char[_width, _height];
            _indices = new int[_width, _height];
            int p = 0;
            for (int y = 0; y < _height; ++y)
            {
                for (int x = 0; x < _width; ++x)
                {
                    _cells[x, y] = spec[p];
                    p++;
                }
            }

            LocateIndices();
        }
    }

    public int Width
    {
        get => _width;
        set => _width = value;
    }

    public int Height
    {
        get => _height;
        set => _height = value;
    }

    public char Cell(GridPoint p)
    {
        return _cells[p.X, p.Y];
    }

    public void SetCell(GridPoint point, char quelle)
    {
        _cells[point.X, point.Y] = quelle;
        LocateIndices();
    }

    private string? ClueSharingCell(GridPoint point, string clueKey, out char otherClueLetter)
    {
        string? retVal = null;
        otherClueLetter = '.'; // arbitrary default value
        foreach (var clef in _clus.Keys)
        {
            if (clef != clueKey) // don't return the clue that's making the enquiry
            {
                Clue indice = _clus[clef];
                char? c = indice.IncludesCell(point);
                if (c.HasValue)
                {
                    otherClueLetter = c.Value;
                    retVal = clef;
                    break;
                }
            }
        }

        return retVal;
    }

    public int Index(int x, int y)
    {
        return _indices[x, y];
    }

    private string RunAcrossFrom(int x, int y)
    {
        if (_cells[x, y] == BlackChar)
        {
            return string.Empty;
        } // cell is a black cell

        if (x >= (_width - 1))
        {
            return string.Empty;
        } // there is no cell to the right

        if (_cells[x + 1, y] == BlackChar)
        {
            return string.Empty;
        } // cell to the right is a black cell

        if (x > 0) // if this cell is not at the left edge...
        {
            if (_cells[x - 1, y] == WhiteChar) // and the cell to its left is white, then this is not the start of a run
            {
                return string.Empty;
            }
        }

        // string the white cells in this run
        string rs = string.Empty;
        while (_cells[x, y] == WhiteChar)
        {
            rs += _cells[x, y];
            x++;
            if (x == _width)
            {
                break;
            }
        }

        return rs;
    }

    private string RunDownFrom(int x, int y)
    {
        if (_cells[x, y] == BlackChar)
        {
            return string.Empty;
        } // cell is a black cell

        if (y >= (_height - 1))
        {
            return string.Empty;
        } // there is no cell below

        if (_cells[x, y + 1] == BlackChar)
        {
            return string.Empty;
        } // cell below is a black cell

        if (y > 0) // if this cell is not at the top edge...
        {
            if (_cells[x, y - 1] == WhiteChar) // and the cell above is white, then this is not the start of a run
            {
                return string.Empty;
            }
        }

        // string the white cells in this run
        string rs = string.Empty;
        while (_cells[x, y] == WhiteChar)
        {
            rs += _cells[x, y];
            y++;
            if (y == _height)
            {
                break;
            }
        }

        return rs;
    }

    public void LocateIndices()
    {
        int v = 0;
        _clus.Clear();
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                _indices[x, y] = 0;
                string runA = RunAcrossFrom(x, y);
                string runD = RunDownFrom(x, y);
                if (!string.IsNullOrEmpty(runA + runD))
                {
                    v++;
                    _indices[x, y] = v;
                }

                if (!string.IsNullOrEmpty(runA))
                {
                    string ky = $"A{v}";
                    Clue clu = new Clue(ky, runA.Length, x, y);
                    _clus.Add(ky, clu);
                }

                if (!string.IsNullOrEmpty(runD))
                {
                    string ky = $"D{v}";
                    Clue clu = new Clue(ky, runD.Length, x, y);
                    _clus.Add(ky, clu);
                }
            }
        }
    }

    public List<Clue> CluesAcross => ClueList('A');

    public List<Clue> CluesDown => ClueList('D');

    private List<Clue> ClueList(char d)
    {
        List<Clue> clus = new List<Clue>();
        foreach (Clue c in _clus.Values)
        {
            if (c.Direction == d)
            {
                clus.Add(c);
            }
        }

        return clus;
    }

    public List<string> ClueKeyList => _clus.Keys.ToList();

    public Clue ClueOf(string ky)
    {
        return _clus[ky];
    }

    public List<string> CrossingConflictsDetected(string clueKey, string proposedLetters)
    {
        // Detect conflicts between the specified clue letters and crossing clues - return keys of crossing clues in conflict
        List<string> violated = new List<string>();
        
        List<GridPoint> cellList = _clus[clueKey].IncludedCells();
        if (cellList.Count == proposedLetters.Length)
        {
            for (int z = 0; z < cellList.Count; z++)
            {
                char homechar = proposedLetters[z];
                string? autreClef = ClueSharingCell(cellList[z], clueKey, out char alienChar);
                if (autreClef is not null)
                {
                    if (_clus[autreClef].IsComplete())
                    {
                        if (homechar != alienChar)
                        {
                            violated.Add(autreClef);
                        }
                    }
                }
            }
        }

        return violated;
    }

    public string PatternedWordConstrained(string clueKey) // pattern of letters supplied by crossing words
    {
        var clu = _clus[clueKey];
        if (clu.IsComplete())
        {
            return clu.PatternedWordIntrinsic;
        }

        var aliens = new StringBuilder();
        var cellList = clu.IncludedCells();
        foreach (var t in cellList)
        {
            var autreClef = ClueSharingCell(t, clueKey, out var alienChar);
            if (autreClef is null)
            {
                aliens.Append(Clue.UnknownLetterChar);
            }
            else
            {
                aliens.Append(_clus[autreClef].IsComplete() ? alienChar : Clue.UnknownLetterChar);
            }
        }

        var chiffres = aliens.ToString();
        var letterPointer = 0;
        var elements = ClueContent.FormatList(clu.Content.Format);
        var builder = new StringBuilder();
        foreach (var element in elements)
        {
            switch (element[0])
            {
                case '-':
                {
                    builder.Append('-');
                    break;
                }
                case ',':
                {
                    builder.Append(' ');
                    break;
                }
            }

            var numeric = element[1..];
            if (int.TryParse(numeric, out var i))
            {
                for (var a = 0; a < i; a++)
                {
                    builder.Append(chiffres[letterPointer]);
                    letterPointer++;
                }
            }
        }
        return builder.ToString();
    }

    public string UnPatternedWordConstrained(string clueKey) // letters supplied by crossing words without spacing formatting
    {
        Clue clu = _clus[clueKey];
        if (clu.IsComplete())
        {
            return clu.Content.Letters;
        }

        StringBuilder aliens = new StringBuilder();
        List<GridPoint> cellList = clu.IncludedCells();
        foreach (var t in cellList)
        {
            string? autreClef = ClueSharingCell(t, clueKey, out char alienChar);
            if (autreClef is null)
            {
                aliens.Append(Clue.UnknownLetterChar);
            }
            else
            {
                aliens.Append(_clus[autreClef].IsComplete() ? alienChar : Clue.UnknownLetterChar);
            }
        }

        return aliens.ToString();
    }
    // public void AddClue(Clue cloo)
    // {
    //     _clus.Add(cloo.Key, cloo);
    // }
}