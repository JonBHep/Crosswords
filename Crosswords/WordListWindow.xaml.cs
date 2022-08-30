using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Crosswords;

public partial class WordListWindow
{
    public WordListWindow(string target, Connu dic)
    {
        InitializeComponent();
        _source =dic;
        _sought = target;
    }

    private Connu _source;
    private string _sought;

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void WordListWindow_OnContentRendered(object? sender, EventArgs e)
    {
        PathTextBlock.Text = _source.FilePath;
        SizeTextBlock.Text = "Not yet counted";
        AddButton.IsEnabled = false;
        FindButton.IsEnabled = false;
        OrderTextBlock.Text = string.Empty;
        FindTextBox.Text = _sought.ToLowerInvariant();
        FindTextBox.Focus();
    }

    private void OrderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var report =_source.SourceListHealth();
        OrderTextBlock.Text = report;
        SizeTextBlock.Text =$"{_source.LexiconCount():#,0} entries";
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        Cursor=Cursors.Wait;
        
        if (AddButton.Tag is string word)
        {
            AddButton.IsEnabled = false;
            _source.AddWord(word);
            SizeTextBlock.Text = $"{_source.LexiconCount():#,#} words";
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
        var cherchee = FindTextBox.Text.Trim();
        var searchReport = _source.SearchReport(cherchee);
        
        FindResultTextBlock.Text =searchReport;
        AddButton.IsEnabled = (searchReport == "Not found");
        AddButton.Tag = cherchee;
        SizeTextBlock.Text = $"{_source.LexiconCount():#,#} words";
    }
    
}