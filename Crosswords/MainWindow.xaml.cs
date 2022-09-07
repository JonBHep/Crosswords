using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace Crosswords;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    // https://www.crosswordunclued.com/2009/09/crossword-grid-symmetry.html
       
    public MainWindow()
    {
        InitializeComponent();
        _puzzle = new CrosswordGrid(DefaultSpecification);
        _strangers = new List<string>();
        _familiars = new Connu();
    }
    
    private const string DefaultSpecification
        = "15......#........#.#.#.#.#.#.#.#........#......#.#.#.#.#.#.#.####............#.#.#.#.#.###.#....###........#.#.#.#.#.#.#.#........###....#.###.#.#.#.#.#............####.#.#.#.#.#.#.#......#........#.#.#.#.#.#.#.#........#......";
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const double SquareSide = 36;
    private const double LetterWidth = 30;
    private readonly FontFamily _fixedFont = new("Liberation Mono");
    private CrosswordGrid _puzzle;
    private string _xWordTitle = "default";
        
    private readonly Brush _barBrush = Brushes.OrangeRed;
    private readonly Brush _hyphenBrush = Brushes.Crimson;
    private readonly Brush _letterBrush = Brushes.DarkRed;
    private readonly Brush _blackSquareBrush = Brushes.DimGray;
    private readonly Brush _highlightSquareBrush = Brushes.Moccasin;
        
    private string _selectedClueKey = string.Empty;
    private bool _disableCheckBoxesTrigger;
    private Canvas[,] _cellPaper = new Canvas[0, 0];
    private readonly List<string> _strangers;
    private readonly Connu _familiars;
    
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
        PuzzleHeaderDockPanel.Visibility = Visibility.Hidden;
        SwitchClueControls(false);
    }

    private void SwitchClueControls(bool on)
    {
        Visibility vis = on ? Visibility.Visible : Visibility.Hidden;
        SelectedClueGrid.Visibility = vis;
        SecondClueTitleTextBlock.Visibility = vis;
        if (on)
        {
            LettersEntryTextBox.Focus();
        }
        else
        {
            _selectedClueKey = string.Empty;
        }
    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
        DisplayGrid();
        NameTextBlock.Text = "Choose Open or New";
        SwitchClueControls(false);
        ListEachButton.IsEnabled = false;
        ListWholeButton.IsEnabled = false;
            
        PuzzleHeaderDockPanel.Visibility = Visibility.Visible;
        var path = MostRecentlySavedGamePath();
        if (path is { })
        {
            LoadPuzzleFromFile(path);    
        }
        else
        {
            DisplayGrid(); // using default specification
        }
    }

    private void DisplayGrid()
    {
        // Constructing a rectangular Grid with rows and columns
        // Each cell contains a Canvas enclosed in a Border
        // Indices are inserted in the cell Canvas as a TextBlock
        // Bars and hyphens are added directly to the Grid cells not to the Canvases - they are sourced from Clue.PatternedWord

        _cellPaper = new Canvas[_puzzle.Width, _puzzle.Height];

        const double gapSize = 2;
        var ff = new FontFamily("Times New Roman");

        CrosswordGrid.Children.Clear(); // clear Grid
        CrosswordGrid.ColumnDefinitions.Clear();
        CrosswordGrid.RowDefinitions.Clear();

        for (var x = 0; x < _puzzle.Width; x++) // add column definitions
        {
            CrosswordGrid.ColumnDefinitions.Add(new ColumnDefinition()
                {Width = new GridLength(SquareSide)}); // column of letter squares
            CrosswordGrid.ColumnDefinitions.Add(new ColumnDefinition()
                {Width = new GridLength(gapSize)}); // gap between columns
        }

        ColumnDefinition lastcol = new ColumnDefinition();
        CrosswordGrid.ColumnDefinitions.Add(lastcol);

        for (var y = 0; y < _puzzle.Height; y++) // add row definitions
        {
            CrosswordGrid.RowDefinitions.Add(new RowDefinition()
                {Height = new GridLength(SquareSide)}); // row of letter squares
            CrosswordGrid.RowDefinitions.Add(new RowDefinition()
                {Height = new GridLength(gapSize)}); // gap between rows
        }

        var lastRow = new RowDefinition();
        CrosswordGrid.RowDefinitions.Add(lastRow);

        for (var x = 0; x < _puzzle.Width; x++)
        {
            for (var y = 0; y < _puzzle.Height; y++)
            {
                _cellPaper[x, y] = new Canvas()
                {
                    Tag = Coords(x, y)
                };

                _cellPaper[x, y].MouseDown += Cell_MouseDown;

                if (_puzzle.Cell(new GridPoint(x, y)) == Crosswords.CrosswordGrid.BlackChar)
                {
                    _cellPaper[x, y].Background =_blackSquareBrush;
                }
                else
                {
                    _cellPaper[x, y].Background = Brushes.White;
                    int i = _puzzle.Index(x, y);
                    if (i > 0)
                    {
                        // Display index
                        var indexBlock = new TextBlock()
                            {FontSize = 11, Text = i.ToString(), Margin = new Thickness(1, -2, 0, 0)};
                        _cellPaper[x, y].Children.Add(indexBlock);
                    }
                }

                var b = new Border()
                {
                    BorderBrush = Brushes.DarkSlateGray, BorderThickness = new Thickness(1)
                    , Child = _cellPaper[x, y]
                };
                Grid.SetColumn(b, x * 2);
                Grid.SetRow(b, y * 2);
                CrosswordGrid.Children.Add(b);
            }
        }

        ListClues();

        // Add letters and word-separators (bars and hyphens) from Clue.PatternedWord
        foreach (string q in _puzzle.ClueKeyList)
        {
            Clue clu = _puzzle.ClueOf(q);
            int px = clu.Xstart;
            int py = clu.Ystart;
            if (clu.Direction == 'A')
            {
                px--;
                foreach (var t in clu.PatternedWordIntrinsic)
                {
                    if (t == ' ')
                    {
                        MakeRightBar(new GridPoint(px, py));
                    }
                    else if (t == '-')
                    {
                        MakeRightDash(new GridPoint(px, py));
                    }
                    else
                    {
                        px++;
                        if (t != Clue.UnknownLetterChar)
                        {
                            TextBlock letterBlock = new TextBlock()
                            {
                                FontFamily = ff, FontSize = 22, Text = t.ToString(), FontWeight = FontWeights.Bold
                                , Foreground = _letterBrush, Width = LetterWidth, TextAlignment = TextAlignment.Center
                            };
                            Canvas.SetLeft(letterBlock, 2);
                            Canvas.SetTop(letterBlock, 6);
                            _cellPaper[px, py].Children.Add(letterBlock);
                        }
                    }
                }
            }
            else
            {
                py--;
                foreach (var t in clu.PatternedWordIntrinsic)
                {
                    if (t == ' ')
                    {
                        MakeBottomBar(new GridPoint(px, py));
                    }
                    else if (t == '-')
                    {
                        MakeBottomDash(new GridPoint(px, py));
                    }
                    else
                    {
                        py++;
                        if (t != Clue.UnknownLetterChar)
                        {
                            var letterBlock = new TextBlock()
                            {
                                FontFamily = ff, FontSize = 22, Text = t.ToString(), FontWeight = FontWeights.Bold
                                , Foreground = _letterBrush, Width = LetterWidth, TextAlignment = TextAlignment.Center
                            };
                            Canvas.SetLeft(letterBlock, 2);
                            Canvas.SetTop(letterBlock, 6);
                            _cellPaper[px, py].Children.Add(letterBlock);
                        }
                    }
                }
            }
        }
    }

    private void Cell_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Canvas {Tag: string q})
        {
            // get coordinates
            GridPoint locus = CoordPoint(q);

            var t = string.Empty;
            ClueAListBox.SelectedIndex = -1;
            ClueDListBox.SelectedIndex = -1;
            foreach (var s in _puzzle.ClueKeyList)
            {
                if (_puzzle.ClueOf(s).IncludesCell(locus) is not null)
                {
                    t = s;
                    break;
                }
            }

            // Select the corresponding clue in the Across or Down clue ListBox 
            int r = -1;
            for (var z = 1; z < ClueAListBox.Items.Count; z++) // don't take first item which is the heading
            {
                if (ClueAListBox.Items[z] is ListBoxItem {Tag: string j})
                {
                    if (j == t)
                    {
                        r = z;
                        break;
                    }
                }
            }

            if (r >= 0)
            {
                ClueAListBox.SelectedIndex = r;
                return;
            }

            for (int z = 1; z < ClueDListBox.Items.Count; z++) // don't take first item which is the heading
            {
                if (ClueDListBox.Items[z] is ListBoxItem {Tag: string j})
                {
                    if (j == t)
                    {
                        r = z;
                        break;
                    }
                }
            }

            if (r >= 0)
            {
                ClueDListBox.SelectedIndex = r;
                return;
            }

            SwitchClueControls(false);
        }
    }

    private void MakeRightBar(GridPoint point)
    {
        var px = point.X * 2;
        var py = point.Y * 2;
        var r = new Rectangle() {Fill = _barBrush};
        CrosswordGrid.Children.Add(r);
        Grid.SetColumn(r, px + 1);
        Grid.SetRow(r, py);
    }

    private void MakeRightDash(GridPoint point)
    {
        var px = point.X * 2;
        var py = point.Y * 2;
        var r = new Rectangle() {Fill = _hyphenBrush, Height = 12};
        CrosswordGrid.Children.Add(r);
        Grid.SetColumn(r, px + 1);
        Grid.SetRow(r, py);
    }

    private void MakeBottomBar(GridPoint point)
    {
        var px = point.X * 2;
        var py = point.Y * 2;
        var r = new Rectangle() {Fill = _barBrush};
        CrosswordGrid.Children.Add(r);
        Grid.SetColumn(r, px);
        Grid.SetRow(r, py + 1);
    }

    private void MakeBottomDash(GridPoint point)
    {
        var px = point.X * 2;
        var py = point.Y * 2;
        var r = new Rectangle() {Fill = _hyphenBrush, Width = 12};
        CrosswordGrid.Children.Add(r);
        Grid.SetColumn(r, px);
        Grid.SetRow(r, py + 1);
    }

    private void ListClues()
    {
        var cluesDone = 0;
        var dimBrush = Brushes.Gray;
        var clueBrush = Brushes.RoyalBlue;
        var fSize = ClueListSmallCheckBox.IsChecked ?? false ? 12 : 15;
        var vis = ClueListHideCheckBox.IsChecked ?? false ? Visibility.Visible : Visibility.Collapsed;
        
        ClueAListBox.Items.Clear();
        ClueDListBox.Items.Clear();

        // Across heading
        SolidColorBrush pinceau;
        var clueList = _puzzle.CluesAcross;
        var clueCount = clueList.Count;
        var block = new TextBlock();
        var r = new Run() {Text = $"ACROSS: ", FontWeight = FontWeights.Bold,FontSize = fSize, Foreground = clueBrush};
        block.Inlines.Add(r);
        r = new Run() {Text = $"{clueList.Count} clues", Foreground = clueBrush};
        block.Inlines.Add(r);
        ClueAListBox.Items.Add(new ListBoxItem() {Content = block, IsHitTestVisible = false});

        foreach (var clu in clueList)
        {
            // default values for unsolved clue
            pinceau = clueBrush;
            var seen = Visibility.Visible;
            
            if (clu.IsComplete())
            {
                pinceau = dimBrush; // depends on CheckBox selection
                seen = vis; // depends on CheckBox selection
                cluesDone++;
            }

            var spl = new StackPanel() {Orientation = Orientation.Horizontal};

            block = new TextBlock()
                {Width = 80, Text = clu.Number.ToString(), FontWeight = FontWeights.Medium, FontSize = fSize, Foreground = pinceau};
            spl.Children.Add(block);

            if (clu.Content.Format.Length == 0)
            {
                clu.Content.Format = $"{clu.WordLength}";
            }

            block = new TextBlock() {Text = $" ({clu.Content.Format})",FontSize = fSize, Foreground = pinceau, Width = 80};
            spl.Children.Add(block);

            var wd = _puzzle.PatternedWordConstrained(clu.Key);
            block = new TextBlock()
                {FontFamily = _fixedFont, Foreground = pinceau, Text = wd,FontSize = fSize, Padding = new Thickness(0, 3, 0, 0)};
            spl.Children.Add(block);

            ClueAListBox.Items.Add(new ListBoxItem() {Content = spl, Tag = clu.Key, Visibility = seen});
        }

        clueList = _puzzle.CluesDown;
        clueCount += clueList.Count;
        block = new TextBlock();
        r = new Run() {Text = $"DOWN: ", FontWeight = FontWeights.Bold,FontSize = fSize, Foreground = clueBrush};
        block.Inlines.Add(r);
        r = new Run() {Text = $"{clueList.Count} clues", Foreground = clueBrush};
        block.Inlines.Add(r);
        ClueDListBox.Items.Add(new ListBoxItem() {Content = block, IsHitTestVisible = false});

        foreach (var clu in clueList)
        {
            pinceau = clueBrush;
            Visibility seen = Visibility.Visible;
            
            if (clu.IsComplete())
            {
                pinceau = dimBrush;
                seen = vis;
                cluesDone++;
            }

            var spl = new StackPanel() {Orientation = Orientation.Horizontal};

            block = new TextBlock()
                {Width = 80, Text = clu.Number.ToString(), FontWeight = FontWeights.Medium,FontSize = fSize, Foreground = pinceau};
            spl.Children.Add(block);

            if (clu.Content.Format.Length == 0)
            {
                clu.Content.Format = $"{clu.WordLength}";
            }

            block = new TextBlock() {Text = $" ({clu.Content.Format})", Foreground = pinceau,FontSize = fSize, Width = 80};
            spl.Children.Add(block);

            var wd = _puzzle.PatternedWordConstrained(clu.Key);
            block = new TextBlock()
                {FontFamily = _fixedFont, Foreground = pinceau,FontSize = fSize, Text = wd, Padding = new Thickness(0, 3, 0, 0)};
            spl.Children.Add(block);
            ClueDListBox.Items.Add(new ListBoxItem() {Content = spl, Tag = clu.Key, Visibility = seen});

            // Display progress
            ProgressTextBlock.Inlines.Clear();
            ProgressTextBlock.Inlines.Add(new Run()
                {Text = (cluesDone == clueCount) ? "COMPLETE" : $"Completed {cluesDone} of {clueCount} clues"});
            if (cluesDone < clueCount)
            {
                ProgressTextBlock.Inlines.Add(new Run()
                    {Text = $" [{clueCount - cluesDone}]", Foreground = Brushes.Tomato});
            }

            ProgressPanel.Children.Clear();
            for (var n = 0; n < cluesDone; n++)
            {
                ProgressPanel.Children.Add(ProgressShape(Brushes.SeaGreen));
            }
            for (var n = 0; n <clueCount - cluesDone; n++)
            {
                ProgressPanel.Children.Add( ProgressShape(Brushes.IndianRed));
            }
        }
        
    }

    private static Polygon ProgressShape(Brush pinceau)
    {
        var form = new Polygon();
        form.Points.Add(new Point(2,0));
        form.Points.Add(new Point(10,0));
        form.Points.Add(new Point(12,2));
        form.Points.Add(new Point(12,10));
        form.Points.Add(new Point(10,12));
        form.Points.Add(new Point(2,12));
        form.Points.Add(new Point(0,10));
        form.Points.Add(new Point(0,2));
        form.Fill = pinceau;
        form.Margin = new Thickness(1, 0, 1, 0);
        return form;
    }
        
    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        SaveCrossword();
        var cwin = new CreationWindow() {Owner = this};

        var q = cwin.ShowDialog();

        if (!q.HasValue)
        {
            return;
        }

        if (!q.Value)
        {
            return;
        }

        NameTextBlock.Text = _xWordTitle;
        PuzzleHeaderDockPanel.Visibility = Visibility.Visible;
        LoadPuzzleFromFile(cwin.NameOfTheGame);
            
        DisplayGrid();
        SwitchClueControls(false);
    }

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
        SaveCrossword();
        Close();
    }

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult answ = MessageBox.Show("Clear all the letters?", Jbh.AppManager.AppName
            , MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (answ == MessageBoxResult.Cancel)
        {
            return;
        }

        foreach (var key in _puzzle.ClueKeyList)
        {
            _puzzle.ClueOf(key).ClearLetters();
        }

        DisplayGrid();
    }

    private void SaveCrossword()
    {
        var path = System.IO.Path.Combine(CrosswordsPath, _xWordTitle + ".cwd");
        var fs = new FileStream(path, FileMode.Create);
        using var wri = new StreamWriter(fs, Clue.JbhEncoding);
        wri.WriteLine(_puzzle.Specification);
        foreach (var tk in _puzzle.ClueKeyList)
        {
            wri.WriteLine($"{tk}%{_puzzle.ClueOf(tk).Content.Specification()}");
        }
    }

    private void LoadPuzzleFromFile(string puzzlePath)
    {
        AnagramTextBox.Clear();
        TemplateTextBox.Clear();
        using (StreamReader rdr = new StreamReader(puzzlePath, Clue.JbhEncoding))
        {
            // load puzzle grid
            string? read = rdr.ReadLine();
            if (read is { } spec)
            {
                _puzzle = new CrosswordGrid(spec);
                // load clue content specifications
                while (!rdr.EndOfStream)
                {
                    string? readData = rdr.ReadLine();
                    if (readData is { } dat)
                    {
                        int pk = dat.IndexOf('%');
                        string clef = dat.Substring(0, pk);
                        _puzzle.ClueOf(clef).AddContent(dat.Substring(pk + 1));
                    }
                }
            }
        }

        _xWordTitle = System.IO.Path.GetFileNameWithoutExtension(puzzlePath);

        NameTextBlock.Text = _xWordTitle;

        DisplayGrid();
        SwitchClueControls(false);
        CheckVocab(clearFirst: true);
    }
   
    private void LettersApplyButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyLetters();
    }

    private void ApplyLetters()
    {
        if (!string.IsNullOrWhiteSpace(_selectedClueKey))
        {
            string nova = LettersEntryTextBox.Text;
            List<string> conflictingClues = _puzzle.CrossingConflictsDetected(_selectedClueKey, nova);

            if (conflictingClues.Count > 0)
            {
                string message = "Conflicts with other clues:";
                foreach (var clue in conflictingClues)
                {
                    message += $"\n{clue}";
                }

                MessageBox.Show(message, "Crosswords", MessageBoxButton.OK
                    , MessageBoxImage.Asterisk);
            }
            else
            {
                LettersEntryTextBox.Clear();
                _puzzle.ClueOf(_selectedClueKey).Content.Letters = nova;
                    
                var wd = _puzzle.PatternedWordConstrained(_selectedClueKey);
                var whether =_familiars.FoundInWordList(wd);
                if (!whether)
                {
                    if (!_strangers.Contains(wd))
                    {
                        _strangers.Add(wd);
                        RefreshStrangersList();
                    }
                }
                else
                {
                    ShowLexiconCount();
                }
                DisplayGrid();
                SwitchClueControls(false);
                SaveCrossword(); // in case of crash / power failure
            }
        }
    }

    private void ShowLexiconCount()
    {
        NotInListBlock.Text = $"NOT IN LIST ({_familiars.LexiconCount():#,#})";
    }
    
    private void RefreshStrangersList()
    {
        AddStrangerButton.IsEnabled = false;
        _strangers.Sort();
        StrangersSignal.Background = _strangers.Count == 0 ? Brushes.DarkSeaGreen : Brushes.OrangeRed;
        StrangersSignal.BorderBrush = _strangers.Count == 0 ? Brushes.SeaGreen : Brushes.Ivory;
        ShowLexiconCount();
        StrangerListBox.Items.Clear();
        foreach (var biz in _strangers)
        {
            StrangerListBox.Items.Add(new TextBlock()
            {
                Text = biz
                , FontFamily = new FontFamily("Liberation Mono"), Foreground = Brushes.Red
                , FontSize = 14
            });
        }
    }

    private void ClueListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox {SelectedItem: ListBoxItem {Tag: string k}} box) return;
        if (box.Equals(ClueAListBox))
        {
            ClueDListBox.SelectedIndex = -1;
        }
        else
        {
            ClueAListBox.SelectedIndex = -1;
        }
        ShowClueDetails(k);
    }

    private void ShowClueDetails(string clueCode)
    {
        var cloo = _puzzle.ClueOf(clueCode);
        HighLightClueInGrid(cloo);
        ShowClueTitle(cloo);
        _selectedClueKey = clueCode;
        SwitchClueControls(true);
        FormatEntryTextBox.Text = cloo.Content.Format;
        LetterCountTextBlock.Text = $"({cloo.WordLength} letters)";
        FillCluePatternCombo(cloo.WordLength);
        PatternTextBlock.Text = TemplateTextBox.Text = _puzzle.PatternedWordConstrained(clueCode);
        ExtraLettersTextBox.Clear();
        WarnLettersVsClue();
        TemplateListBox.Items.Clear();
        ClearMatchingResult();
    }

    private void ShowClueTitle(Clue indice)
    {
        var direction = indice.Direction == 'A' ? "Across" : "Down";
        ClueTitleTextBlock.Text = $"{indice.Number} {direction}";
        SecondClueTitleTextBlock.Text = $"{indice.Number} {direction}";
        if (indice.IsComplete())
        {
            ClueTitleTextBlock.Foreground=Brushes.Green;
            ClueTitleTextBorder.Background=Brushes.MintCream;
            ClueTitleTextBorder.BorderBrush=Brushes.Green;
            
            SecondClueTitleTextBlock.Foreground=Brushes.Green;
            SecondClueTitleTextBorder.Background=Brushes.MintCream;
            SecondClueTitleTextBorder.BorderBrush=Brushes.Green;
        }
        else
        {
            ClueTitleTextBlock.Foreground=Brushes.IndianRed;
            ClueTitleTextBorder.Background=Brushes.OldLace;
            ClueTitleTextBorder.BorderBrush=Brushes.IndianRed;
            
            SecondClueTitleTextBlock.Foreground=Brushes.IndianRed;
            SecondClueTitleTextBorder.Background=Brushes.OldLace;
            SecondClueTitleTextBorder.BorderBrush=Brushes.IndianRed;
        }
    }
        
    private void HighLightClueInGrid(Clue indice)
    {
        for (var xx = 0; xx < _puzzle.Width; xx++)
        {
            for (var yy = 0; yy < _puzzle.Height; yy++)
            {
                if (_puzzle.Cell(new GridPoint(xx, yy)) != Crosswords.CrosswordGrid.BlackChar)
                {
                    _cellPaper[xx, yy].Background = Brushes.White;
                }
            }
        }

        var sx = indice.Xstart;
        var sy = indice.Ystart;
        for (var a = 0; a < indice.WordLength; a++)
        {
            int px;
            int py;
            if (indice.Direction == 'A')
            {
                px = sx + a;
                py = sy;
            }
            else
            {
                px = sx;
                py = sy + a;
            }

            _cellPaper[px, py].Background =_highlightSquareBrush;
        }
    }

    private void FillCluePatternCombo(int length)
    {
        var variants = ClueContent.WordLengthPatterns(length);
        
        CluePatternCombo.Items.Clear();
        ClueVariantCombo.Items.Clear();
        CluePatternCombo.Items.Add(
            new ComboBoxItem() {Tag = $"{length}", Content = new TextBlock() {Foreground =Brushes.Blue, Text = $"ONE WORD {length}".PadLeft(2)}});
        var words = 1;
        foreach (var variant in variants)
        {
            if (variant.Count >= words)
            {
                words = variant.Count + 1;
                CluePatternCombo.Items.Add(
                    new ComboBoxItem() {IsHitTestVisible =false, Content = new TextBlock() {Foreground =Brushes.Blue,Text = $"{words} WORDS"}});    
            }
            var ts = ClueContent.TranslatedSpaced(length, variant);
            var tn = ClueContent.Translated(length, variant);
            CluePatternCombo.Items.Add(
                new ComboBoxItem() {Tag = tn, Content = new TextBlock() {Text = ts}});
        }
        
        CluePatternCombo.SelectedIndex = -1;
    }
    
    private void AnagramTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        CleanGivenLetters(AnagramTextBox, false);
        AnagramLengthBlock.Text = $"Length {AnagramTextBox.Text.Trim().Length}";
        AnagramListBox.Items.Clear();
        AnagramCountBlock.Text = string.Empty;
        AnagramRandomButton.IsEnabled = AnagramCountButton.IsEnabled
            = AnagramButton.IsEnabled = AnagramTextBox.Text.Trim().Length > 0;
    }
        
    private void FormatEntryTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (FormatEntryTextBox.Text.Trim().Length > 0)
        {
            var fmt = FormatEntryTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(_selectedClueKey))
            {
                int cluelength = _puzzle.ClueOf(_selectedClueKey).WordLength;
                if (ClueContent.GoodFormatSpecification(fmt, cluelength))
                {
                    FormatApplyButton.IsEnabled = true;
                    FormatApplyButton.IsDefault = true;
                }
                else
                {
                    FormatApplyButton.IsEnabled = false;
                }
            }
        }
        else
        {
            FormatApplyButton.IsEnabled = false;
        }
    }
    private void LettersEntryTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        CleanGivenLetters(LettersEntryTextBox, false);
        WarnLettersVsClue();
    }
        
    private static void CleanGivenLetters(TextBox box, bool allowFormatting)
    {
        var given = box.Text.ToUpper();
        var reformed = string.Empty;
        var p = box.CaretIndex;
        foreach (var t in given)
        {
            if (char.IsLetter(t))
            {
                reformed += t;
            }
            else
            {
                if (allowFormatting)
                {
                    if ( t is '.' or ' ' or '-')
                    {
                        reformed += t;
                    }    
                }
            }
        }

        box.Text = reformed;
        box.CaretIndex = p; 
    }
        
    private void WarnLettersVsClue()
    {
        if (!string.IsNullOrWhiteSpace(_selectedClueKey))
        {
            SolidColorBrush warning = Brushes.IndianRed;
            string pattern = _puzzle.UnPatternedWordConstrained(_selectedClueKey);
            string given = LettersEntryTextBox.Text.ToUpper().Trim();
            int p = LettersEntryTextBox.CaretIndex;
            LettersEntryTextBox.Text = given;
            LettersEntryTextBox.CaretIndex = p;
            char q = Matches(pattern, given);
            // Z, X, L or A
            // Z = nothing given
            // X = wrong length or illegal character
            // L = letter conflicts with previously entered letter
            // A = OK
            switch (q)
            {
                case 'Z':
                {
                    LettersApplyButton.IsEnabled = false; // impossible string (empty)
                    LettersConflictWarningTextBlock.Text = string.Empty;
                    break;
                }
                case 'X':
                {
                    LettersEntryTextBox.Foreground = Brushes.Red;
                    LettersApplyButton.IsEnabled = false; // impossible string (wrong length or illegal character)
                    LettersConflictWarningTextBlock.Visibility = Visibility.Visible;
                    LettersConflictWarningTextBlock.Text = "Wrong length or bad characters";
                    break;
                }
                case 'L':
                {
                    LettersEntryTextBox.Foreground
                        = warning; // allowable but conflicts with letters given in pattern
                    LettersApplyButton.IsEnabled = true;
                    LettersApplyButton.IsDefault = true;
                    LettersConflictWarningTextBlock.Visibility = Visibility.Visible;
                    LettersConflictWarningTextBlock.Text = "OK but conflicts with existing letters";
                    break;
                }
                default:
                {
                    LettersEntryTextBox.Foreground = Brushes.Black;
                    LettersApplyButton.IsEnabled = true;
                    LettersApplyButton.IsDefault = true;
                    LettersConflictWarningTextBlock.Visibility = Visibility.Hidden;
                    break;
                }
            }

        }
    }

    private char Matches(string patternString, string offeredString)
    {
        // empty offered string
        if (string.IsNullOrWhiteSpace(offeredString))
        {
            return 'Z';
        }

        // check offered word length against pattern
        if (patternString.Length != offeredString.Length)
        {
            return 'X';
        }

        // check for invalid characters in offered string
        bool validflag = true;
        foreach (var u in offeredString)
        {
            if (!char.IsLetter(u))
            {
                validflag = false;
            }
        }

        if (!validflag)
        {
            return 'X';
        }

        bool lettersflag = true;

        for (int p = 0; p < offeredString.Length; p++)
        {
            char u = offeredString[p];
            char v = patternString[p];

            if ((v != Clue.UnknownLetterChar) && (v != u))
            {
                lettersflag = false;
            } // a letter in the offered string is different from the pattern and the pattern's letter is not a wildcard
        }

        if (!lettersflag)
        {
            return 'L';
        }

        return 'A';
    }

    private void ClueClearButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_selectedClueKey))
        {
            Clue cloo = _puzzle.ClueOf(_selectedClueKey);
            cloo.Content.Letters = CrosswordWordTemplate.Stringy(cloo.WordLength, Clue.UnknownLetterChar);
            PatternTextBlock.Text = _puzzle.PatternedWordConstrained(_selectedClueKey);
            DisplayGrid();
        }
    }

    private static string CrosswordsPath => System.IO.Path.Combine(Jbh.AppManager.DataPath, "Crosswords");
       

    private void AnagramListButton_OnClick(object sender, RoutedEventArgs e)
    {
        ListAnagrams();
    }

    private void AnagramCountButton_OnClick(object sender, RoutedEventArgs e)
    {
        Cursor = Cursors.Wait;
        AnagramListBox.Items.Clear();
        _ =GetAnagramList(); // displays count
        Cursor = Cursors.Arrow;
    }

    private void ListAnagrams()
    {
        Cursor = Cursors.Wait;
        AnagramListBox.Items.Clear();
        var mixes = GetAnagramList();
        foreach (var a in mixes)
        {
            AnagramListBox.Items.Add(a);
        }
        Cursor = Cursors.Arrow;
    }

    private List<string> GetAnagramList()
    {
        var source = AnagramTextBox.Text.Trim();
        bool self = _familiars.FoundInWordList(source);
        var mixes= _familiars.GetAnagrams(source);
        var g = mixes.Count;
        if (self)
        {
            g--;
            AnagramCountBlock.Text = (g < 1) ? "No others" : g > 1 ? $"{g:#,0} others" : "1 other";    
        }
        else
        {
            AnagramCountBlock.Text = (g < 1) ? "No matches" : g > 1 ? $"{g:#,0} matches" : "1 match";
        }
        return mixes;
    }
    
    private void TemplateListButton_OnClick(object sender, RoutedEventArgs e)
    {
        ListTemplateMatches();
    }
    
    private void TemplateCountButton_OnClick(object sender, RoutedEventArgs e)
    {
        CountTemplateMatches();
    }
       
    private void ListTemplateMatches()
    {
        Cursor = Cursors.Wait;
        List<string> hits = TemplateMatchesList();
        TemplateListBox.Items.Clear();
        foreach (var wd in hits)
        {
            TemplateListBox.Items.Add(wd);
        }
        ShowMatchingResult(TemplateListBox.Items.Count);
        Cursor = Cursors.Arrow;
    }
        
    private List<string> TemplateMatchesList()
    {
        var onlyCaps = CapitalsCheckBox.IsChecked ?? false;
        var onlyRevs = ReversibleCheckBox.IsChecked ?? false;
        var pattern = TemplateTextBox.Text;
        var extras = ExtraLettersTextBox.Text.Trim();
        return  _familiars.GetTemplateMatches(pattern, onlyCaps, onlyRevs, extras);
    }
 
    private List<string> TemplateMatchesIndividualWordsList(string pattern)
    {
        var finds = new List<string>();
        var onlyCaps =false;
        var onlyRevs = false;
        var extras = string.Empty;
        pattern = pattern.Replace('-', ' '); // make all word breaks spaces (no hyphens)
        string[] words = pattern.Split(" ");
        if (words.Length < 2)
        {
            return finds;
        }

        for (int w = 0; w < words.Length; w++)
        {
            var partialList = _familiars.GetTemplateMatches(words[w], onlyCaps, onlyRevs,  extras);
            foreach (var match in partialList)
            {
                string combi = string.Empty;
                for (int u = 0; u < words.Length; u++)
                {
                    if (u == w)
                    {
                        combi += $" {match}";
                    }
                    else
                    {
                        combi += $" {words[u]}";
                    }
                }
                    
                finds.Add(combi[1..]);
            }
        }
            
        return finds;
    }
        
    private List<string> TemplateMatchesUnSpacedList(string pattern)
    {
        var onlyCaps =false;
        var onlyRevs = false;
        var extras = string.Empty;
        var unSpaced = string.Empty;
        foreach (var ch in pattern)
        {
            if (ch != ' ')
            {
                if (ch != '-')
                {
                    unSpaced += ch;
                }
            }
        }
        return _familiars.GetTemplateMatches(unSpaced, onlyCaps, onlyRevs,  extras);
    }
        
    private void CountTemplateMatches()
    {
        Cursor = Cursors.Wait;
        TemplateListBox.Items.Clear();
        List<string> hits = TemplateMatchesList();
        int count = hits.Count;
        ShowMatchingResult(count);
        Cursor = Cursors.Arrow;
    }

    private void TemplateBox_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBox {SelectedItem: string word})
        {
            if (SelectedClueGrid.IsVisible)
            {
                LettersEntryTextBox.Text = Constrain(word);
                TemplateTextBox.Clear();
            }
        }
    }

    private void AnagramBox_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBox {SelectedItem: string word})
        {
            string constrained = Constrain(word);
            if (!string.IsNullOrWhiteSpace(_selectedClueKey))
            {
                if (_puzzle.ClueOf(_selectedClueKey).WordLength == constrained.Length)
                {
                    LettersEntryTextBox.Text = constrained;
                    AnagramTextBox.Clear();
                }
            }
        }
    }

    private static string Constrain(string input)
    {
        StringBuilder builder = new StringBuilder();
        foreach (var c in input)
        {
            if (char.IsLetter(c))
            {
                builder.Append(c);
            }
        }

        var output = builder.ToString();
        output = UnAccent(output);
        return output.ToUpperInvariant();
    }

    private static string UnAccent(string quoi)
    {
        char[] toReplace = "àèìòùÀÈÌÒÙ äëïöüÄËÏÖÜ âêîôûÂÊÎÔÛ áéíóúÁÉÍÓÚðÐýÝ ãñõÃÑÕšŠžŽçÇåÅøØ".ToCharArray();
        char[] replaceChars = "aeiouAEIOU aeiouAEIOU aeiouAEIOU aeiouAEIOUdDyY anoANOsSzZcCaAoO".ToCharArray();
        for (int index = 0; index <= toReplace.GetUpperBound(0); index++)
        {
            quoi = quoi.Replace(toReplace[index], replaceChars[index]);
        }

        return quoi;
    }

    private void ListButton_OnClick(object sender, RoutedEventArgs e)
    {
        WordListWindow wlw = new(String.Empty, _familiars) {Owner = this};
        wlw.ShowDialog();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        SaveCrossword();
        OpenFileDialog dlg = new OpenFileDialog()
        {
            Filter = "Crossword files (*.cwd)|*.cwd", InitialDirectory = CrosswordsPath, Title = "Open crossword"
        };
        bool? ans = dlg.ShowDialog();
        if (ans ?? false)
        {
            LoadPuzzleFromFile(dlg.FileName);
        }
    }
       
    private static string? MostRecentlySavedGamePath()
    {
        var gameFiles = Directory.GetFiles(CrosswordsPath, "*.cwd");
        if (gameFiles.Length < 1)
        {
            return null;
        }
        List<Tuple<DateTime, string>> history = new();
        foreach (string fileSpec in gameFiles)
        {
            DateTime d = File.GetLastWriteTime(fileSpec);
            Tuple<DateTime, string> jeu = new Tuple<DateTime, string>(d, fileSpec);
            history.Add(jeu);
        }

        history.Sort();
        Tuple<DateTime, string> newest = history.Last();
        
        return newest.Item2;
    }
       
    private void FormatApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        ApplyFormat();
    }

    private void ApplyFormat()
    {
        if (FormatEntryTextBox.Text.Trim().Length > 0)
        {
            var fmt = FormatEntryTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(_selectedClueKey))
            {
                if (ClueContent.GoodFormatSpecification(fmt, _puzzle.ClueOf(_selectedClueKey).WordLength))
                {
                    Cursor= Cursors.Wait;
                    _puzzle.ClueOf(_selectedClueKey).Content.Format = fmt;
                    DisplayGrid();
                    ShowClueDetails(_selectedClueKey);
                    FormatApplyButton.IsEnabled = false;
                    Cursor= Cursors.Arrow;
                }
                else
                {
                    MessageBox.Show("Not a valid format for this clue length", "Crosswords", MessageBoxButton.OK
                        , MessageBoxImage.Information);
                }
            }
        }
       
    }
  
    private void CheckVocab(bool clearFirst)
    {
        Cursor = Cursors.Wait;
        var bizarre = new List<string>();
        
        if (clearFirst){_strangers.Clear();}
        
        foreach (var mot in _strangers)
        {
            if (!_familiars.FoundInWordList(mot))
            {
                bizarre.Add(mot);
            }
        }

        List<Clue> clist = _puzzle.CluesAcross;

        foreach (var clu in clist)
        {
            if (clu.Content.Format.Length == 0)
            {
                clu.Content.Format = $"{clu.WordLength}";
            }

            if (clu.IsComplete())
            {
                var wd = _puzzle.PatternedWordConstrained(clu.Key);
                var whether = _familiars.FoundInWordList(wd);
                if (!whether)
                {
                    if (!bizarre.Contains(wd))
                    {
                        bizarre.Add(wd);
                    }
                }
            }
        }

        clist = _puzzle.CluesDown;

        foreach (var clu in clist)
        {
            if (clu.Content.Format.Length == 0)
            {
                clu.Content.Format = $"{clu.WordLength}";
            }

            if (clu.IsComplete())
            {
                var wd = _puzzle.PatternedWordConstrained(clu.Key);
                var whether = _familiars.FoundInWordList(wd);
                if (!whether)
                {
                    if (!bizarre.Contains(wd))
                    {
                        bizarre.Add(wd);
                    }
                }
            }
            
        }

        bizarre.Sort();
        _strangers.Clear();
        foreach (var mot in bizarre)
        {
            _strangers.Add(mot);
        }

        RefreshStrangersList();
        Cursor = Cursors.Arrow;
    }
    
    private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (FormatEntryTextBox.IsFocused)
            {
                if (FormatApplyButton.IsEnabled)
                {
                    ApplyFormat();
                }
            }
            else if (LettersEntryTextBox.IsFocused)
            {
                if (LettersApplyButton.IsEnabled)
                {
                    ApplyLetters();
                }
            }
            else if (AnagramTextBox.IsFocused)
            {
                ListAnagrams();
            }
            else if (TemplateTextBox.IsFocused)
            {
                ListTemplateMatches();
            }
        }
    }

    private void CluePatternCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CluePatternCombo.SelectedItem is ComboBoxItem {Tag: string format})
        {
            FormatEntryTextBox.Text = format;
            ClueVariantCombo.Items.Clear();
            List<string> variants = ClueContent.CommaHyphenPermutations(format);
            foreach (var v in variants)
            {
                ClueVariantCombo.Items.Add(new ComboBoxItem() {Tag = v, Content = new TextBlock() {Text = v}});
            }

            ClueVariantCombo.SelectedIndex = 0;
        }
    }

    private void ClueVariantCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClueVariantCombo.SelectedItem is ComboBoxItem {Tag: string format})
        {
            FormatEntryTextBox.Text = format;
        }
    }

    

    private void PointersButton_OnClick(object sender, RoutedEventArgs e)
    {
        var win = new PointersWindow() {Owner = this};
        win.ShowDialog();
    }

    private void TemplateListEachButton_OnClick(object sender, RoutedEventArgs e)
    {
        Cursor= Cursors.Wait;
        TemplateListBox.Items.Clear();
        List<string> finds = TemplateMatchesIndividualWordsList(TemplateTextBox.Text.Trim());
        foreach (var find in finds)
        {
            TemplateListBox.Items.Add(find);
        }
        ShowMatchingResult(TemplateListBox.Items.Count);
        Cursor = Cursors.Arrow;
    }

    private void TemplateListWholeButton_OnClick(object sender, RoutedEventArgs e)
    {
        Cursor= Cursors.Wait;
        TemplateListBox.Items.Clear();
        List<string> finds =TemplateMatchesUnSpacedList(TemplateTextBox.Text.Trim());
        foreach (var find in finds)
        {
            TemplateListBox.Items.Add(find);
        }
        ShowMatchingResult(TemplateListBox.Items.Count);
        Cursor = Cursors.Arrow;
    }

    private void StrangerListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StrangerListBox.SelectedItem is TextBlock {Text: { } word})
        {
            AddStrangerButton.Tag = word;
            AddStrangerButton.IsEnabled = true;AddStrangerButton.Tag = word;
            AddStrangerButton.IsEnabled = true;
        }
        else
        {
            AddStrangerButton.Tag = null;
            AddStrangerButton.IsEnabled = false;                
        }
    }

    private void AddStrangerButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (AddStrangerButton.Tag is string word)
        {
            var win = new WordListWindow(word, _familiars){Owner = this};
            win.ShowDialog();
            CheckVocab(clearFirst: false);
        }
    }

    private void ClearExtrasButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExtraLettersTextBox.Clear();
    }
    
    private void TemplateTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        CleanGivenLetters(TemplateTextBox, true);
        ClearMatchingResult();

        _disableCheckBoxesTrigger = true;
        CapitalsCheckBox.IsChecked = false;
        ReversibleCheckBox.IsChecked = false;
        _disableCheckBoxesTrigger = false;

        // enable list buttons according to whether pattern is multi-word
        var pattern = TemplateTextBox.Text.Trim().Replace('-', ' '); // make all word breaks spaces (no hyphens)
        var words = pattern.Split(" ");
        ListEachButton.IsEnabled = ListWholeButton.IsEnabled = words.Length > 1;
    }
    
    private void ExtraLettersTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        CleanGivenLetters(ExtraLettersTextBox, false);
        ClearMatchingResult();
    }
 
    private void MatchingCheckBox_Toggled(object sender, RoutedEventArgs e)
    {
        if (_disableCheckBoxesTrigger)
        {
            return;
        }
        ClearMatchingResult();
    }
    
    private void ClearMatchingResult()
    {
        TemplateListBox.Items.Clear();
        TemplateCountBlock.Text = "Unchecked";
        TemplateCountBlock.Foreground= Brushes.DimGray;
    }
    
    private void ShowMatchingResult(int count)
    {
        TemplateCountBlock.Text = (count > 499) ? "500+ matches" :
            (count < 1) ? "No matches" :
            count > 1 ? $"{count:#,0} matches" : "1 match";
        TemplateCountBlock.Foreground= Brushes.DarkViolet;
    }

    private void ClueListOptionCheckBox_OnChecked(object sender, RoutedEventArgs e)
    {
        ListClues();
    }

    private void AnagramRandomButton_OnClick(object sender, RoutedEventArgs e)
    {
        Cursor = Cursors.Wait;
        AnagramListBox.Items.Clear();
        var mixes = TenMixes(AnagramTextBox.Text.Trim().ToLower());
        foreach (var a in mixes)
        {
            AnagramListBox.Items.Add(a);
        }

        Cursor = Cursors.Arrow;
    }

    private static List<string> TenMixes(string seed)
    {
        var mixList = new List<string>();
        while (mixList.Count < 10)
        {
            var array = seed.ToCharArray();
            var rng = new Random();
            var n = array.Length;
            while (n > 1)
            {
                n--;
                var k = rng.Next(n + 1);
                (array[k], array[n]) = (array[n], array[k]);
            }
            mixList.Add( new string(array));
        }
        mixList.Sort();
        return mixList;
    }
}