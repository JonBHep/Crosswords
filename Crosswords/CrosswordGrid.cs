using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

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
        public const char UnknownLetterChar = '.';
        private readonly char[,] _cells;
        private readonly int[,] _indices;
        private int _width;
        private int _height;
        private readonly Dictionary<string, Clue> _clus;
        public CrosswordGrid(string specifn)
        {
            _clus = new Dictionary<string, Clue>();
            _cells = new char[1,1];
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

        public int Width { get => _width; set => _width = value; }
        public int Height { get => _height; set => _height = value; }

        public char Cell(GridPoint p) { return _cells[p.X, p.Y]; }
        
        public void SetCell(GridPoint point, char quelle)
        {
            _cells[point.X,point.Y] =quelle;
            LocateIndices();
        }

        public string? ClueSharingCell(GridPoint point, string clueKey, out char? otherClueLetter)
        {
            string? retVal = null;
            otherClueLetter = null;
            foreach (var clef in _clus.Keys)
            {
                if (clef != clueKey) // don't return the clue that's making the enquiry
                {
                    Clue indice = _clus[clef];
                    otherClueLetter = indice.IncludesCell(point);
                    if (otherClueLetter is not null)
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
            if (_cells[x, y] == BlackChar) { return string.Empty; } // cell is a black cell
            if (x >= (_width - 1)) { return string.Empty; } // there is no cell to the right
            if (_cells[x + 1, y] == BlackChar) { return string.Empty; } // cell to the right is a black cell
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
                if (x == _width) { break; }
            }
            return rs;
        }

        private string RunDownFrom(int x, int y)
        {
            if (_cells[x, y] == BlackChar) { return string.Empty; } // cell is a black cell
            if (y >= (_height - 1)) { return string.Empty; } // there is no cell below
            if (_cells[x, y + 1] == BlackChar) { return string.Empty; } // cell below is a black cell
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
                if (c.Direction == d) { clus.Add(c); }
            }
            return clus;
        }

        public List<string> ClueKeyList => _clus.Keys.ToList();

        public Clue ClueOf(string ky)
        {
            return _clus[ky];
        }
// TODO Continue working with refreshing the grid and the contents separately, feeding the contents from the clue letters
// not from the grid 'letrres' which hopefully can be disposed of - no sense in keeping two copies of the same data

        
        public void AmendClueFormat(string clueKey, string newFormat)
        {
            _clus[clueKey].Content.Format = newFormat;
        }
        
        public void ClearClue(string clueKey)
        {
            // clear the clue, preserving any letters which belong to crossing clues which have been filled in
            Clue cloo = _clus[clueKey];
            List<GridPoint> cellList = cloo.IncludedCells();
            cloo.Content.Letters = string.Empty;
            StringBuilder builder = new StringBuilder();
            for (int z = 0; z < cellList.Count; z++)
            {
                string? indice = ClueSharingCell(cellList[z], clueKey, out char? caractere);
                if (indice is not null)
                {
                    Clue crossingClue = _clus[indice];
                    if (crossingClue.IsComplete())
                    {
                        builder.Append(cloo.Content.Letters[z]);  // retain a letter shared with another completed clue
                    }
                    else
                    {
                        builder.Append(UnknownLetterChar);  // clear a letter shared with another clue if the other clue has not been completed
                    }
                }
                else
                {
                    builder.Append(UnknownLetterChar);  // clear a letter not shared with another clue
                }
            }
        }

        public bool SuccessfullyApplyCrossings()
        {
            // Modify clue letters where they cross other clues - return false if conflict is found
            bool faultless = true;
            foreach (string key in _clus.Keys)
            {
                List<GridPoint> cellList = _clus[key].IncludedCells();
                string lettres = _clus[key].Content.Letters;
                for (int z=0; z<cellList.Count; z++)
                {
                    char homechar = lettres[z];
                    if (ClueSharingCell(cellList[z], key, out char? alienChar) is not null)
                    {
                        if (alienChar is { } incomer)
                        {
                            if (incomer != UnknownLetterChar)
                            {
                                if (homechar == UnknownLetterChar)
                                { 
                                    lettres = AlteredCharacter(lettres, z, incomer);    
                                }
                                else if (homechar != alienChar)
                                {
                                    faultless =false;
                                }                                
                            }
                        }
                    }
                }

                if (lettres != _clus[key].Content.Letters)
                {
                    _clus[key].Content.Letters = lettres;
                }
            }

            return faultless;
        }

        private string AlteredCharacter(string source, int position, char replacement)
        {
            StringBuilder builder = new StringBuilder();
            for (int p = 0; p < source.Length; p++)
            {
                if (p == position)
                {
                    builder.Append(replacement);
                }
                else
                {
                    builder.Append(source[p]);    
                }
            }

            return builder.ToString();
        }
        
        public static bool IsFormattingCharacter(char j)
        {
            if (j == ' ') { return true; }
            if (j == '-') { return true; }
            return false;
        }

        public static bool IsLetterOrWhiteCell(char j)
        {
            if (j == WhiteChar) { return true; }
            if ("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(j.ToString())) { return true; }
            return false;
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

        // public static string NakedTemplate(string formattedWord)
        // {
        //     string oput = string.Empty;
        //     for (int a = 0; a < formattedWord.Length; a++)
        //     {
        //         if (IsFormattingCharacter(formattedWord[a]))
        //         {
        //             oput += formattedWord[a];
        //         }
        //         else
        //         {
        //             oput += WhiteChar;
        //         }
        //     }
        //     return oput;
        // }

        public void AddClue(Clue cloo)
        {
            _clus.Add(cloo.Key, cloo);
        }
}