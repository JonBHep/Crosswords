using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Crosswords;

public partial class WordListWindow
{
    public WordListWindow()
    {
        InitializeComponent();
        _filePath = Path.Combine(Jbh.AppManager.DataPath, "Lists", "wordlist.txt");
        _tempPath = Path.Combine(Jbh.AppManager.DataPath, "Lists", "tempcopy.txt");
    }

    private readonly string _filePath;
    private readonly string _tempPath;

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void WordListWindow_OnContentRendered(object? sender, EventArgs e)
    {
        PathTextBlock.Text = _filePath;
        SizeTextBlock.Text = "Not yet counted";
        AddButton.IsEnabled = false;
        FindButton.IsEnabled = false;
        OrderTextBlock.Text = string.Empty;
        FindTextBox.Focus();
    }

    private void OrderButton_OnClick(object sender, RoutedEventArgs e)
    {
        int counter = 0;
        string precedent = string.Empty;
        string precedentUnSpaced = string.Empty;
        string flaw = "No order errors";
        using (StreamReader reader = new StreamReader(_filePath, Clue.JbhEncoding))
        {
            while (!reader.EndOfStream)
            {
                string? mot = reader.ReadLine();
                if (mot is { } word)
                {
                    counter++;
                    string wordUnSpaced = CrosswordWordTemplate.SortingString(word);
                    if (string.Compare(wordUnSpaced, precedentUnSpaced, StringComparison.CurrentCultureIgnoreCase) <0)
                    {
                        flaw = $"{precedent} / {word}";
                    }

                    precedent = word;
                    precedentUnSpaced = wordUnSpaced;
                }
            }
        }

        OrderTextBlock.Text = flaw;
        SizeTextBlock.Text = $"{counter:#,0} words";
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        Cursor=Cursors.Wait;
        
        if (AddButton.Tag is string word)
        {
            AddButton.IsEnabled = false;
            bool waiting = true;
            int counter = 0;
            string wordSorted = CrosswordWordTemplate.SortingString(word);
            using (FileStream fs = new FileStream(_tempPath, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(fs, Clue.JbhEncoding))
                {
                    using (StreamReader reader = new StreamReader(_filePath, Clue.JbhEncoding))
                    {
                        while (!reader.EndOfStream)
                        {
                            string? existing = reader.ReadLine();
                            if (existing is { } mot)
                            {
                                string existingSorted = CrosswordWordTemplate.SortingString(existing);
                                
                                if (waiting && (string.Compare(existingSorted, wordSorted
                                        , StringComparison.CurrentCultureIgnoreCase) > 0))
                                {
                                    writer.WriteLine(word);
                                    waiting = false;
                                    counter++;
                                }
                                writer.WriteLine(existing);
                                counter++;
                            }
                        }
                    }
                }
            }
            File.Delete(_filePath);
            File.Move(_tempPath, _filePath);
            SizeTextBlock.Text = $"{counter:#,0} words";
            FindResultTextBlock.Text = "Added";
            Cursor=Cursors.Arrow;
        }
    }

    private void FindTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        FindButton.IsEnabled = (FindTextBox.Text.Trim().Length > 0);
    }

    private void FindButton_OnClick(object sender, RoutedEventArgs e)
    {
        int counter = 0;
        string foundString = "Not found";
        string cherchee = FindTextBox.Text.Trim(); 
        using (StreamReader reader = new StreamReader(_filePath, Clue.JbhEncoding))
        {
            while (!reader.EndOfStream)
            {
                string? mot = reader.ReadLine();
                if (mot is { } word)
                {
                    counter++;
                    if (string.Equals(word, cherchee, StringComparison.CurrentCultureIgnoreCase))
                    {
                        foundString = string.Equals(word, cherchee, StringComparison.CurrentCulture) ? "Found exact string" : "Found string differently cased";
                    }
                }
            }
        }
        FindResultTextBlock.Text = foundString;
        AddButton.IsEnabled = (foundString == "Not found");
        AddButton.Tag = cherchee;
        SizeTextBlock.Text = $"{counter:#,0} words";
    }
}