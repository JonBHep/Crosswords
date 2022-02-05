using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly FontFamily _fixedFont = new FontFamily("Liberation Mono");
        private CrosswordGrid _puzzle;
        private bool _loaded;
        private string _xwordTitle = "default";
        private readonly SolidColorBrush _barBrush = Brushes.Black;

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
            LockMessage.Visibility = Visibility.Hidden;
        }

        private void SwitchClueControls(bool on)
        {
            Visibility vis = (on) ? Visibility.Visible : Visibility.Hidden;
            ClueTitleTextBlock.Visibility = vis;
            CluePatternTextBox.Visibility = vis;
            ClueEntryTextBox.Visibility = vis;
            ClueApplyButton.Visibility = vis;
            ClueCopyButton.Visibility = vis;
            ClueClearButton.Visibility = vis;
            ConflictWarningTextBlock.Visibility = vis;
            ConflictWarningTextBlock.Text = string.Empty;
            ClueRubricATextBlock.Visibility = vis;
            ClueRubricBTextBlock.Visibility = vis;
            if (on) ClueEntryTextBox.Focus();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DisplayGrid();
            _loaded = true;
        }

        private void DisplayGrid()
        {
            SwitchClueControls(false);
            double gapSize = 2;
            FontFamily ff = new FontFamily("Times New Roman");
            Brush blackBrush = (GridLocked) ? Brushes.Black : Brushes.Sienna;
            Visibility v = (GridLocked) ? Visibility.Visible : Visibility.Hidden;
            ClueAListBox.Visibility = v;
            ClueDListBox.Visibility = v;
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
                    Canvas c = new Canvas
                    {
                        Tag = Coords(x, y)
                    };
                    c.MouseDown += Cell_MouseDown;
                    if (_puzzle.Letter(new GridPoint(x, y)) == CrosswordGrid.BlackChar)
                    {
                        c.Background = blackBrush;
                    }
                    else
                    {
                        c.Background = Brushes.White;
                        int i = _puzzle.Index(x, y);
                        if (i > 0)
                        {
                            TextBlock indexBlock = new TextBlock() {FontSize = 8, Text = i.ToString()};
                            c.Children.Add(indexBlock);
                        }

                        char l = _puzzle.Letter(new GridPoint(x, y));
                        if (l != CrosswordGrid.WhiteChar)
                        {
                            TextBlock letterBlock = new TextBlock()
                                {FontFamily = ff, FontSize = 22, Text = l.ToString(), FontWeight = FontWeights.Bold};
                            Canvas.SetLeft(letterBlock, 9);
                            Canvas.SetTop(letterBlock, 6);
                            c.Children.Add(letterBlock);
                        }
                    }

                    Border b = new Border()
                        {BorderBrush = Brushes.DarkSlateGray, BorderThickness = new Thickness(1), Child = c};
                    Grid.SetColumn(b, x * 2);
                    Grid.SetRow(b, y * 2);
                    XwordGrid.Children.Add(b);
                }
            }

            ListClues();
            foreach (string q in _puzzle.Templates.Keys)
            {
                Clue clu = _puzzle.ClueOf(q);
                int px = clu.Xstart;
                int py = clu.Ystart;
                string plate = _puzzle.Templates[q];
                if (clu.Direction == 'A')
                {
                    px--;
                    foreach (var t in plate)
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
                        }
                    }
                }
                else
                {
                    py--;
                    foreach (var t in plate)
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

                if (GridLocked)
                {
                    // highlight clue
                    string t = string.Empty;
                    ClueAListBox.SelectedIndex = -1;
                    ClueDListBox.SelectedIndex = -1;
                    foreach (string s in _puzzle.ClueKeyList)
                    {
                        if (_puzzle.ClueOf(s).IncludesCell(locus))
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
                else
                {
                    // build grid structure

                    _puzzle.SetLetter(locus
                        , _puzzle.Letter(locus) == CrosswordGrid.BlackChar
                            ? CrosswordGrid.WhiteChar
                            : CrosswordGrid.BlackChar);

                    if (Symmetrical)
                    {
                        int symmX = _puzzle.Width - (locus.X + 1);
                        int symmY = _puzzle.Height - (locus.Y + 1);
                        GridPoint symmLocus = new GridPoint(symmX, symmY);
                        _puzzle.SetLetter(symmLocus, _puzzle.Letter(locus));
                    }

                    DisplayGrid();
                }
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

        private bool GridLocked => (LockCheckBox.IsChecked.HasValue) && (LockCheckBox.IsChecked.Value);

        private bool Symmetrical => (SymmCheckBox.IsChecked.HasValue) && (SymmCheckBox.IsChecked.Value);

        private void ListClues()
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
                if (clu.Word.Contains(CrosswordGrid.WhiteChar.ToString()))
                {
                    pinceau = abrush;
                }

                StackPanel spl = new StackPanel() {Orientation = Orientation.Horizontal};
                blk = new TextBlock() {Width = 80};
                r = new Run() {Text = clu.Number.ToString(), FontWeight = FontWeights.Medium, Foreground = pinceau};
                blk.Inlines.Add(r);
                r = new Run() {Text = $" {_puzzle.PatternedLength(clu.Key)}", Foreground = pinceau};
                blk.Inlines.Add(r);
                spl.Children.Add(blk);
                string wd = _puzzle.PatternedWord(clu.Key);
                blk = new TextBlock()
                    {FontFamily = _fixedFont, Foreground = pinceau, Text = wd, Padding = new Thickness(0, 3, 0, 0)};
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
                if (clu.Word.Contains(CrosswordGrid.WhiteChar.ToString()))
                {
                    pinceau = dbrush;
                }

                StackPanel spl = new StackPanel() {Orientation = Orientation.Horizontal};
                blk = new TextBlock() {Width = 80};
                r = new Run() {Text = clu.Number.ToString(), FontWeight = FontWeights.Medium, Foreground = pinceau};
                blk.Inlines.Add(r);
                r = new Run() {Text = $" {_puzzle.PatternedLength(clu.Key)}", Foreground = pinceau};
                blk.Inlines.Add(r);
                spl.Children.Add(blk);
                string wd = _puzzle.PatternedWord(clu.Key);
                blk = new TextBlock()
                    {FontFamily = _fixedFont, Foreground = pinceau, Text = wd, Padding = new Thickness(0, 3, 0, 0)};
                spl.Children.Add(blk);
                itm = new ListBoxItem() {Content = spl, Tag = clu.Key};
                ClueDListBox.Items.Add(itm);
            }
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            CrosswordSizeDialogue dw = new CrosswordSizeDialogue() {Owner = this};

            bool? q = dw.ShowDialog();
            if (!q.HasValue)
            {
                return;
            }

            if (!q.Value)
            {
                return;
            }

            int sq = dw.X_Dimension * dw.Y_Dimension;
            string init = dw.X_Dimension.ToString();
            init = init.PadRight(2);
            for (int z = 0; z < sq; z++)
            {
                init += CrosswordGrid.WhiteChar;
            }

            _puzzle = new CrosswordGrid(init);
            LockCheckBox.IsChecked = false;
            DisplayGrid();
            _xwordTitle = "default";
            SetName();
            SaveCrossword();
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

        private void LockCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!_loaded)
            {
                return;
            }

            SymmCheckBox.IsEnabled = false;
            SymmCheckBox.Opacity = .5;
            LockMessage.Visibility = Visibility.Hidden;
            DisplayGrid();
        }

        private void LockCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded)
            {
                return;
            }

            SymmCheckBox.IsEnabled = true;
            SymmCheckBox.Opacity = 1;
            LockMessage.Visibility = Visibility.Visible;
            DisplayGrid();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCrossword();
            Close();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult answ = MessageBox.Show("Clear all the letters?", Jbh.AppManager.AppName
                , MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (answ == MessageBoxResult.Cancel)
            {
                return;
            }

            for (int y = 0; y < _puzzle.Height; y++)
            {
                for (int x = 0; x < _puzzle.Width; x++)
                {
                    GridPoint locus = new GridPoint(x, y);
                    if (_puzzle.Letter(locus) != CrosswordGrid.BlackChar)
                    {
                        if (_puzzle.Letter(locus) != CrosswordGrid.WhiteChar)
                        {
                            _puzzle.SetLetter(locus, CrosswordGrid.WhiteChar);
                        }
                    }
                }
            }

            DisplayGrid();
        }

        private void SaveCrosswordAs()
        {
            SaveFileDialog dlg = new SaveFileDialog()
            {
                AddExtension = true, DefaultExt = "xwd", Filter = "Crossword files (*.xwd)|*.xwd"
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
                SetName();
            }
        }

        private void SetName()
        {
            NameTextBlock.Text = _xwordTitle;
            DefaultNameWarningTextBlock.Visibility = _xwordTitle == "default" ? Visibility.Visible : Visibility.Hidden;
        }

        private void SaveCrossword()
        {
            string path = System.IO.Path.Combine(CrosswordsPath, _xwordTitle + ".xwd");
            FileStream fs = new FileStream(path, FileMode.Create);
            using StreamWriter wri = new StreamWriter(fs, Clue.JbhEncoding);
            wri.WriteLine(_puzzle.Specification);
            foreach (string tk in _puzzle.Templates.Keys)
            {
                wri.WriteLine($"{tk}~{_puzzle.Templates[tk]}");
            }
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCrosswordAs();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCrossword();

            OpenFileDialog dlg = new OpenFileDialog()
            {
                Filter = "Crossword files (*.xwd)|*.xwd", InitialDirectory = CrosswordsPath, Title = "Open crossword"
            };
            bool? ans = dlg.ShowDialog();
            if ((ans.HasValue) && (ans.Value))
            {
                using (StreamReader rdr = new StreamReader(dlg.FileName, Clue.JbhEncoding))
                {
                    // load puzzle grid
                    string? read = rdr.ReadLine();
                    if (read is { } spec)
                    {
                        _puzzle = new CrosswordGrid(spec);
                        // load clue templates (for those clues that don't consist of a single word
                        while (!rdr.EndOfStream)
                        {
                            string? readData = rdr.ReadLine();
                            if (readData is { } dat)
                            {
                                string[] pars = dat.Split('~');
                                _puzzle.Templates.Add(pars[0], pars[1]);
                            }
                        }
                    }
                }

                _xwordTitle = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                SetName();

                DisplayGrid();
                LockCheckBox.IsChecked = true;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            string nova = ClueEntryTextBox.Text.Trim().ToUpper();
            ClueEntryTextBox.Clear();
            if (ClueTitleTextBlock.Tag is string clef)
            {
                _puzzle.AmendClue(clef, nova);
                DisplayGrid();
                SwitchClueControls(false);
            }
        }

        private void ClueListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox {SelectedItem: ListBoxItem {Tag: string k}})
            {
                Clue cloo = _puzzle.ClueOf(k);
                string dirn = (cloo.Direction == 'A') ? "Across" : "Down";
                ClueTitleTextBlock.Text = $"{cloo.Number} {dirn}";
                ClueTitleTextBlock.Tag = k;
                SwitchClueControls(true);
                CluePatternTextBox.Text = _puzzle.PatternedWord(cloo.Key);
                ConflictWarningTextBlock.Text = string.Empty;
            }
        }

        private void ClueEntryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_loaded)
            {
                return;
            }

            SolidColorBrush warning = Brushes.IndianRed;
            string pattern = CluePatternTextBox.Text;
            string given = ClueEntryTextBox.Text.ToUpper();
            int p = ClueEntryTextBox.CaretIndex;
            ClueEntryTextBox.Text = given;
            ClueEntryTextBox.CaretIndex = p;
            char q = Matches(pattern, given);
            if (q == 'X')
            {
                ClueEntryTextBox.Foreground = Brushes.Red;
                ClueApplyButton.IsEnabled = false; // impossible string (wrong length or illegal character)
                ConflictWarningTextBlock.Visibility = Visibility.Visible;
                ConflictWarningTextBlock.Text = "Wrong length or bad characters";
            }
            else if (q == '2')
            {
                ClueEntryTextBox.Foreground
                    = warning; // allowable but conflicts with letters and template given in  pattern
                ClueApplyButton.IsEnabled = true;
                ConflictWarningTextBlock.Visibility = Visibility.Visible;
                ConflictWarningTextBlock.Text = "OK but conflicts with existing letters and template";
            }
            else if (q == 'L')
            {
                ClueEntryTextBox.Foreground = warning; // allowable but conflicts with letters given in  pattern
                ClueApplyButton.IsEnabled = true;
                ConflictWarningTextBlock.Visibility = Visibility.Visible;
                ConflictWarningTextBlock.Text = "OK but conflicts with existing letters";
            }
            else if (q == 'T')
            {
                ClueEntryTextBox.Foreground = warning; // allowable but conflicts with template given in  pattern
                ClueApplyButton.IsEnabled = true;
                ConflictWarningTextBlock.Visibility = Visibility.Visible;
                ConflictWarningTextBlock.Text = "OK but conflicts with existing template";
            }
            else
            {
                ClueEntryTextBox.Foreground = Brushes.Black;
                ClueApplyButton.IsEnabled = true;
                ConflictWarningTextBlock.Visibility = Visibility.Hidden;
            }
        }

        private char Matches(string patternString, string offeredString)
        {
            string patternWord = CrosswordGrid.NakedWord(patternString);
            string offeredWord = CrosswordGrid.NakedWord(offeredString);
            string patternTemplate = CrosswordGrid.NakedTemplate(patternString);
            string offeredTemplate = CrosswordGrid.NakedTemplate(offeredString);

            // check offered word length against pattern
            if (patternWord.Length != offeredWord.Length)
            {
                return 'X';
            }

            // check for invalid characters in offered string
            bool validflag = true;
            foreach (var u in offeredString)
            {
                if (!CrosswordGrid.IsPermittedCharacter(u))
                {
                    validflag = false;
                }
            }

            if (!validflag)
            {
                return 'X';
            }

            bool templateflag = true;
            bool lettersflag = true;
            if (patternTemplate != offeredTemplate)
            {
                templateflag = false;
            }

            for (int p = 0; p < offeredWord.Length; p++)
            {
                char u = offeredWord[p];
                char v = patternWord[p];
                if (Alphabet.IndexOf(u) >= 0)
                {
                    if ((v != CrosswordGrid.WhiteChar) && (v != u))
                    {
                        lettersflag = false;
                    } // a letter in the offered string is different from the pattern and the pattern's letter is not a wildcard
                }

            }

            if ((!lettersflag) && (!templateflag))
            {
                return '2';
            }

            if (!lettersflag)
            {
                return 'L';
            }

            if (!templateflag)
            {
                return 'T';
            }

            return 'A';
        }


        // TODO Align letters better in grid e.g. 'I'

        private void ClueClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ClueTitleTextBlock.Tag is string clef)
            {
                _puzzle.ClearClue(clef);
                DisplayGrid();
                SwitchClueControls(false);
            }
        }
// TODO Easily load last / recent puzzle

        private string CrosswordsPath => System.IO.Path.Combine(Jbh.AppManager.DataPath, "Crosswords");
        private string WordListFile => System.IO.Path.Combine(Jbh.AppManager.DataPath, "CrosswordLists", "wordlist.txt");
        private void AnagramButton_OnClick(object sender, RoutedEventArgs e)
        {
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

            AnagramCountBlock.Text = $"{AnagramBox.Items.Count} matches";
        }

        private void TemplateButton_OnClick(object sender, RoutedEventArgs e)
        {
            TemplateBox.Items.Clear();
            string source = TemplateTextBox.Text;
            CrosswordWordTemplate template = new CrosswordWordTemplate(source);
            using StreamReader reader = new(WordListFile, Clue.JbhEncoding);
            while (!reader.EndOfStream)
            {
                string? mot = reader.ReadLine();
                if (mot is { } word)
                {
                    CrosswordWordTemplate wordTemplate = new CrosswordWordTemplate(word);
                    if (wordTemplate.MatchesTemplate(template))
                    {
                        TemplateBox.Items.Add(word);
                    }
                }
            }
            TemplateCountBlock.Text = $"{TemplateBox.Items.Count} matches";
        }

        private void ClueCopyButton_OnClick(object sender, RoutedEventArgs e)
        {
            string pattern = CluePatternTextBox.Text.Trim();
            TemplateTextBox.Text = pattern;
            TemplateBox.Items.Clear();
            TemplateCountBlock.Text = $"{TemplateBox.Items.Count} matches";
        }

        private void TemplateBox_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox {SelectedItem: string word})
            {
                ClueEntryTextBox.Text = word;
                TemplateTextBox.Clear();
            }

        }

        private void AnagramBox_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox {SelectedItem: string word})
            {
                ClueEntryTextBox.Text = word;
                AnagramTextBox.Clear();
            }
        }

        private void ListButton_OnClick(object sender, RoutedEventArgs e)
        {
            WordListWindow wlw = new() {Owner = this};
            wlw.ShowDialog();
        }
    }
}