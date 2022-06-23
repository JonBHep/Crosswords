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
            string defaultSpecification
                = "15......#........#.#.#.#.#.#.#.#........#......#.#.#.#.#.#.#.####............#.#.#.#.#.###.#....###........#.#.#.#.#.#.#.#........###....#.###.#.#.#.#.#............####.#.#.#.#.#.#.#......#........#.#.#.#.#.#.#.#........#......";
            _puzzle = new CrosswordGrid(defaultSpecification);
        }

        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private readonly double _squareSide = 36;
        private readonly double _letterWidth = 30;
        private readonly FontFamily _fixedFont = new("Liberation Mono");
        private CrosswordGrid _puzzle;
        private string _xwordTitle = "default";
        private Brush _barBrush = Brushes.DarkBlue;

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
            SwitchClueControls(false);
            FillGamesComboBox();
            FillColourComboBox();
        }

        private void SwitchClueControls(bool on)
        {
            Visibility vis = (on) ? Visibility.Visible : Visibility.Hidden;
            SelectedClueGrid.Visibility = vis;

            if (on) LettersEntryTextBox.Focus();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DisplayGrid();
            NameTextBlock.Text = "Choose Open or New";
            SwitchClueControls(false);
            VocabButton.IsEnabled = false;
        }

        private void DisplayGrid()
        {
            // Constructing a rectangular Grid with rows and columns
            // Each cell contains a Canvas enclosed in a Border
            // Indices are inserted in the cell Canvas as a TextBlock
            // Bars and hyphens are added directly to the Grid cells not to the Canvases - they are sourced from Clue.PatternedWord

            Canvas[,] cellCanvas = new Canvas[_puzzle.Width, _puzzle.Height];

            double gapSize = 2;
            FontFamily ff = new FontFamily("Times New Roman");

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
                        cellCanvas[x, y].Background = Brushes.Black;
                    }
                    else
                    {
                        cellCanvas[x, y].Background = Brushes.White;
                        int i = _puzzle.Index(x, y);
                        if (i > 0)
                        {
                            TextBlock indexBlock = new TextBlock()
                                {FontSize = 8, Text = i.ToString(), Margin = new Thickness(2, 0, 0, 0)};
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
                                    , Foreground =_barBrush, Width = _letterWidth, TextAlignment = TextAlignment.Center
                                };
                                Canvas.SetLeft(letterBlock, 2); 
                                Canvas.SetTop(letterBlock, 6);
                                cellCanvas[px, py].Children.Add(letterBlock);
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
                                // TextBlock letterBlock = new TextBlock()
                                // {
                                //     FontFamily = ff, FontSize = 22, Text = t.ToString(), FontWeight = FontWeights.Bold
                                //     , Foreground = _barBrush
                                // };
                                // Canvas.SetLeft(letterBlock, 9);
                                // Canvas.SetTop(letterBlock, 6);
                                // cellCanvas[px, py].Children.Add(letterBlock);
                                
                                
                                TextBlock letterBlock = new TextBlock()
                                {
                                    FontFamily = ff, FontSize = 22, Text = t.ToString(), FontWeight = FontWeights.Bold
                                    , Foreground =_barBrush, Width = _letterWidth, TextAlignment = TextAlignment.Center
                                };
                                Canvas.SetLeft(letterBlock, 2); 
                                Canvas.SetTop(letterBlock, 6);
                                cellCanvas[px, py].Children.Add(letterBlock);
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

                // if (_puzzle.IsLocked)
                // {
                // highlight clue
                string t = string.Empty;
                ClueAListBox.SelectedIndex = -1;
                ClueDListBox.SelectedIndex = -1;
                foreach (string s in _puzzle.ClueKeyList)
                {
                    if (_puzzle.ClueOf(s).IncludesCell(locus) is not null)
                    {
                        t = s;
                        break;
                    }
                }

                int r = -1;
                for (int z = 1; z < ClueAListBox.Items.Count; z++) // don't take first item which is the heading
                {
                    ListBoxItem itm = (ListBoxItem) ClueAListBox.Items[z];
                    string? k = itm.Tag.ToString();
                    if (k == t)
                    {
                        r = z;
                        break;
                    }
                }

                if (r >= 0)
                {
                    ClueAListBox.SelectedIndex = r;
                    return;
                }

                for (int z = 1; z < ClueDListBox.Items.Count; z++) // don't take first item which is the heading
                {
                    ListBoxItem itm = (ListBoxItem) ClueDListBox.Items[z];
                    string? k = itm.Tag.ToString();
                    if (k == t)
                    {
                        r = z;
                        break;
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
            int px = point.X * 2;
            int py = point.Y * 2;
            Rectangle r = new Rectangle() {Fill = _barBrush};
            XwordGrid.Children.Add(r);
            Grid.SetColumn(r, px + 1);
            Grid.SetRow(r, py);
        }

        private void MakeRightDash(GridPoint point)
        {
            int px = point.X * 2;
            int py = point.Y * 2;
            Rectangle r = new Rectangle() {Fill = _barBrush, Height = 4};
            XwordGrid.Children.Add(r);
            Grid.SetColumn(r, px + 1);
            Grid.SetRow(r, py);
        }

        private void MakeBottomBar(GridPoint point)
        {
            int px = point.X * 2;
            int py = point.Y * 2;
            Rectangle r = new Rectangle() {Fill = _barBrush};
            XwordGrid.Children.Add(r);
            Grid.SetColumn(r, px);
            Grid.SetRow(r, py + 1);
        }

        private void MakeBottomDash(GridPoint point)
        {
            int px = point.X * 2;
            int py = point.Y * 2;
            Rectangle r = new Rectangle() {Fill = _barBrush, Width = 4};
            XwordGrid.Children.Add(r);
            Grid.SetColumn(r, px);
            Grid.SetRow(r, py + 1);
        }

        private void ListClues()
        {
            bool allDone = true;
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
                    allDone = false;
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
                blk = new TextBlock()
                    {FontFamily = _fixedFont, Foreground = pinceau, Text = wd, Padding = new Thickness(0, 3, 0, 0)};
                spl.Children.Add(blk);
                
                itm = new ListBoxItem() {Content =spl, Tag = clu.Key};
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
                    allDone = false;
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
                blk = new TextBlock()
                    {FontFamily = _fixedFont, Foreground = pinceau, Text = wd, Padding = new Thickness(0, 3, 0, 0)};
                spl.Children.Add(blk);
                itm = new ListBoxItem() {Content = spl, Tag = clu.Key};
                ClueDListBox.Items.Add(itm);
            }

            if (allDone)
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

            NameTextBlock.Text = _xwordTitle;
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
            string path = System.IO.Path.Combine(CrosswordsPath, _xwordTitle + ".cwd");
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

            _xwordTitle = System.IO.Path.GetFileNameWithoutExtension(puzzlePath);

            NameTextBlock.Text = _xwordTitle;

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
            if (ClueTitleTextBlock.Tag is string clef)
            {
                string nova = LettersEntryTextBox.Text;
                List<string> conflictingClues = _puzzle.CrossingConflictsDetected(clef, nova);

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
                    _puzzle.ClueOf(clef).Content.Letters = nova;
                    DisplayGrid();
                    SwitchClueControls(false);
                }
            }
        }
        private void ClueListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox {SelectedItem: ListBoxItem {Tag: string k}})
            {
                ShowClueDetails(k);
            }
        }

        private void ShowClueDetails(string clueCode)
        {
            Clue cloo = _puzzle.ClueOf(clueCode);
            string dirn = (cloo.Direction == 'A') ? "Across" : "Down";
            ClueTitleTextBlock.Text = $"{cloo.Number} {dirn}";
            ClueTitleTextBlock.Tag = clueCode;
            SwitchClueControls(true);
            FormatEntryTextBox.Text = cloo.Content.Format;
            FillCluePatternCombo(cloo.WordLength);
            PatternTextBox.Text = TemplateTextBox.Text = _puzzle.PatternedWordConstrained(clueCode);
            LettersConflictWarningTextBlock.Text = string.Empty;
            TemplateBlindMatchCount();
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
        
        private void ClueEntryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (ClueTitleTextBlock.Tag is string clef)
            {
                SolidColorBrush warning = Brushes.IndianRed;
                string pattern = _puzzle.UnPatternedWordConstrained(clef);
                string given = LettersEntryTextBox.Text.ToUpper();
                int p = LettersEntryTextBox.CaretIndex;
                LettersEntryTextBox.Text = given;
                LettersEntryTextBox.CaretIndex = p;
                char q = Matches(pattern, given);
                // X, L or A
                // X = wrong length or illegal character
                // L = letter conflicts with previously entered letter
                // A = OK
                if (q == 'X')
                {
                    LettersEntryTextBox.Foreground = Brushes.Red;
                    LettersApplyButton.IsEnabled = false; // impossible string (wrong length or illegal character)
                    LettersConflictWarningTextBlock.Visibility = Visibility.Visible;
                    LettersConflictWarningTextBlock.Text = "Wrong length or bad characters";
                }
                else if (q == 'L')
                {
                    LettersEntryTextBox.Foreground = warning; // allowable but conflicts with letters given in pattern
                    LettersApplyButton.IsEnabled = true;
                    LettersApplyButton.IsDefault = true;
                    LettersConflictWarningTextBlock.Visibility = Visibility.Visible;
                    LettersConflictWarningTextBlock.Text = "OK but conflicts with existing letters";
                }
                else
                {
                    LettersEntryTextBox.Foreground = Brushes.Black;
                    LettersApplyButton.IsEnabled = true;
                    LettersApplyButton.IsDefault = true;
                    LettersConflictWarningTextBlock.Visibility = Visibility.Hidden;
                }
            }
        }

        private char Matches(string patternString, string offeredString)
        {
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
            if (ClueTitleTextBlock.Tag is string clef)
            {
                Clue cloo = _puzzle.ClueOf(clef);
                cloo.Content.Letters = CrosswordWordTemplate.Stringy(cloo.WordLength, Clue.UnknownLetterChar);
                PatternTextBox.Text = _puzzle.PatternedWordConstrained(clef);
                DisplayGrid();
            }
        }

        private static string CrosswordsPath => System.IO.Path.Combine(Jbh.AppManager.DataPath, "Crosswords");

        private string WordListFile =>
            System.IO.Path.Combine(Jbh.AppManager.DataPath, "CrosswordLists", "wordlist.txt");

        private void AnagramButton_OnClick(object sender, RoutedEventArgs e)
        {
            GetAnagrams();
        }

        private void GetAnagrams()
        {
            Cursor = Cursors.Wait;
            AnagramBox.Items.Clear();
            string source = AnagramTextBox.Text.Trim();
            string ordered = CrosswordWordTemplate.AnagramString(source);
            using StreamReader reader = new(WordListFile, Clue.JbhEncoding);
            while (!reader.EndOfStream)
            {
                string? mot = reader.ReadLine();
                if (mot is { } word)
                {
                    string bien = CrosswordWordTemplate.AnagramString(word);
                    if (bien == ordered)
                    {
                        AnagramBox.Items.Add(word);
                    }
                }
            }

            int g = AnagramBox.Items.Count;
            AnagramCountBlock.Text = (g < 1) ? "No matches" : g > 1 ? $"{g:#,0} matches" : "1 match";
            Cursor = Cursors.Arrow;
        }
        private void TemplateButton_OnClick(object sender, RoutedEventArgs e)
        {
            GetTemplateMatches();
        }

        private void GetTemplateMatches()
        {
            // TODO For multiple-word clues find the words individually as well as in phrases
            Cursor = Cursors.Wait;
            TemplateBox.Items.Clear();
            string source = TemplateTextBox.Text;
            CrosswordWordTemplate template = new CrosswordWordTemplate(source);
            using StreamReader reader = new(WordListFile, Clue.JbhEncoding);
            while (!reader.EndOfStream)
            {
                string? mot = reader.ReadLine();
                if (mot is {} word)
                {
                    CrosswordWordTemplate wordTemplate = new CrosswordWordTemplate(word);
                    if (wordTemplate.MatchesTemplate(template))
                    {
                        TemplateBox.Items.Add(word);
                    }
                }
            }

            int g = TemplateBox.Items.Count;
            TemplateCountBlock.Text = (g < 1) ? "No matches" : g > 1 ? $"{g:#,0} matches" : "1 match";
            Cursor = Cursors.Arrow;
        }
        
        private void TemplateBlindMatchCount()
        {
            Cursor = Cursors.Wait;
            int counter = 0;
            TemplateBox.Items.Clear();
            string source = TemplateTextBox.Text;
            CrosswordWordTemplate template = new CrosswordWordTemplate(source);
            using StreamReader reader = new(WordListFile, Clue.JbhEncoding);
            while (!reader.EndOfStream && counter < 1000)
            {
                string? mot = reader.ReadLine();
                if (mot is { } word)
                {
                    CrosswordWordTemplate wordTemplate = new CrosswordWordTemplate(word);
                    if (wordTemplate.MatchesTemplate(template))
                    {
                        counter++;
                    }
                }
            }

            TemplateCountBlock.Text = (counter > 999) ? "1,000+ matches" :
                (counter < 1) ? "No matches" :
                counter > 1 ? $"{counter:#,0} matches" : "1 match";
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
                if (SelectedClueGrid.IsVisible)
                {
                    LettersEntryTextBox.Text = Constrain(word);
                    AnagramTextBox.Clear();
                }
            }
        }

        private static string Constrain(string input)
        {
            StringBuilder builder = new StringBuilder();
            for (int x = 0; x < input.Length; x++)
            {
                char c = input[x];
                if (char.IsLetter(c))
                {
                    builder.Append(c);
                }
            }

            string output = builder.ToString();
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
                LoadPuzzleFromFile(path);
            }
        }

        private void FillColourComboBox()
        {
            ColourComboBox.Items.Clear();
            ColourComboBox.Items.Add(new ListBoxItem()
                {Tag = Brushes.Black, Content = new Rectangle() {Width = 128, Height = 16, Fill = Brushes.Black}});
            ColourComboBox.Items.Add(new ListBoxItem()
            {
                Tag = Brushes.RoyalBlue, Content = new Rectangle() {Width = 128, Height = 16, Fill = Brushes.RoyalBlue}
            });
            ColourComboBox.Items.Add(new ListBoxItem()
            {
                Tag = Brushes.SaddleBrown
                , Content = new Rectangle() {Width = 128, Height = 16, Fill = Brushes.SaddleBrown}
            });
            ColourComboBox.Items.Add(new ListBoxItem()
            {
                Tag = Brushes.ForestGreen
                , Content = new Rectangle() {Width = 128, Height = 16, Fill = Brushes.ForestGreen}
            });
            ColourComboBox.Items.Add(new ListBoxItem()
            {
                Tag = Brushes.DarkViolet
                , Content = new Rectangle() {Width = 128, Height = 16, Fill = Brushes.DarkViolet}
            });
            ColourComboBox.Items.Add(new ListBoxItem()
                {Tag = Brushes.Crimson, Content = new Rectangle() {Width = 128, Height = 16, Fill = Brushes.Crimson}});
            ColourComboBox.SelectedIndex = 0;
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
            AnagramBox.Items.Clear();
            AnagramCountBlock.Text = String.Empty;
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
                if (ClueTitleTextBlock.Tag is string clef)
                {
                    if (ClueContent.GoodFormatSpecification(fmt, _puzzle.ClueOf(clef).WordLength))
                    {
                        _puzzle.ClueOf(clef).Content.Format = fmt;
                        DisplayGrid();
                        ShowClueDetails(clef);
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
                if (ClueTitleTextBlock.Tag is string clef)
                {
                    if (ClueContent.GoodFormatSpecification(fmt, _puzzle.ClueOf(clef).WordLength))
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
            TemplateBox.Items.Clear();
            TemplateCountBlock.Text = string.Empty;
        }

        private void CheckVocabButton_Click(object sender, RoutedEventArgs e)
        {
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
                bool whether = FoundInWordList(wd);
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
                bool whether = FoundInWordList(wd);
                Brush brosse = whether ? Brushes.DarkGreen : Brushes.Red;

                blk = new TextBlock()
                    {FontFamily = _fixedFont, Foreground = brosse, Text = wd, Padding = new Thickness(0, 3, 0, 0)};
                spl.Children.Add(blk);

                itm = new ListBoxItem() {Content = spl, Tag = clu.Key};
                ClueDListBox.Items.Add(itm);
            }
        }

        private bool FoundInWordList(string verba)
        {
            CrosswordWordTemplate verbaTemplate = new CrosswordWordTemplate(verba);
            bool flag = false;
            using StreamReader reader = new(WordListFile, Clue.JbhEncoding);
            while (!reader.EndOfStream)
            {
                string? mot = reader.ReadLine();
                if (mot is { } word)
                {
                    CrosswordWordTemplate wordTemplate = new CrosswordWordTemplate(word);
                    if (wordTemplate.MatchesTemplate(verbaTemplate))
                    {
                        flag = true;
                        break;
                    }
                }
            }

            return flag;
        }

        private void ColourComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ColourComboBox.SelectedItem is ListBoxItem {Tag: Brush pinceau})
            {
                _barBrush = pinceau;
            }
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
                    GetAnagrams();
                }
                else if (TemplateTextBox.IsFocused)
                {
                    GetTemplateMatches();
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

        // private void TesterButton_OnClick(object sender, RoutedEventArgs e)
        // {
        //     string targetWord = "ship's boy";
        //     string hypothesisWord = "..... ...";
        //     CrosswordWordTemplate target = new CrosswordWordTemplate(targetWord);
        //     CrosswordWordTemplate hypothesis = new CrosswordWordTemplate( hypothesisWord);
        //     if (target.MatchesTemplate(hypothesis))
        //     {
        //         MessageBox.Show($"{targetWord} matches {hypothesisWord}");
        //     }
        //     else
        //     {
        //         MessageBox.Show($"{targetWord} does not match {hypothesisWord}");
        //     }
        // }
    }
    
}