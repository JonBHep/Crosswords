using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Crosswords;

public partial class PointersWindow
{
    public PointersWindow()
    {
        InitializeComponent();
        _filePath = Path.Combine(Jbh.AppManager.DataPath, "CrosswordLists", "pointers.txt");
    }

    private readonly string _filePath;
    private readonly List<string> _pointerList = new();
    private readonly FontFamily _fontFamily = new FontFamily("Consolas");
    private void PointersWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        var scrX = SystemParameters.PrimaryScreenWidth;
        var scrY = SystemParameters.PrimaryScreenHeight;
        var winX = scrX * .67;
        var winY = scrY * .67;
        var xm = (scrX - winX) / 2;
        var ym = (scrY - winY) / 4;
        Width = winX;
        Height = winY;
        Left = xm;
        Top = ym;
    }

    private void PointersWindow_OnContentRendered(object? sender, EventArgs e)
    {
        LoadData();
        RefreshList();
    }

    private void LoadData()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        _pointerList.Clear();
        using var reader = new StreamReader(_filePath);
        while (!reader.EndOfStream)
        {
            var l = reader.ReadLine();
            if (l is { })
            {
                _pointerList.Add(l);
            }
        }
    }
    
    private void SaveData()
    {
        using var writer = new StreamWriter(_filePath);
        foreach (var p in _pointerList)
        {
            writer.WriteLine(p);
        }
    }

    private void PointersWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        SaveData();
    }

    private void RefreshList()
    {
        PointerListBox.Items.Clear();
        TargetListBox.Items.Clear();
        _pointerList.Sort();
        var uniques = UniquePointers();
        foreach (var v in uniques)
        {
            PointerListBox.Items.Add(new ListBoxItem()
                    {Tag = v, Content = new TextBlock() {Text = v, FontFamily = _fontFamily, FontSize = 14}});
        }

        PointerTextBox.Focus();
    }

    private List<string> UniquePointers()
    {
        var list = new List<string>();
        foreach (var v in _pointerList)
        {
            var clue = FirstOf(v);
            if (clue is { })
            {
                if (!list.Contains(clue))
                {
                    list.Add(clue);
                }
            }
        }

        return list;
    }

    private static string? FirstOf(string indice)
    {
        if (indice.Length == 64)
        {
            var clue = indice[..32];
            return clue.Trim();
        }

        return null;
    }
    private static string? SecondOf(string indice)
    {
        if (indice.Length == 64)
        {
            var clue = indice[32..];
            return clue.Trim();
        }

        return null;
    }
    
    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        var p = PointerTextBox.Text.Trim().ToLower(CultureInfo.CurrentCulture);
        var q = IndicationTextBox.Text.Trim().ToLower(CultureInfo.CurrentCulture);
        if (string.IsNullOrWhiteSpace(p)){return;}
        if (string.IsNullOrWhiteSpace(q)){return;}
        PointerTextBox.Clear();
        IndicationTextBox.Clear();
        var pp = p.PadRight(32);
        var qq = q.PadRight(32);
        var whole = $"{pp}{qq}";
        if (!_pointerList.Contains(whole))
        {
            _pointerList.Add(whole);
        }
        RefreshList();
    }

    private void PointerListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PointerListBox.SelectedItem is ListBoxItem {Tag: string what})
        {
            TargetListBox.Items.Clear();
            TargetListBox.Items.Add(new ListBoxItem()
                {IsHitTestVisible =false, Content = new TextBlock() {Text = what.ToUpper(CultureInfo.CurrentCulture), FontFamily = _fontFamily, FontSize = 16, Foreground = Brushes.MediumBlue, FontWeight = FontWeights.Bold}});   
            foreach (var v in _pointerList)
            {
                var premier = FirstOf(v);
                if (premier is { } && premier == what)
                {
                    var signification = SecondOf(v);
                    if (signification is { })
                    {
                        TargetListBox.Items.Add(new ListBoxItem()
                            {Tag = v, Content = new TextBlock() {Text = signification.ToUpper(CultureInfo.CurrentCulture), 
                                FontFamily = _fontFamily, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.DarkOrchid}});            
                    }
                }
            }
                    
        }
    }
 
    private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TargetListBox.SelectedItem is ListBoxItem {Tag: string a})
        {
            if (_pointerList.Contains(a))
            {
                var b = FirstOf(a);
                var c = SecondOf(a);
                if (b is { } && c is { })
                {
                    MessageBoxResult result = MessageBox.Show($"Remove '{b} : {c.ToUpper(CultureInfo.CurrentCulture)}'?", "Delete pointer", MessageBoxButton.OKCancel
                        , MessageBoxImage.Question);
                    if (result == MessageBoxResult.OK)
                    {
                        _pointerList.Remove(a);
                        RefreshList();
                    }        
                }
            }
        } 
    }
}