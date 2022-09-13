using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Crosswords;

public partial class OpenDialogueWindow
{
    public OpenDialogueWindow()
    {
        InitializeComponent();
        _bookTitle = string.Empty;
    }

    private string _bookTitle;
    private int _puzzleNumber;
    
    private void OpenDialogueWindow_OnLoadedDialogueWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        _bookNames.Clear();
        var gameFiles = Directory.GetFiles(CrosswordsPath, "*.cwd");
        _puzzleFiles = gameFiles.ToList();
        foreach (var gameFile in _puzzleFiles)
        {
            var ok = Interpret(gameFile, out var livre, out var _);
            if (!ok) continue;
            if (!_bookNames.Contains(livre))
            {
                _bookNames.Add(livre);
            }
        }

        _bookNames.Sort();
        BooksCombo.Items.Clear();
        foreach (var bookName in _bookNames)
        {
            BooksCombo.Items.Add(new ComboBoxItem() {Content = bookName});
        }

        SelectedPuzzleTextBlock.Text = string.Empty;
        
    }
    
    private static string CrosswordsPath => Path.Combine(Jbh.AppManager.DataPath, "Crosswords");
    private readonly List<string> _bookNames = new();
    private List<string> _puzzleFiles = new();
   
    private static bool Interpret(string filePath, out string book, out int puzzle)
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
            Interpret(gameFile, out var volume, out var game);
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
        var bk = _bookTitle;
        var gm = _puzzleNumber;
        
        return PuzzleFileSpecification(bk, gm);
    }

    private string PuzzleFileSpecification(string volume, int game)
    {
        var bk = _bookTitle;
        if (string.IsNullOrWhiteSpace(volume))
        {
            return string.Empty;
        }

        if (_puzzleNumber<=0)
        {
            return string.Empty;
        }

        var pFile = $"{volume}-{game}.cwd";
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
        if (BooksCombo.SelectedItem is not ComboBoxItem {Content: string book}) return;
        _bookTitle = book;
        ShowExistingPuzzleNumbers(book);
    }

    private void BooksCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectBookButton.IsEnabled = BooksCombo.SelectedIndex >= 0;
        
        if (BooksCombo.SelectedItem is not ComboBoxItem {Content: string book}) return;
        _bookTitle = book;
        ShowExistingPuzzleNumbers(book);
    }

    private void ShowExistingPuzzleNumbers(string vol)
    {
        PuzzleCombo.Items.Clear();
        var list = PuzzlesForBook(vol);
        foreach (var i in list)
        {
            PuzzleCombo.Items.Add(new ComboBoxItem(){Tag = i, Content = new TextBlock(){Text = $"Puzzle {i}"}});
        }

        // SelectPuzzleButton.IsEnabled = false;
    }


    private void PuzzleCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //SelectPuzzleButton.IsEnabled = PuzzleCombo.SelectedIndex >= 0;      
        
        if (PuzzleCombo.SelectedItem is not ComboBoxItem {Tag: int game}) return;
        _puzzleNumber = game;
        var path = PuzzleFileSpecification();
        SelectedPuzzleTextBlock.Text =Path.GetFileNameWithoutExtension(path);
    }

    private void SelectPuzzleButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PuzzleCombo.SelectedItem is not ComboBoxItem {Tag: int game}) return;
        _puzzleNumber = game;
        var path = PuzzleFileSpecification();
        SelectedPuzzleTextBlock.Text =Path.GetFileNameWithoutExtension(path);
    }
    
}