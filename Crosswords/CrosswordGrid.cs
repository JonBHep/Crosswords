using System.Collections.Generic;
using System.Linq;

namespace Crosswords;

public class CrosswordGrid
{
    /*
         Shape And Size
         A typical cryptic crossword grid is square, and 15 x 15 in size. Grids used by The Hindu, Economic Times and most daily publications from UK fit that description. There could be different-sized grids too - the New Indian Express crossword is a smaller 13 x 13, while the Times Jumbo as large as 23 x 23. Smaller grids tend to be used for easier puzzles (I'm not sure why!).
         The standard cryptic grid is a blocked grid – that is, within the grid there is a pattern of black-and-white squares. Each square is called a cell. Series of white squares into which answers are entered are called lights; the black squares are called darks, blacks, or blocks.
         All mainstream crossword grids have 180° rotational symmetry, also called two-way symmetry or half-turn symmetry. This means that when the grid is rotated 180° the black squares and white squares are in the same locations. Some grids also have 90° symmetry, which is four-way symmetry or quarter-turn symmetry.
         The rotational symmetry doesn't affect the solving in any way, just adds to visual appeal (except if you have a Poirot-esque obsession with order then it will help by not distracting you from the solving). It does help the setter in keeping other aspects of the grid consistent across the puzzle.

         */

        public const char BlackChar = '#';
        public const char WhiteChar = '.';
        private readonly char[,] _letters;
        private readonly int[,] _indices;
        private int _width;
        private int _height;
        private readonly Dictionary<string, Clue> _clus;
        private readonly Dictionary<string, string> _templates;
// Templates exist for those clues which are not just plain words e.g. multiple words or hyphenated words
        public CrosswordGrid(string specifn)
        {
            _clus = new Dictionary<string, Clue>();
            _templates = new Dictionary<string, string>();
            _letters = new char[1, 1];
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
                        q += _letters[x, y];
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
                _letters = new char[_width, _height];
                _indices = new int[_width, _height];
                int p = 0;
                for (int y = 0; y < _height; ++y)
                {
                    for (int x = 0; x < _width; ++x)
                    {
                        _letters[x, y] = spec[p];
                        p++;
                    }
                }
                LocateIndices();
            }
        }

        public int Width { get => _width; set => _width = value; }
        public int Height { get => _height; set => _height = value; }

        public char Letter(GridPoint p) { return _letters[p.X, p.Y]; }

        public void SetLetter(GridPoint point, char p)
        {
            _letters[point.X,point.Y] = p;
            LocateIndices();
        }


        public string IsSharedWith(GridPoint point, string clueKey)
        {
            string retVal = string.Empty;
            foreach (var clef in _clus.Keys)
            {
                if (clef != clueKey)
                {
                    Clue indice = _clus[clef];
                    if (indice.IncludesCell(point))
                    {
                        retVal = clef; break;
                    }
                }
            }

            return retVal;
        }
        public int Index(int x, int y) { return _indices[x, y]; }

        private string RunAcrossFrom(int x, int y)
        {
            if (_letters[x, y] == BlackChar) { return string.Empty; } // cell is a black cell
            if (x >= (_width - 1)) { return string.Empty; } // there is no cell to the right
            if (_letters[x + 1, y] == BlackChar) { return string.Empty; } // cell to the right is a black cell
            if (x > 0) // if this cell is not at the left edge...
            {
                if (_letters[x - 1, y] != BlackChar) // and the cell to its left is white, then this is not the start of a run
                {
                    return string.Empty;
                }
            }
            // string the white cells in this run
            string rs = string.Empty;
            while (_letters[x, y] != BlackChar)
            {
                rs += _letters[x, y];
                x++;
                if (x == _width) { break; }
            }
            return rs;
        }

        private string RunDownFrom(int x, int y)
        {
            if (_letters[x, y] == BlackChar) { return string.Empty; } // cell is a black cell
            if (y >= (_height - 1)) { return string.Empty; } // there is no cell below
            if (_letters[x, y + 1] == BlackChar) { return string.Empty; } // cell below is a black cell
            if (y > 0) // if this cell is not at the top edge...
            {
                if (_letters[x, y - 1] != BlackChar) // and the cell above is white, then this is not the start of a run
                {
                    return string.Empty;
                }
            }
            // string the white cells in this run
            string rs = string.Empty;
            while (_letters[x, y] != BlackChar)
            {
                rs += _letters[x, y];
                y++;
                if (y == _height) { break; }
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
                    if (!string.IsNullOrEmpty(runA + runD)) { v++; _indices[x, y] = v; }
                    if (!string.IsNullOrEmpty(runA))
                    {
                        string ky = $"A{v}";
                        Clue clu = new Clue(ky, runA, x, y);
                        _clus.Add(ky, clu);
                    }
                    if (!string.IsNullOrEmpty(runD))
                    {
                        string ky = $"D{v}";
                        Clue clu = new Clue(ky, runD, x, y);
                        _clus.Add(ky, clu);
                    }
                }
            }
        }

        public List<Clue> CluesAcross
        {
            get
            {
                return ClueList('A');
            }
        }

        public List<Clue> CluesDown
        {
            get
            {
                return ClueList('D');
            }
        }

        private List<Clue> ClueList(char d)
        {
            List<Clue> clus = new List<Clue>();
            foreach (Clue c in _clus.Values)
            {
                if (c.Direction == d) { clus.Add(c); }
            }
            return clus;
        }

        public List<string> ClueKeyList
        {
            get
            {
                return _clus.Keys.ToList();
            }
        }

        public Dictionary<string, string> Templates => _templates;

        public Clue ClueOf(string ky)
        {
            return _clus[ky];
        }

        public void AmendClue(string clueKey, string newWord)
        {
            if (_templates.ContainsKey(clueKey)) { _templates.Remove(clueKey); } // if there is a previous template for this clue, erase it
            string plate = string.Empty;
            bool nonstandard = false;
            Clue cloo = _clus[clueKey];
            int xs = cloo.Xstart;
            int ys = cloo.Ystart;
            if (cloo.Direction == 'A')
            {
                for (int a = 0; a < newWord.Length; a++)
                {
                    if (IsFormattingCharacter(newWord[a]))
                    {
                        plate += newWord[a];
                        nonstandard = true;
                    }
                    else
                    {
                        _letters[xs, ys] = newWord[a];
                        xs++;
                        plate += WhiteChar;
                    }
                }
            }
            else
            {
                for (int a = 0; a < newWord.Length; a++)
                {
                    if (IsFormattingCharacter(newWord[a]))
                    {
                        plate += newWord[a];
                        nonstandard = true;
                    }
                    else
                    {
                        _letters[xs, ys] = newWord[a];
                        ys++;
                        plate += WhiteChar;
                    }
                }
            }
            if (nonstandard) { _templates.Add(clueKey, plate); }
            LocateIndices();
        }
        
        public void ClearClue(string clueKey)
        {
            // clear the clue, preserving any letters which belong to crossing clues which have been filled in
            Clue cloo = _clus[clueKey];
            List<GridPoint> cellList = cloo.IncludedCells();
            for (int z = 0; z < cellList.Count; z++)
            {
                string indice = IsSharedWith(cellList[z], clueKey);
                if (indice.Length>0)
                {
                    Clue crossingClue = _clus[indice];
                    if (!crossingClue.IsComplete())
                    {
                        _letters[cellList[z].X, cellList[z].Y] = WhiteChar; // clear a letter shared with another clue if the other clue has not been completed    
                    }
                }
                else
                {
                    _letters[cellList[z].X, cellList[z].Y] = WhiteChar; // clear a letter not shared with another clue
                }
            }
            LocateIndices();
        }
        public static bool IsFormattingCharacter(char j)
        {
            if (j == ' ') { return true; }
            if (j == '-') { return true; }
            return false;
        }

        public static bool IsPermittedCharacter(char j)
        {
            if (j == WhiteChar) { return true; }
            if (IsFormattingCharacter(j)) { return true; }
            if ("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(j.ToString())) { return true; }
            return false;
        }

        public string PatternedWord(string clueKey)
        {
            string plainWord = _clus[clueKey].Word;
            if (_templates.ContainsKey(clueKey))
            {
                string template = _templates[clueKey];
                string word = string.Empty;
                int u = -1;
                for (int t = 0; t < template.Length; t++)
                {
                    if (IsFormattingCharacter(template[t]))
                    {
                        word += template[t];
                    }
                    else
                    {
                        u++;
                        word += plainWord[u];
                    }
                }
                return word;
            }
            return plainWord;
        }

        public string PatternedLength(string clueKey)
        {
            if (_templates.ContainsKey(clueKey))
            {
                string template = _templates[clueKey];
                string oput = "(";
                int u = 0;
                for (int t = 0; t < template.Length; t++)
                {
                    if (template[t] == ' ')
                    {
                        oput += $"{u}, ";
                        u = 0;
                    }
                    else if (template[t] == '-')
                    {
                        oput += $"{u}-";
                        u = 0;
                    }
                    else
                    {
                        u++;
                    }
                }
                oput += u.ToString() + ")";
                return oput;
            }
            else
            {
                return $"({_clus[clueKey].Word.Length})";
            }
        }

        public static string NakedWord(string formattedWord)
        {
            string oput = string.Empty;
            for (int a = 0; a < formattedWord.Length; a++)
            {
                if (!IsFormattingCharacter(formattedWord[a]))
                {
                    oput += formattedWord[a];
                }
            }
            return oput;
        }

        public static string NakedTemplate(string formattedWord)
        {
            string oput = string.Empty;
            for (int a = 0; a < formattedWord.Length; a++)
            {
                if (IsFormattingCharacter(formattedWord[a]))
                {
                    oput += formattedWord[a];
                }
                else
                {
                    oput += WhiteChar;
                }
            }
            return oput;
        }
}