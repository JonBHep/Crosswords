using System;
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
        _source = new Connu();
    }

    private Connu _source;

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
        FindTextBox.Focus();
    }

    private void OrderButton_OnClick(object sender, RoutedEventArgs e)
    {
        Connu.ListReport report =_source.SourceListHealth();
        OrderTextBlock.Text = report.StringReport;
        SizeTextBlock.Text = $"{report.WordCount:#,0} words";
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        Cursor=Cursors.Wait;
        
        if (AddButton.Tag is string word)
        {
            AddButton.IsEnabled = false;
            int newCount = _source.AddWord(word);
            SizeTextBlock.Text = $"{newCount:#,0} words";
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
        Connu.ListReport searchReport = _source.SearchReport(cherchee);
        
        FindResultTextBlock.Text =searchReport.StringReport;
        AddButton.IsEnabled = (searchReport.StringReport == "Not found");
        AddButton.Tag = cherchee;
        SizeTextBlock.Text = $"{searchReport.WordCount:#,0} words";
    }
    
}