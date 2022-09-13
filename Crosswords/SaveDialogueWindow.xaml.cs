using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Crosswords;

public partial class SaveDialogueWindow
{
    public SaveDialogueWindow()
    {
        InitializeComponent();
    }

    private static string CrosswordsPath => Path.Combine(Jbh.AppManager.DataPath, "Crosswords");
    private readonly List<string> _bookNames = new();
    private List<string> _puzzleFiles = new();

    private void SaveDialogueWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        _bookNames.Clear();
        string[] gameFiles = Directory.GetFiles(CrosswordsPath, "*.cwd");
        _puzzleFiles = gameFiles.ToList();
        foreach (var gameFile in _puzzleFiles)
        {
            bool ok = Interpret(gameFile, out string livre, out int _);
            if (ok)
            {
                if (!_bookNames.Contains(livre))
                {
                    _bookNames.Add(livre);
                }
            }
        }

        _bookNames.Sort();
        BooksCombo.Items.Clear();
        foreach (var bookName in _bookNames)
        {
            BooksCombo.Items.Add(new ComboBoxItem() {Content = bookName});
        }

        SelectBookButton.IsEnabled = NewBookTitleButton.IsEnabled = false;
    }

    private bool Interpret(string filePath, out string book, out int puzzle)
    {
        puzzle = 0;
        book = string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var lastHyphen = fileName.LastIndexOf('-');

        if (lastHyphen >= 0)
        {
            var numero = fileName.Substring(lastHyphen + 1);
            if (int.TryParse(numero, out var g)) // game number is included in file name as expected
            {
                puzzle = g;
                book = fileName.Substring(0, lastHyphen);
                return true;
            }
        }

        return false;
    }

    private List<int> PuzzlesForBook(string title)
    {
        List<int> already = new();
        foreach (var gameFile in _puzzleFiles)
        {
            Interpret(gameFile, out string volume, out int game);
            if (volume == title)
            {
                already.Add(game);
            }
        }

        already.Sort();
        return already;
    }

    public string PuzzleFileSpecification()
    {
        string bk = BookTitleBlock.Text;
        
        if (string.IsNullOrWhiteSpace(bk))
        {
            return string.Empty;
        }

        if (!int.TryParse(PuzzleNumberBox.Text, out var p))
        {
            return string.Empty;
        }

        var pFile = $"{bk}-{p}.cwd";
        return Path.Combine(CrosswordsPath, pFile);
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PuzzleFileSpecification()))
        {
            MessageBox.Show("Select valid book and puzzle number", Jbh.AppManager.AppName, MessageBoxButton.OK
                , MessageBoxImage.Exclamation);
            return;
        }

        DialogResult = true;
    }

    private void SelectBookButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (BooksCombo.SelectedItem is ComboBoxItem {Content: string book})
        {
            BookTitleBlock.Text = book;
            ShowExistingPuzzleNumbers(book);
        }
    }

    private void BooksCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectBookButton.IsEnabled = BooksCombo.SelectedIndex >= 0;
    }

    private void NewBookTitleTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        NewBookTitleButton.IsEnabled = !string.IsNullOrWhiteSpace(NewBookTitleTextBox.Text.Trim());
    }

    private void NewBookTitleButton_OnClick(object sender, RoutedEventArgs e)
    {
        BookTitleBlock.Text = NewBookTitleTextBox.Text.Trim();
        ShowExistingPuzzleNumbers(NewBookTitleTextBox.Text.Trim());
    }

    private void ShowExistingPuzzleNumbers(string vol)
    {
        ExistingPuzzleNumbersTextBlock.Text = string.Empty;
        var list = PuzzlesForBook(vol);
        ExistingPuzzleNumbersTextBlock.Text = NumbersReport(list);
        PuzzleNumberBox.Text =$"{FirstFree(list)}";
    }
    
    private string NumbersReport(List<int> set)
    {
        string report = string.Empty;
        int previous = -11267;
        bool followingOn = false;
        foreach (var i in set)
        {
            followingOn = i == previous + 1;
            if (!followingOn)
            {
                report +=previous==-11267 ? $"{i}":$"-{previous}, {i}" ;
            }
            previous = i;
        }

        if (followingOn)
        {
            report += $"-{previous}"; // final item
        }
        return report;
    }
    private int FirstFree(List<int> set)
    {
        if (set.Count < 1)
        {
            return 1;
        }
        var vide = -1;
        var top = set.Max();
        for (var a = 1; a < top; a++)
        {
            if (set.Contains(a)) continue;
            vide = a;
            break;
        }

        if (vide == -1)
        {
            vide = top + 1;
        }

        return vide;
    }
    
}