using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Crosswords;

public partial class CreationWindow
{
    public CreationWindow()
    {
        InitializeComponent();
        _puzzle = new CrosswordGrid(BlankGridSpecification(8, 8)); // arbitrary size, not to be used
    }

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private readonly double _squareSide = 36;
    private CrosswordGrid _puzzle;
    private string _gamePath = string.Empty;

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var scrX = SystemParameters.PrimaryScreenWidth;
        var scrY = SystemParameters.PrimaryScreenHeight;
        var winX = scrX * .98;
        var winY = scrY * .94;
        var xm = (scrX - winX) / 2;
        var ym = (scrY - winY) / 4;
        Width = winX;
        Height = winY;
        Left = xm;
        Top = ym;
    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
        DimensionComboBox.Items.Clear();
        YDimensionComboBox.Items.Clear();
        YDimensionComboBox.Items.Add(new ComboBoxItem()
            {Tag = 0, Content = new TextBlock() {Text = "= X", FontFamily = new FontFamily("Lucida Console")}});
        for (int d = 4; d < 27; d++)
        {
            DimensionComboBox.Items.Add(new ComboBoxItem()
                {Tag = d, Content = new TextBlock() {Text = $"{d}", FontFamily = new FontFamily("Lucida Console")}});
            YDimensionComboBox.Items.Add(new ComboBoxItem()
                {Tag = d, Content = new TextBlock() {Text = $"{d}", FontFamily = new FontFamily("Lucida Console")}});
        }

        DimensionComboBox.SelectedIndex = 11;
        YDimensionComboBox.SelectedIndex = 0;
    }

    private void DisplayGrid()
    {
        // Constructing a rectangular Grid with rows and columns
        // Each cell contains a Canvas enclosed in a Border
        // Indices are inserted in the cell Canvas as a TextBlock

        Canvas[,] cellCanvas = new Canvas[_puzzle.Width, _puzzle.Height];
        const double gapSize = 2;
        Brush blackBrush = Brushes.DarkSlateGray;
        XwordGrid.Children.Clear();
        XwordGrid.ColumnDefinitions.Clear();
        XwordGrid.RowDefinitions.Clear();
        
        for (var x = 0; x < _puzzle.Width; x++)
        {
            ColumnDefinition col = new ColumnDefinition() {Width = new GridLength(_squareSide)};
            XwordGrid.ColumnDefinitions.Add(col);
            ColumnDefinition gap = new ColumnDefinition() {Width = new GridLength(gapSize)};
            XwordGrid.ColumnDefinitions.Add(gap);
        }

        XwordGrid.ColumnDefinitions.Add(new ColumnDefinition());

        for (var y = 0; y < _puzzle.Height; y++)
        {
            RowDefinition row = new RowDefinition() {Height = new GridLength(_squareSide)};
            XwordGrid.RowDefinitions.Add(row);
            RowDefinition gap = new RowDefinition() {Height = new GridLength(gapSize)};
            XwordGrid.RowDefinitions.Add(gap);
        }

        XwordGrid.RowDefinitions.Add(new RowDefinition());

        for (var x = 0; x < _puzzle.Width; x++)
        {
            for (var y = 0; y < _puzzle.Height; y++)
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
                        TextBlock indexBlock = new TextBlock() {FontSize = 12, Text = i.ToString(), Margin = new Thickness(2,0,0,0)};
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
        SaveDialogueWindow sdw = new SaveDialogueWindow() {Owner = this};
        bool? ans = sdw.ShowDialog();
        if (ans ?? false)
        {
            _gamePath = sdw.PuzzleFileSpecification();
            if (File.Exists(_gamePath))
            {
                MessageBoxResult result = MessageBox.Show("File already exists. Overwrite?", "Save puzzle grid"
                    , MessageBoxButton.OKCancel
                    , MessageBoxImage.Question);
                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            FileStream fs = new FileStream(_gamePath, FileMode.Create);  // create or overwrite
            using (var wri = new StreamWriter(fs, Clue.JbhEncoding))
            {
                wri.WriteLine(_puzzle.Specification);
                foreach (string tk in _puzzle.ClueKeyList)
                {
                    wri.WriteLine($"{tk}%{_puzzle.ClueOf(tk).Content.Specification()}");
                }
            }

            DialogResult = true;
        }

    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private static string BlankGridSpecification(int width, int height)
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

    private static string StarterGridSpecification(int width, int height)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(width.ToString().PadRight(2));
        for (int y = 0; y < height; y++)
        {
            char squareChar = CrosswordGrid.WhiteChar;
            for (int x = 0; x < width; x++)
            {
                builder.Append(squareChar);
                if (y % 2 == 1)
                {
                    squareChar = (squareChar == CrosswordGrid.WhiteChar)
                        ? CrosswordGrid.BlackChar
                        : CrosswordGrid.WhiteChar;
                }
            }
        }

        return builder.ToString();
    }

    public string NameOfTheGame => _gamePath;

    private void StartPatternButton_OnClick(object sender, RoutedEventArgs e)
    {
        string specification = StarterGridSpecification(_puzzle.Width, _puzzle.Height);
        _puzzle = new CrosswordGrid(specification);
        DisplayGrid();
    }

    private void ApplyDimensionsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DimensionComboBox.SelectedItem is ComboBoxItem {Tag: int x})
        {
            if (YDimensionComboBox.SelectedItem is ComboBoxItem {Tag: int y})
            {
                var dx = x;
                var dy = y == 0 ? x : y;
                string specification = BlankGridSpecification(dx, dy);
                _puzzle = new CrosswordGrid(specification);
                DisplayGrid();
                StartButton.IsEnabled = true;
                SaveButton.IsEnabled = true;
            }
        }
    }
}