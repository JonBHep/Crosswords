﻿using System;
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

namespace Crosswords
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        // https://www.crosswordunclued.com/2009/09/crossword-grid-symmetry.html

       
        public MainWindow()
        {
            InitializeComponent();
            const string defaultSpecification
                = "15......#........#.#.#.#.#.#.#.#........#......#.#.#.#.#.#.#.####............#.#.#.#.#.###.#....###........#.#.#.#.#.#.#.#........###....#.###.#.#.#.#.#............####.#.#.#.#.#.#.#......#........#.#.#.#.#.#.#.#........#......";
            _puzzle = new CrosswordGrid(defaultSpecification);
        }

        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const double SquareSide = 36;
        private const double LetterWidth = 30;
        private readonly FontFamily _fixedFont = new("Liberation Mono");
        private CrosswordGrid _puzzle;
        private string _xWordTitle = "default";
        
        private readonly Brush _barBrush = Brushes.Blue;
        private readonly Brush _letterBrush = Brushes.Black;
        private readonly Brush _blackSquareBrush = Brushes.DimGray;
        
        private string _selectedClueKey = string.Empty;
        private bool _disableCheckBoxesTrigger;
        private Canvas[,] _cellPaper = new Canvas[0, 0];

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
            FillGamesComboBox();
        }

        private void SwitchClueControls(bool on)
        {
            Visibility vis = on ? Visibility.Visible : Visibility.Hidden;
            SelectedClueGrid.Visibility = vis;
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
            VocabButton.IsEnabled = false;
            ListEachButton.IsEnabled = false;
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

            XwordGrid.Children.Clear(); // clear Grid
            XwordGrid.ColumnDefinitions.Clear();
            XwordGrid.RowDefinitions.Clear();

            for (int x = 0; x < _puzzle.Width; x++) // add column definitions
            {
                XwordGrid.ColumnDefinitions.Add(new ColumnDefinition()
                    {Width = new GridLength(SquareSide)}); // column of letter squares
                XwordGrid.ColumnDefinitions.Add(new ColumnDefinition()
                    {Width = new GridLength(gapSize)}); // gap between columns
            }

            ColumnDefinition lastcol = new ColumnDefinition();
            XwordGrid.ColumnDefinitions.Add(lastcol);

            for (int y = 0; y < _puzzle.Height; y++) // add row definitions
            {
                XwordGrid.RowDefinitions.Add(new RowDefinition()
                    {Height = new GridLength(SquareSide)}); // row of letter squares
                XwordGrid.RowDefinitions.Add(new RowDefinition()
                    {Height = new GridLength(gapSize)}); // gap between rows
            }

            RowDefinition lastrow = new RowDefinition();
            XwordGrid.RowDefinitions.Add(lastrow);

            for (int x = 0; x < _puzzle.Width; x++)
            {
                for (int y = 0; y < _puzzle.Height; y++)
                {
                    _cellPaper[x, y] = new Canvas()
                    {
                        Tag = Coords(x, y)
                    };

                    _cellPaper[x, y].MouseDown += Cell_MouseDown;

                    if (_puzzle.Cell(new GridPoint(x, y)) == CrosswordGrid.BlackChar)
                    {
                        _cellPaper[x, y].Background =_blackSquareBrush;
                    }
                    else
                    {
                        _cellPaper[x, y].Background = Brushes.White;
                        int i = _puzzle.Index(x, y);
                        if (i > 0)
                        {
                            TextBlock indexBlock = new TextBlock()
                                {FontSize = 8, Text = i.ToString(), Margin = new Thickness(2, 0, 0, 0)};
                            _cellPaper[x, y].Children.Add(indexBlock);
                        }
                    }

                    Border b = new Border()
                    {
                        BorderBrush = Brushes.DarkSlateGray, BorderThickness = new Thickness(1)
                        , Child = _cellPaper[x, y]
                    };
                    Grid.SetColumn(b, x * 2);
                    Grid.SetRow(b, y * 2);
                    XwordGrid.Children.Add(b);
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
            XwordGrid.Children.Add(r);
            Grid.SetColumn(r, px + 1);
            Grid.SetRow(r, py);
        }

        private void MakeRightDash(GridPoint point)
        {
            var px = point.X * 2;
            var py = point.Y * 2;
            var r = new Rectangle() {Fill = _barBrush, Height = 8};
            XwordGrid.Children.Add(r);
            Grid.SetColumn(r, px + 1);
            Grid.SetRow(r, py);
        }

        private void MakeBottomBar(GridPoint point)
        {
            var px = point.X * 2;
            var py = point.Y * 2;
            var r = new Rectangle() {Fill = _barBrush};
            XwordGrid.Children.Add(r);
            Grid.SetColumn(r, px);
            Grid.SetRow(r, py + 1);
        }

        private void MakeBottomDash(GridPoint point)
        {
            var px = point.X * 2;
            var py = point.Y * 2;
            var r = new Rectangle() {Fill = _barBrush, Width = 8};
            XwordGrid.Children.Add(r);
            Grid.SetColumn(r, px);
            Grid.SetRow(r, py + 1);
        }

        private void ListClues()
        {
            int cluesDone = 0;
            SolidColorBrush dimbrush = Brushes.Silver;
            SolidColorBrush abrush = Brushes.RoyalBlue;
            SolidColorBrush dbrush = Brushes.DarkViolet;

            ClueAListBox.Items.Clear();
            ClueDListBox.Items.Clear();

            // Across heading
            SolidColorBrush pinceau;
            List<Clue> clueList = _puzzle.CluesAcross;
            var clueCount = clueList.Count;
            TextBlock block = new TextBlock();
            Run r = new Run() {Text = $"ACROSS: ", FontWeight = FontWeights.Bold, Foreground = abrush};
            block.Inlines.Add(r);
            r = new Run() {Text = $"{clueList.Count} clues", Foreground = abrush};
            block.Inlines.Add(r);
            ClueAListBox.Items.Add(new ListBoxItem() {Content = block, IsHitTestVisible = false});

            foreach (Clue clu in clueList)
            {
                pinceau = abrush;
                if (clu.IsComplete())
                {
                    pinceau = dimbrush;
                    cluesDone++;
                }

                StackPanel spl = new StackPanel() {Orientation = Orientation.Horizontal};

                block = new TextBlock()
                    {Width = 80, Text = clu.Number.ToString(), FontWeight = FontWeights.Medium, Foreground = pinceau};
                spl.Children.Add(block);

                if (clu.Content.Format.Length == 0)
                {
                    clu.Content.Format = $"{clu.WordLength}";
                }

                block = new TextBlock() {Text = $" ({clu.Content.Format})", Foreground = pinceau, Width = 80};
                spl.Children.Add(block);

                string wd = _puzzle.PatternedWordConstrained(clu.Key);
                block = new TextBlock()
                    {FontFamily = _fixedFont, Foreground = pinceau, Text = wd, Padding = new Thickness(0, 3, 0, 0)};
                spl.Children.Add(block);

                ClueAListBox.Items.Add(new ListBoxItem() {Content = spl, Tag = clu.Key});
            }

            clueList = _puzzle.CluesDown;
            clueCount += clueList.Count;
            block = new TextBlock();
            r = new Run() {Text = $"DOWN: ", FontWeight = FontWeights.Bold, Foreground = dbrush};
            block.Inlines.Add(r);
            r = new Run() {Text = $"{clueList.Count} clues", Foreground = dbrush};
            block.Inlines.Add(r);
            ClueDListBox.Items.Add(new ListBoxItem() {Content = block, IsHitTestVisible = false});

            foreach (Clue clu in clueList)
            {
                pinceau = dbrush;
                if (clu.IsComplete())
                {
                    pinceau = dimbrush;
                    cluesDone++;
                }

                StackPanel spl = new StackPanel() {Orientation = Orientation.Horizontal};

                block = new TextBlock()
                    {Width = 80, Text = clu.Number.ToString(), FontWeight = FontWeights.Medium, Foreground = pinceau};
                spl.Children.Add(block);

                if (clu.Content.Format.Length == 0)
                {
                    clu.Content.Format = $"{clu.WordLength}";
                }

                block = new TextBlock() {Text = $" ({clu.Content.Format})", Foreground = pinceau, Width = 80};
                spl.Children.Add(block);

                string wd = _puzzle.PatternedWordConstrained(clu.Key);
                block = new TextBlock()
                    {FontFamily = _fixedFont, Foreground = pinceau, Text = wd, Padding = new Thickness(0, 3, 0, 0)};
                spl.Children.Add(block);
                ClueDListBox.Items.Add(new ListBoxItem() {Content = spl, Tag = clu.Key});

                // Display progress
                ProgressTextBlock.Inlines.Clear();
                ProgressTextBlock.Inlines.Add(new Run()
                    {Text = (cluesDone == clueCount) ? "COMPLETE" : $"Completed {cluesDone} of {clueCount} clues"});
                if (cluesDone < clueCount)
                {
                    ProgressTextBlock.Inlines.Add(new Run()
                        {Text = $" [{clueCount - cluesDone}]", Foreground = Brushes.Tomato});
                }

                PuzzleProgressBar.Maximum = clueCount;
                PuzzleProgressBar.Value = cluesDone;
            }

            if (cluesDone == clueCount)
            {
                VocabButton.IsEnabled = true;
            }

        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            CreationWindow cwin = new CreationWindow() {Owner = this};

            bool? q = cwin.ShowDialog();

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
            string path = System.IO.Path.Combine(CrosswordsPath, _xWordTitle + ".cwd");
            FileStream fs = new FileStream(path, FileMode.Create);
            using StreamWriter wri = new StreamWriter(fs, Clue.JbhEncoding);
            wri.WriteLine(_puzzle.Specification);
            foreach (string tk in _puzzle.ClueKeyList)
            {
                wri.WriteLine($"{tk}%{_puzzle.ClueOf(tk).Content.Specification()}");
            }
        }

        private void LoadPuzzleFromFile(string puzzlePath)
        {
            SaveCrossword();
            VocabButton.IsEnabled = false;
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
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog()
            {
                Filter = "Crossword files (*.cwd)|*.cwd", InitialDirectory = CrosswordsPath, Title = "Open crossword"
            };
            bool? ans = dlg.ShowDialog();
            if ((ans.HasValue) && (ans.Value))
            {
                LoadPuzzleFromFile(dlg.FileName);
            }
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
                    DisplayGrid();
                    SwitchClueControls(false);
                }
            }
        }

        private void ClueListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox {SelectedItem: ListBoxItem {Tag: string k} })
            {
               ShowClueDetails(k);
            }
        }

        private void ShowClueDetails(string clueCode)
        {
            Clue cloo = _puzzle.ClueOf(clueCode);
            HighLightClueInGrid(cloo);
            string dirn = cloo.Direction == 'A' ? "Across" : "Down";
            ClueTitleTextBlock.Text = $"{cloo.Number} {dirn}";
            _selectedClueKey = clueCode;
            SwitchClueControls(true);
            FormatEntryTextBox.Text = cloo.Content.Format;
            FillCluePatternCombo(cloo.WordLength);
            PatternTextBox.Text = TemplateTextBox.Text = _puzzle.PatternedWordConstrained(clueCode);
            ShowUnspacedCheckBox();
            ListEachButton.IsEnabled = false;
            ExtraLettersTextBox.Clear();
            WarnLettersVsClue();
            CountTemplateMatches();
        }

        private void ShowUnspacedCheckBox()
        {
            var enable = TemplateTextBox.Text.Contains(' ') || TemplateTextBox.Text.Contains('-');

            UnspacedCheckBox.IsChecked = false;
            UnspacedCheckBox.Visibility = enable  ? Visibility.Visible: Visibility.Hidden;
        }
        
        private void HighLightClueInGrid(Clue indice)
        {
            for (int xx = 0; xx < _puzzle.Width; xx++)
            {
                for (int yy = 0; yy < _puzzle.Height; yy++)
                {

                    if (_puzzle.Cell(new GridPoint(xx, yy)) != CrosswordGrid.BlackChar)
                    {
                        _cellPaper[xx, yy].Background = Brushes.White;
                    }
                }
            }

            int sx = indice.Xstart;
            int sy = indice.Ystart;
            for (int a = 0; a < indice.WordLength; a++)
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

                _cellPaper[px, py].Background = Brushes.NavajoWhite;
            }
        }

        private void FillCluePatternCombo(int length)
        {
            List<string> patterns = ClueContent.LetterPatterns(length);
            CluePatternCombo.Items.Clear();
            ClueVariantCombo.Items.Clear();
            CluePatternCombo.Items.Add(
                new ComboBoxItem() {Tag = $"{length}", Content = new TextBlock() {Text = $"{length}"}});
            foreach (var pattern in patterns)
            {
                CluePatternCombo.Items.Add(
                    new ComboBoxItem() {Tag = pattern, Content = new TextBlock() {Text = pattern}});
            }

            CluePatternCombo.SelectedIndex = -1;
        }

        private void LettersEntryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CleanGivenLetters();
            WarnLettersVsClue();
        }

        private void CleanGivenLetters()
        {
            var given = LettersEntryTextBox.Text.ToUpper();
            var reformed = string.Empty;
            var p = LettersEntryTextBox.CaretIndex;
            foreach (var t in given)
            {
                if (char.IsLetter(t))
                {
                    reformed += t;
                }
            }

            LettersEntryTextBox.Text = reformed;
            LettersEntryTextBox.CaretIndex = p; 
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

        /// <summary>
        /// Converts content of TextBox to upper case
        /// </summary>
        private void MakeUpperTextBoxText(TextBox box)
        {
            string letters = box.Text.Trim();
            string lettersU = letters.ToUpper();
            if (lettersU != letters)
            {
                int pt = box.CaretIndex;
                box.Text = lettersU;
                box.CaretIndex = pt;
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
                PatternTextBox.Text = _puzzle.PatternedWordConstrained(_selectedClueKey);
                DisplayGrid();
            }
        }

        private static string CrosswordsPath => System.IO.Path.Combine(Jbh.AppManager.DataPath, "Crosswords");
       

        private void AnagramButton_OnClick(object sender, RoutedEventArgs e)
        {
            GetAnagrams();
        }

        private void GetAnagrams()
        {
            Cursor = Cursors.Wait;
            AnagramListBox.Items.Clear();
            string source = AnagramTextBox.Text.Trim();
            var known = new Connu();
            List<string> mixes = known.GetAnagrams(source);
            foreach (var a in mixes)
            {
                AnagramListBox.Items.Add(a);
            }

            int g = AnagramListBox.Items.Count;
            AnagramCountBlock.Text = (g < 1) ? "No matches" : g > 1 ? $"{g:#,0} matches" : "1 match";
            Cursor = Cursors.Arrow;
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
            var g = TemplateListBox.Items.Count;
            TemplateCountBlock.Text = (g < 1) ? "No matches" : g > 1 ? $"{g:#,0} matches" : "1 match";
            
            var pattern =TemplateTextBox.Text.Trim().Replace('-', ' '); // make all word breaks spaces (no hyphens)
            var words = pattern.Split(" ");
            ListEachButton.IsEnabled = words.Length > 1;
            
            Cursor = Cursors.Arrow;
        }
        
        private List<string> TemplateMatchesList()
        {
            var onlyCaps = CapitalsCheckBox.IsChecked ?? false;
            var onlyRevs = ReversibleCheckBox.IsChecked ?? false;
            var alsoWhole = UnspacedCheckBox.IsChecked ?? false;
            var pattern = TemplateTextBox.Text;
            var extras = ExtraLettersTextBox.Text.Trim();
            var known = new Connu();
            return known.GetTemplateMatches(pattern, onlyCaps, onlyRevs, alsoWhole, extras);
        }
        
        
        private void DisplayTemplateMatchesIndividualWordsList()
        {
            var onlyCaps =false;
            var onlyRevs = false;
            var pattern = TemplateTextBox.Text.Trim();
            var extras =string.Empty;
            var known = new Connu();
            pattern = pattern.Replace('-', ' '); // make all word breaks spaces (no hyphens)
            string[] words = pattern.Split(" ");
            if (words.Length < 2)
            {
                return;
            }

            int w = 1;
            foreach (var word in words)
            {
                TemplateListBox.Items.Add(new ListBoxItem()
                {
                    IsHitTestVisible = false
                    , Content = new TextBlock()
                        {Text = $"WORD {w}", FontWeight = FontWeights.Bold, Foreground = Brushes.Blue}
                });
                
                var splitList = known.GetTemplateMatches(word, onlyCaps, onlyRevs, false, extras);
                foreach (var s in splitList)
                {
                    TemplateListBox.Items.Add(s);
                }

                w++;
            }
        }
        
        private void CountTemplateMatches()
        {
            Cursor = Cursors.Wait;
            TemplateListBox.Items.Clear();
            List<string> hits = TemplateMatchesList();
            int count = hits.Count;
            TemplateCountBlock.Text = (count > 999) ? "1,000+ matches" :
                (count < 1) ? "No matches" :
                count > 1 ? $"{count:#,0} matches" : "1 match";
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
            WordListWindow wlw = new() {Owner = this};
            wlw.ShowDialog();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (GamesComboBox.SelectedItem is ComboBoxItem {Tag: string path})
            {
                OpenButton.IsEnabled = false;
                PuzzleHeaderDockPanel.Visibility = Visibility.Visible;
                LoadPuzzleFromFile(path);
            }
        }
       
        private void FillGamesComboBox()
        {
            GamesComboBox.Items.Clear();
            OpenButton.IsEnabled = false;
            string[] gameFiles = Directory.GetFiles(CrosswordsPath, "*.cwd");

            List<Tuple<DateTime, string>> history = new();
            foreach (string fileSpec in gameFiles)
            {
                DateTime d = File.GetLastWriteTime(fileSpec);
                Tuple<DateTime, string> jeu = new Tuple<DateTime, string>(d, fileSpec);
                history.Add(jeu);
            }

            history.Sort();
            history.Reverse();
            int top = Math.Min(10, history.Count);
            for (int x = 0; x < top; x++)
            {
                string caption = System.IO.Path.GetFileNameWithoutExtension(history[x].Item2);
                if (caption != "default")
                {
                    ComboBoxItem item = new ComboBoxItem
                    {
                        Tag = history[x].Item2
                        , Content = new TextBlock()
                        {
                            FontFamily = new FontFamily("Lucida Console")
                            , Text = caption
                        }
                    };
                    GamesComboBox.Items.Add(item);
                }
            }

            if (GamesComboBox.Items.Count > 0)
            {
                GamesComboBox.SelectedIndex = 0;
            }
        }

        private void GamesComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OpenButton.IsEnabled = GamesComboBox.SelectedItem is { };
        }

        private void AnagramTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            AnagramLengthBlock.Text = $"({AnagramTextBox.Text.Trim().Length})";
            AnagramListBox.Items.Clear();
            AnagramCountBlock.Text = String.Empty;
            MakeUpperTextBoxText(AnagramTextBox);
            AnagramButton.IsEnabled = AnagramTextBox.Text.Trim().Length > 0;
        }

        private void FormatApplyButton_OnClick(object sender, RoutedEventArgs e)
        {
            ApplyFormat();
        }

        private void ApplyFormat()
        {
            if (FormatEntryTextBox.Text.Trim().Length > 0)
            {
                string fmt = FormatEntryTextBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(_selectedClueKey))
                {
                    if (ClueContent.GoodFormatSpecification(fmt, _puzzle.ClueOf(_selectedClueKey).WordLength))
                    {
                        _puzzle.ClueOf(_selectedClueKey).Content.Format = fmt;
                        DisplayGrid();
                        ShowClueDetails(_selectedClueKey);
                        FormatApplyButton.IsEnabled = false;
                    }
                    else
                    {
                        MessageBox.Show("Not a valid format for this clue length", "Crosswords", MessageBoxButton.OK
                            , MessageBoxImage.Information);
                    }
                }
            }
        }

        private void FormatEntryTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (FormatEntryTextBox.Text.Trim().Length > 0)
            {
                string fmt = FormatEntryTextBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(_selectedClueKey))
                {
                    if (ClueContent.GoodFormatSpecification(fmt, _puzzle.ClueOf(_selectedClueKey).WordLength))
                    {
                        FormatApplyButton.IsEnabled = true;
                        FormatConflictWarningTextBlock.Text = string.Empty;
                        FormatApplyButton.IsDefault = true;
                    }
                    else
                    {
                        FormatApplyButton.IsEnabled = false;
                        FormatConflictWarningTextBlock.Text = "Not a valid format for this clue length";
                    }
                }
            }
            else
            {
                FormatApplyButton.IsEnabled = false;
            }
        }

        private void TemplateTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            TemplateListBox.Items.Clear();
            TemplateCountBlock.Text = string.Empty;
            MakeUpperTextBoxText(TemplateTextBox);

            _disableCheckBoxesTrigger = true;
            CapitalsCheckBox.IsChecked = false;
            ReversibleCheckBox.IsChecked = false;
            _disableCheckBoxesTrigger = false;
        }

        private void CheckVocabButton_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            SolidColorBrush dimbrush = Brushes.Silver;
            SolidColorBrush abrush = Brushes.RoyalBlue;
            SolidColorBrush dbrush = Brushes.DarkViolet;

            ClueAListBox.Items.Clear();
            ClueDListBox.Items.Clear();

            SolidColorBrush pinceau = abrush;
            List<Clue> clist = _puzzle.CluesAcross;
            TextBlock blk = new TextBlock();
            Run r = new Run() {Text = $"ACROSS: ", FontWeight = FontWeights.Bold, Foreground = pinceau};
            blk.Inlines.Add(r);
            r = new Run() {Text = $"{clist.Count} clues", Foreground = pinceau};
            blk.Inlines.Add(r);
            ListBoxItem itm = new ListBoxItem() {Content = blk, IsHitTestVisible = false};
            ClueAListBox.Items.Add(itm);

            foreach (Clue clu in clist)
            {
                pinceau = dimbrush;
                if (!clu.IsComplete())
                {
                    pinceau = abrush;
                }

                StackPanel spl = new StackPanel() {Orientation = Orientation.Horizontal};

                blk = new TextBlock()
                    {Width = 80, Text = clu.Number.ToString(), FontWeight = FontWeights.Medium, Foreground = pinceau};
                spl.Children.Add(blk);

                if (clu.Content.Format.Length == 0)
                {
                    clu.Content.Format = $"{clu.WordLength}";
                }

                blk = new TextBlock() {Text = $" ({clu.Content.Format})", Foreground = pinceau, Width = 80};
                spl.Children.Add(blk);

                string wd = _puzzle.PatternedWordConstrained(clu.Key);
                Connu known = new Connu();
                bool whether =known.FoundInWordList(wd);
                
                Brush brosse = whether ? Brushes.DarkGreen : Brushes.Red;

                blk = new TextBlock()
                    {FontFamily = _fixedFont, Foreground = brosse, Text = wd, Padding = new Thickness(0, 3, 0, 0)};
                spl.Children.Add(blk);

                itm = new ListBoxItem() {Content = spl, Tag = clu.Key};
                ClueAListBox.Items.Add(itm);
            }

            pinceau = dbrush;
            clist = _puzzle.CluesDown;
            blk = new TextBlock();
            r = new Run() {Text = $"DOWN: ", FontWeight = FontWeights.Bold, Foreground = pinceau};
            blk.Inlines.Add(r);
            r = new Run() {Text = $"{clist.Count} clues", Foreground = pinceau};
            blk.Inlines.Add(r);
            itm = new ListBoxItem() {Content = blk, IsHitTestVisible = false};
            ClueDListBox.Items.Add(itm);

            foreach (Clue clu in clist)
            {
                pinceau = dimbrush;
                if (!clu.IsComplete())
                {
                    pinceau = dbrush;
                }

                StackPanel spl = new StackPanel() {Orientation = Orientation.Horizontal};

                blk = new TextBlock()
                    {Width = 80, Text = clu.Number.ToString(), FontWeight = FontWeights.Medium, Foreground = pinceau};
                spl.Children.Add(blk);

                if (clu.Content.Format.Length == 0)
                {
                    clu.Content.Format = $"{clu.WordLength}";
                }

                blk = new TextBlock() {Text = $" ({clu.Content.Format})", Foreground = pinceau, Width = 80};
                spl.Children.Add(blk);

                string wd = _puzzle.PatternedWordConstrained(clu.Key);
                Connu known = new Connu();
                bool whether =known.FoundInWordList(wd);
                Brush brosse = whether ? Brushes.DarkGreen : Brushes.Red;

                blk = new TextBlock()
                    {FontFamily = _fixedFont, Foreground = brosse, Text = wd, Padding = new Thickness(0, 3, 0, 0)};
                spl.Children.Add(blk);

                itm = new ListBoxItem() {Content = spl, Tag = clu.Key};
                ClueDListBox.Items.Add(itm);
            }

            Cursor = Cursors.Arrow;
        }
       
        // private void ColourComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        // {
        //     if (ColourComboBox.SelectedItem is ListBoxItem {Tag: Brush pinceau})
        //     {
        //         _barBrush = pinceau;
        //         ClueTitleTextBlock.Foreground = pinceau;
        //     }
        // }

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
                    GetAnagrams();
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

        private void Capitals_Toggled(object sender, RoutedEventArgs e)
        {
            if (_disableCheckBoxesTrigger)
            {
                return;
            }
            CountTemplateMatches();
        }

        private void PointersButton_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new PointersWindow() {Owner = this};
            win.ShowDialog();
        }

        private void ExtraLettersTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            MakeUpperTextBoxText(ExtraLettersTextBox);
        }

        private void TemplateListEachButton_OnClick(object sender, RoutedEventArgs e)
        {
            Cursor= Cursors.Wait;
            ListEachButton.IsEnabled = false;
            TemplateListBox.Items.Clear();
            DisplayTemplateMatchesIndividualWordsList();
            Cursor = Cursors.Arrow;
        }
    }
}
    
