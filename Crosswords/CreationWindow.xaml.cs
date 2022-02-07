﻿using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace Crosswords;

public partial class CreationWindow
{
    public CreationWindow()
    {
        InitializeComponent();
        _puzzle = new CrosswordGrid(BlankGridSpecification(8,8));  // arbitrary size, not to be used
    }

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private readonly double _squareSide = 36;
    private CrosswordGrid _puzzle;
    private string _gamePath = String.Empty;
    // private string _xwordTitle = string.Empty;

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
        BoxX.Text = BoxY.Text = "15";
        // DisplayGrid();
    }

    private void DisplayGrid()
    {
        // Constructing a rectangular Grid with rows and columns
        // Each cell contains a Canvas enclosed in a Border
        // Indices are inserted in the cell Canvas as a TextBlock
        // Bars and hyphens are added directly to the Grid cells not to the Canvases - they are sourced from Clue.PatternedWord

        Canvas[,] cellCanvas = new Canvas[_puzzle.Width, _puzzle.Height];
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
                cellCanvas[x, y] = new Canvas()
                {
                    Tag = Coords(x, y)
                };

                cellCanvas[x, y].MouseDown += Cell_MouseDown;

                if (_puzzle.Cell(new GridPoint(x, y)) == CrosswordGrid.BlackChar)
                {
                    cellCanvas[x, y].Background = blackBrush;
                }
                else
                {
                    cellCanvas[x, y].Background = Brushes.White;
                    int i = _puzzle.Index(x, y);
                    if (i > 0)
                    {
                        TextBlock indexBlock = new TextBlock() {FontSize = 8, Text = i.ToString()};
                        cellCanvas[x, y].Children.Add(indexBlock);
                    }

                }

                Border b = new Border()
                {
                    BorderBrush = Brushes.DarkSlateGray, BorderThickness = new Thickness(1)
                    , Child = cellCanvas[x, y]
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

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _puzzle.LocateIndices();
        SaveFileDialog dlg = new SaveFileDialog()
        {
            AddExtension = true, DefaultExt = "cwd", Filter = "Crossword files (*.cwd)|*.cwd"
            , InitialDirectory = CrosswordsPath, OverwritePrompt = true, Title = "Save crossword as..."
            , ValidateNames = true
        };
        bool? ans = dlg.ShowDialog();
        if ((ans.HasValue) && (ans.Value))
        {
            _gamePath = dlg.FileName;
            FileStream fs = new FileStream(dlg.FileName, FileMode.Create);
            using (StreamWriter wri = new StreamWriter(fs, Clue.JbhEncoding))
            {
                wri.WriteLine(_puzzle.Specification);
                foreach (string tk in _puzzle.ClueKeyList)
                {
                    wri.WriteLine($"{tk}%{_puzzle.ClueOf(tk).Content.Specification()}" );
                }
            }
            // _xwordTitle = Path.GetFileNameWithoutExtension(dlg.FileName);
            DialogResult = true;
        }
    }
    
    private static string CrosswordsPath => Path.Combine(Jbh.AppManager.DataPath, "Crosswords");

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

        string specification = BlankGridSpecification(dx, dy);
        _puzzle = new CrosswordGrid(specification);
        DisplayGrid();
        SaveButton.IsEnabled = true;
    }

    private string BlankGridSpecification(int width, int height)
    {
        int sq = width * height;
        StringBuilder builder = new StringBuilder();
        builder.Append(width.ToString().PadRight(2));
        for (int z = 0; z < sq; z++)
        {
            builder.Append(CrosswordGrid.WhiteChar);
        }
        return builder.ToString();
    }
    public string NameOfTheGame => _gamePath; 
    // public string GridTitle => _xwordTitle;
}