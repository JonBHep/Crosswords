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
            // LockMessage.Visibility = Visibility.Hidden;
            FillGamesComboBox();
        }

        private void SwitchClueControls(bool on)
        {
            Visibility vis = (on) ? Visibility.Visible : Visibility.Hidden;
            FormatApplyButton.Visibility = vis;
            FormatCaptionTextBlock.Visibility = vis;
            FormatEntryTextBox.Visibility = vis;
            FormatConflictWarningTextBlock.Visibility = vis;

            ClueTitleTextBlock.Visibility = vis;
            CluePatternTextBox.Visibility = vis;
            ContentEntryTextBox.Visibility = vis;
            ContentApplyButton.Visibility = vis;
            ContentConflictWarningTextBlock.Visibility = vis;

            ClueCopyButton.Visibility = vis;
            ClueClearButton.Visibility = vis;
            ClueRubricATextBlock.Visibility = vis;
            ClueRubricBTextBlock.Visibility = vis;

            if (on) ContentEntryTextBox.Focus();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DisplayGrid();
            _loaded = true;
        }

        private void DisplayGrid()
        {
            // Constructing a rectangular Grid with rows and columns
            // Each cell contains a Canvas enclosed in a Border
            // Indices are inserted in the cell Canvas as a TextBlock
            // Bars and hyphens are added directly to the Grid cells not to the Canvases - they are sourced from Clue.PatternedWord
            // TODO Could also source letters from Clue.PatternedWord?

            Canvas[,] cellCanvas = new Canvas[_puzzle.Width, _puzzle.Height];


            SwitchClueControls(false);
            double gapSize = 2;
            FontFamily ff = new FontFamily("Times New Roman");
            Brush blackBrush = Brushes.Black;
            
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

                        // char l = _puzzle.Lettre(new GridPoint(x, y));
                        // if (l != CrosswordGrid.WhiteChar)
                        // {
                        //     TextBlock letterBlock = new TextBlock()
                        //         {FontFamily = ff, FontSize = 22, Text = l.ToString(), FontWeight = FontWeights.Bold};
                        //     Canvas.SetLeft(letterBlock, 9);
                        //     Canvas.SetTop(letterBlock, 6);
                        //     CellCanvas[x,y].Children.Add(letterBlock);
                        // }
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
                    foreach (var t in clu.PatternedWord)
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
                            if (t != CrosswordGrid.UnknownLetterChar)
                            {
                                TextBlock letterBlock = new TextBlock()
                                {
                                    FontFamily = ff, FontSize = 22, Text = t.ToString(), FontWeight = FontWeights.Bold
                                };
                                Canvas.SetLeft(letterBlock, 9);
                                Canvas.SetTop(letterBlock, 6);
                                cellCanvas[px, py].Children.Add(letterBlock);
                            }
                        }
                    }
                }
                else
                {
                    py--;
                    foreach (var t in clu.PatternedWord)
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
                            if (t != CrosswordGrid.UnknownLetterChar)
                            {
                                TextBlock letterBlock = new TextBlock()
                                {
                                    FontFamily = ff, FontSize = 22, Text = t.ToString(), FontWeight = FontWeights.Bold
                                };
                                Canvas.SetLeft(letterBlock, 9);
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
                //}
                // else
                // {
                //     // build grid structure
                //
                //     _puzzle.SetCell(locus
                //         , _puzzle.Cell(locus) == CrosswordGrid.BlackChar
                //             ? CrosswordGrid.WhiteChar
                //             : CrosswordGrid.BlackChar);
                //
                //     if (Symmetrical)
                //     {
                //         int symmX = _puzzle.Width - (locus.X + 1);
                //         int symmY = _puzzle.Height - (locus.Y + 1);
                //         GridPoint symmLocus = new GridPoint(symmX, symmY);
                //         _puzzle.SetCell(symmLocus, _puzzle.Cell(locus));
                //     }
                //
                //     DisplayGrid();
                // }
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

        // private bool Symmetrical => (SymmCheckBox.IsChecked.HasValue) && (SymmCheckBox.IsChecked.Value);

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
                if (!clu.IsComplete())
                {
                    pinceau = abrush;
                }

                StackPanel spl = new StackPanel() {Orientation = Orientation.Horizontal};
                blk = new TextBlock() {Width = 80};
                r = new Run() {Text = clu.Number.ToString(), FontWeight = FontWeights.Medium, Foreground = pinceau};
                blk.Inlines.Add(r);
                if (clu.Content.Format.Length == 0)
                {
                    clu.Content.Format = clu.WordLength.ToString();
                }
                r = new Run() {Text = $" {clu.Content.Format}", Foreground = pinceau};
                blk.Inlines.Add(r);
                spl.Children.Add(blk);
                string wd =clu.PatternedWord;
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
                if (!clu.IsComplete())
                {
                    pinceau = dbrush;
                }

                StackPanel spl = new StackPanel() {Orientation = Orientation.Horizontal};
                blk = new TextBlock() {Width = 80};
                r = new Run() {Text = clu.Number.ToString(), FontWeight = FontWeights.Medium, Foreground = pinceau};
                blk.Inlines.Add(r);
                r = new Run() {Text = $" {clu.Content.Format}", Foreground = pinceau};
                blk.Inlines.Add(r);
                spl.Children.Add(blk);
                string wd = clu.PatternedWord;
                blk = new TextBlock()
                    {FontFamily = _fixedFont, Foreground = pinceau, Text = wd, Padding = new Thickness(0, 3, 0, 0)};
                spl.Children.Add(blk);
                itm = new ListBoxItem() {Content = spl, Tag = clu.Key};
                ClueDListBox.Items.Add(itm);
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

            _xwordTitle = cwin.GridTitle;
            NameTextBlock.Text = _xwordTitle;
            _puzzle = new CrosswordGrid(cwin.GridDefinition);
            
            DisplayGrid();
            
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
                    if (_puzzle.Cell(locus) != CrosswordGrid.BlackChar)
                    { 
                        if (_puzzle.Cell(locus) != CrosswordGrid.WhiteChar)
                        {
                            _puzzle.SetCell(locus, CrosswordGrid.WhiteChar);
                        }
                    }
                }
            }

            DisplayGrid();
        }

        // private void SaveCrosswordAs()
        // {
        //     SaveFileDialog dlg = new SaveFileDialog()
        //     {
        //         AddExtension = true, DefaultExt = "cwd", Filter = "Crossword files (*.cwd)|*.cwd"
        //         , InitialDirectory = CrosswordsPath, OverwritePrompt = true, Title = "Save crossword as..."
        //         , ValidateNames = true
        //     };
        //     bool? ans = dlg.ShowDialog();
        //     if ((ans.HasValue) && (ans.Value))
        //     {
        //         FileStream fs = new FileStream(dlg.FileName, FileMode.Create);
        //         using (StreamWriter wri = new StreamWriter(fs, Clue.JbhEncoding))
        //         {
        //             wri.WriteLine(_puzzle.Specification);
        //         }
        //
        //         _xwordTitle = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
        //         
        //     }
        // }

        // private void SetName()
        // {
        //     NameTextBlock.Text = _xwordTitle;
        //     DefaultNameWarningTextBlock.Visibility = _xwordTitle == "default" ? Visibility.Visible : Visibility.Hidden;
        // }

        private void SaveCrossword()
        {
            string path = System.IO.Path.Combine(CrosswordsPath, _xwordTitle + ".cwd");
            FileStream fs = new FileStream(path, FileMode.Create);
            using StreamWriter wri = new StreamWriter(fs, Clue.JbhEncoding);
            wri.WriteLine(_puzzle.Specification);
            foreach (string tk in _puzzle.ClueKeyList)
            {
                wri.WriteLine($"{tk}%{_puzzle.ClueOf(tk).Content.Specification()}" );
            }
        }

        // private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        // {
        //     SaveCrosswordAs();
        // }

        private void LoadPuzzleFromFile(string puzzlePath)
        {
            SaveCrossword();
            
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
                            _puzzle.ClueOf(clef).AddContent(dat.Substring(pk+1));
                        }
                    }
                }
            }

            _xwordTitle = System.IO.Path.GetFileNameWithoutExtension(puzzlePath);
            
            NameTextBlock.Text = _xwordTitle;

            DisplayGrid();

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
            string nova = ContentEntryTextBox.Text;
            ContentEntryTextBox.Clear();
            if (ClueTitleTextBlock.Tag is string clef)
            {
                _puzzle.ClueOf(clef).Content.Letters = nova;
                bool accomplished = _puzzle.SuccessfullyApplyCrossings();
                if (!accomplished)
                {
                    MessageBox.Show("Conflicting letters in different clues", "Crosswords", MessageBoxButton.OK
                        , MessageBoxImage.Asterisk);
                }
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
                CluePatternTextBox.Text =cloo.PatternedWord;
               ContentConflictWarningTextBlock.Text = string.Empty;
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
            string given = ContentEntryTextBox.Text.ToUpper();
            int p = ContentEntryTextBox.CaretIndex;
            ContentEntryTextBox.Text = given;
            ContentEntryTextBox.CaretIndex = p;
            char q = Matches(pattern, given);
            // X, L or A
            // X = wrong length or illegal character
            // L = letter conflicts with previously entered letter
            // A = OK
            if (q == 'X')
            {
                ContentEntryTextBox.Foreground = Brushes.Red;
                ContentApplyButton.IsEnabled = false; // impossible string (wrong length or illegal character)
                ContentConflictWarningTextBlock.Visibility = Visibility.Visible;
                ContentConflictWarningTextBlock.Text = "Wrong length or bad characters";
            }
            else if (q == 'L')
            {
                ContentEntryTextBox.Foreground = warning; // allowable but conflicts with letters given in pattern
                ContentApplyButton.IsEnabled = true;
                ContentConflictWarningTextBlock.Visibility = Visibility.Visible;
                ContentConflictWarningTextBlock.Text = "OK but conflicts with existing letters";
            }
            else
            {
                ContentEntryTextBox.Foreground = Brushes.Black;
                ContentApplyButton.IsEnabled = true;
                ContentConflictWarningTextBlock.Visibility = Visibility.Hidden;
            }
        }

        private char Matches(string patternString, string offeredString)
        {
            // Does the entered string conform to the clue pattern (length and already entered letters)
            string patternWord = CrosswordGrid.NakedWord(patternString);
        
            // check offered word length against pattern
            if (patternWord.Length != offeredString.Length)
            {
                return 'X';
            }
        
            // check for invalid characters in offered string
            bool validflag = true;
            foreach (var u in offeredString)
            {
                if (!CrosswordGrid.IsLetterOrWhiteCell(u))
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
                char v = patternWord[p];
                if (Alphabet.IndexOf(u) >= 0) // it's a letter not an unknown cell
                {
                    if ((v != CrosswordGrid.UnknownLetterChar) && (v != u))
                    {
                        lettersflag = false;
                    } // a letter in the offered string is different from the pattern and the pattern's letter is not a wildcard
                }
            }
        
            if (!lettersflag)
            {
                return 'L';
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

        private static string CrosswordsPath => System.IO.Path.Combine(Jbh.AppManager.DataPath, "Crosswords");
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
                ContentEntryTextBox.Text = word;
                TemplateTextBox.Clear();
            }

        }

        private void AnagramBox_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox {SelectedItem: string word})
            {
                ContentEntryTextBox.Text = word;
                AnagramTextBox.Clear();
            }
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
                ComboBoxItem item = new ComboBoxItem
                {
                    Tag = history[x].Item2
                    , Content = new TextBlock()
                    {
                        FontFamily = new FontFamily("Lucida Console")
                        , Text = System.IO.Path.GetFileNameWithoutExtension(history[x].Item2)
                    }
                };
                GamesComboBox.Items.Add(item);
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
            AnagramButton.IsEnabled = AnagramTextBox.Text.Trim().Length > 0;
        }
        
        private void FormatApplyButton_OnClick(object sender, RoutedEventArgs e)
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
                        SwitchClueControls(false);    
                    }
                    else
                    {
                        MessageBox.Show("Not a valid format for this clue length", "Crosswords", MessageBoxButton.OK
                            , MessageBoxImage.Information);
                    }
                }
            }
        }

        // private void GridLockingButton_OnClick(object sender, RoutedEventArgs e)
        // {
        //     LockingPanel.Visibility = Visibility.Hidden;
        //     _puzzle.IsLocked = true;
        // }
    }
}