using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace Crosswords;

public partial class CreationWindow
{
    public CreationWindow()

    {
        InitializeComponent();
        string defaultSpecification
            = "15......#........#.#.#.#.#.#.#.#........#......#.#.#.#.#.#.#.####............#.#.#.#.#.###.#....###........#.#.#.#.#.#.#.#........###....#.###.#.#.#.#.#............####.#.#.#.#.#.#.#......#........#.#.#.#.#.#.#.#........#......";
        _puzzle = new CrosswordGrid(defaultSpecification);
    }

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private readonly double _squareSide = 36;
    private CrosswordGrid _puzzle;
    private string _gridDefinition = String.Empty;
    private string _xwordTitle = string.Empty;

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        double scrX = SystemParameters.PrimaryScreenWidth;
        double scrY = SystemParameters.PrimaryScreenHeight;
        double winX = scrX * .98;
        double winY = scrY * .94;
        double xm = (scrX - winX) / 2;
        double ym = (scrY - winY) / 4;
        Width = winX;
        Height = winY;
        Left = xm;
        Top = ym;
    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
        DisplayGrid();
    }

    private void DisplayGrid()
    {
        // Constructing a rectangular Grid with rows and columns
        // Each cell contains a Canvas enclosed in a Border
        // Indices are inserted in the cell Canvas as a TextBlock
        // Bars and hyphens are added directly to the Grid cells not to the Canvases - they are sourced from Clue.PatternedWord

        Canvas[,] CellCanvas = new Canvas[_puzzle.Width, _puzzle.Height];
        double gapSize = 2;
        Brush blackBrush = Brushes.Sienna;
        XwordGrid.Children.Clear();
        XwordGrid.ColumnDefinitions.Clear();
        XwordGrid.RowDefinitions.Clear();
        for (int x = 0; x < _puzzle.Width; x++)
        {
            ColumnDefinition col = new ColumnDefinition() {Width = new GridLength(_squareSide)};
            XwordGrid.ColumnDefinitions.Add(col);
            ColumnDefinition gap = new ColumnDefinition() {Width = new GridLength(gapSize)};
            XwordGrid.ColumnDefinitions.Add(gap);
        }

        ColumnDefinition lastcol = new ColumnDefinition();
        XwordGrid.ColumnDefinitions.Add(lastcol);

        for (int y = 0; y < _puzzle.Height; y++)
        {
            RowDefinition row = new RowDefinition() {Height = new GridLength(_squareSide)};
            XwordGrid.RowDefinitions.Add(row);
            RowDefinition gap = new RowDefinition() {Height = new GridLength(gapSize)};
            XwordGrid.RowDefinitions.Add(gap);
        }

        RowDefinition lastrow = new RowDefinition();
        XwordGrid.RowDefinitions.Add(lastrow);

        for (int x = 0; x < _puzzle.Width; x++)
        {
            for (int y = 0; y < _puzzle.Height; y++)
            {
                CellCanvas[x, y] = new Canvas()
                {
                    Tag = Coords(x, y)
                };

                CellCanvas[x, y].MouseDown += Cell_MouseDown;

                if (_puzzle.Cell(new GridPoint(x, y)) == CrosswordGrid.BlackChar)
                {
                    CellCanvas[x, y].Background = blackBrush;
                }
                else
                {
                    CellCanvas[x, y].Background = Brushes.White;
                    int i = _puzzle.Index(x, y);
                    if (i > 0)
                    {
                        TextBlock indexBlock = new TextBlock() {FontSize = 8, Text = i.ToString()};
                        CellCanvas[x, y].Children.Add(indexBlock);
                    }

                }

                Border b = new Border()
                {
                    BorderBrush = Brushes.DarkSlateGray, BorderThickness = new Thickness(1)
                    , Child = CellCanvas[x, y]
                };
                Grid.SetColumn(b, x * 2);
                Grid.SetRow(b, y * 2);
                XwordGrid.Children.Add(b);
            }
        }
    }

    private void Cell_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Canvas {Tag: string q})
        {
            // get coordinates
            GridPoint locus = CoordPoint(q);

            // build grid structure

            _puzzle.SetCell(locus
                , _puzzle.Cell(locus) == CrosswordGrid.BlackChar
                    ? CrosswordGrid.WhiteChar
                    : CrosswordGrid.BlackChar);

            if (Symmetrical)
            {
                int symmX = _puzzle.Width - (locus.X + 1);
                int symmY = _puzzle.Height - (locus.Y + 1);
                GridPoint symmLocus = new GridPoint(symmX, symmY);
                _puzzle.SetCell(symmLocus, _puzzle.Cell(locus));
            }

            DisplayGrid();

        }
    }

    private bool Symmetrical => (SymmCheckBox.IsChecked.HasValue) && (SymmCheckBox.IsChecked.Value);

    // private void NewButton_Click(object sender, RoutedEventArgs e)
    // {
    //     CrosswordSizeDialogue dw = new CrosswordSizeDialogue() {Owner = this};
    //
    //     bool? q = dw.ShowDialog();
    //     if (!q.HasValue)
    //     {
    //         return;
    //     }
    //
    //     if (!q.Value)
    //     {
    //         return;
    //     }
    //
    //     int sq = dw.X_Dimension * dw.Y_Dimension;
    //     string init = dw.X_Dimension.ToString();
    //     init = init.PadRight(2);
    //     for (int z = 0; z < sq; z++)
    //     {
    //         init += CrosswordGrid.WhiteChar;
    //     }
    //
    //     _puzzle = new CrosswordGrid(init);
    //     DisplayGrid();
    //     _xwordTitle = "default";
    //     
    //     SaveCrossword();
    // }

    private string Coords(int x, int y)
    {
        string rv = Alphabet.Substring(x, 1);
        rv += Alphabet.Substring(y, 1);
        return rv;
    }

    private GridPoint CoordPoint(string seed)
    {
        int x = Alphabet.IndexOf(seed[0]);
        int y = Alphabet.IndexOf(seed[1]);
        return new GridPoint(x, y);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        SaveCrosswordAndClose();
    }

    private void SaveCrosswordAndClose()
    {
        SaveFileDialog dlg = new SaveFileDialog()
        {
            AddExtension = true, DefaultExt = "cwd", Filter = "Crossword files (*.cwd)|*.cwd"
            , InitialDirectory = CrosswordsPath, OverwritePrompt = true, Title = "Save crossword as..."
            , ValidateNames = true
        };
        bool? ans = dlg.ShowDialog();
        if ((ans.HasValue) && (ans.Value))
        {
            FileStream fs = new FileStream(dlg.FileName, FileMode.Create);
            using (StreamWriter wri = new StreamWriter(fs, Clue.JbhEncoding))
            {
                wri.WriteLine(_puzzle.Specification);
            }

            _xwordTitle = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
            DialogResult = true;
        }
    }

    private void SaveAsButton_Click(object sender, RoutedEventArgs e)
    {
        SaveCrosswordAndClose();
    }


    private static string CrosswordsPath => System.IO.Path.Combine(Jbh.AppManager.DataPath, "Crosswords");
    private string WordListFile => System.IO.Path.Combine(Jbh.AppManager.DataPath, "CrosswordLists", "wordlist.txt");

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void BoxXY_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyButton.IsEnabled = false;
        if (int.TryParse(BoxX.Text, out int x))
        {
            if (int.TryParse(BoxY.Text, out int y))
            {
                if (x is > 3 and < 27)
                {
                    if (y is > 3 and < 27)
                    {
                        ApplyButton.IsEnabled = true;
                    }
                }
            }
        }
    }

    private void ApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        bool ok = false;
        int dx = 0;
        int dy = 0;
        ApplyButton.IsEnabled = false;
        if (int.TryParse(BoxX.Text, out int x))
        {
            if (int.TryParse(BoxY.Text, out int y))
            {
                if (x is > 3 and < 27)
                {
                    if (y is > 3 and < 27)
                    {
                        dx = x;
                        dy = y;
                        ok = true;
                    }
                }
            }
        }

        if (!ok)
        {
            return;
        }

        int sq = dx * dy;
        StringBuilder builder = new StringBuilder();
        builder.Append(dx.ToString().PadRight(2));
        for (int z = 0; z < sq; z++)
        {
            builder.Append(CrosswordGrid.WhiteChar);
        }

        _gridDefinition = builder.ToString();
        _puzzle = new CrosswordGrid(builder.ToString());
        DisplayGrid();
    }

    public string GridDefinition
    {
        get => _gridDefinition;
    }

    public string GridTitle
    {
        get => _xwordTitle;
    }
}