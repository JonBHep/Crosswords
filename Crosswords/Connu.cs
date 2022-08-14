using System;
using System.Collections.Generic;
using System.IO;

namespace Crosswords;

public class Connu
{

    public struct ListReport
    {
        public string StringReport { get; }
        public int WordCount { get; }

        public ListReport(string mistake, int quantity)
        {
            StringReport = mistake;
            WordCount = quantity;
        }
    }

    public Connu()
    {
        _filePath = Path.Combine(Jbh.AppManager.DataPath, "Lists", "wordlist.txt");
        _tempPath = Path.Combine(Jbh.AppManager.DataPath, "Lists", "tempcopy.txt");
    }

    private readonly string _filePath;
    private readonly string _tempPath;

    public string FilePath => _filePath;

    public ListReport SourceListHealth()
    {
        var counter = 0;
        var precedent = string.Empty;
        var precedentUnSpaced = string.Empty;
        var flaw = "No order errors";
        using (var reader = new StreamReader(_filePath, Clue.JbhEncoding))
        {
            while (!reader.EndOfStream)
            {
                var mot = reader.ReadLine();
                if (mot is { } word)
                {
                    counter++;
                    var wordUnSpaced = CrosswordWordTemplate.SortingString(word);
                    if (string.Compare(wordUnSpaced, precedentUnSpaced, StringComparison.CurrentCultureIgnoreCase) < 0)
                    {
                        flaw = $"{precedent} / {word}";
                    }

                    precedent = word;
                    precedentUnSpaced = wordUnSpaced;
                }
            }
        }

        return new ListReport(flaw, counter);
    }

    public ListReport SearchReport(string sought)
    {
        var counter = 0;
        var foundString = "Not found";
        using (var reader = new StreamReader(_filePath, Clue.JbhEncoding))
        {
            while (!reader.EndOfStream)
            {
                var mot = reader.ReadLine();
                if (mot is { } word)
                {
                    counter++;
                    if (string.Equals(word, sought, StringComparison.CurrentCultureIgnoreCase))
                    {
                        foundString = string.Equals(word, sought, StringComparison.CurrentCulture)
                            ? "Found exact string"
                            : "Found string differently cased";
                    }
                }
            }
        }

        return new ListReport(foundString, counter);
    }

    public  bool FoundInWordList(string verba)
    {
        var verbaTemplate = new CrosswordWordTemplate(verba);
        var flag = false;
        using StreamReader reader = new(_filePath, Clue.JbhEncoding);
        while (!reader.EndOfStream)
        {
            var mot = reader.ReadLine();
            if (mot is { } word)
            {
                var wordTemplate = new CrosswordWordTemplate(word);
                if (wordTemplate.MatchesTemplate(verbaTemplate))
                {
                    flag = true;
                    break;
                }
            }
        }

        return flag;
    }
    
    public int AddWord(string word)
    {
        var waiting = true;
        var counter = 0;
        var wordSorted = CrosswordWordTemplate.SortingString(word);
        using (var fs = new FileStream(_tempPath, FileMode.Create))
        {
            using (var writer = new StreamWriter(fs, Clue.JbhEncoding))
            {
                using (var reader = new StreamReader(_filePath, Clue.JbhEncoding))
                {
                    while (!reader.EndOfStream)
                    {
                        var existing = reader.ReadLine();
                        if (existing is null) continue;
                        var existingSorted = CrosswordWordTemplate.SortingString(existing);

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

        File.Delete(_filePath);
        File.Move(_tempPath, _filePath);
        return counter;
    }

    public List<string> GetAnagrams(string source)
    {
        List<string> anagrams = new();
        var ordered = CrosswordWordTemplate.AnagramString(source);
        using StreamReader reader = new(_filePath, Clue.JbhEncoding);
        while (!reader.EndOfStream)
        {
            var mot = reader.ReadLine();
            if (mot is { } word)
            {
                var bien = CrosswordWordTemplate.AnagramString(word);
                if (bien == ordered)
                {
                    anagrams.Add(word);
                }
            }
        }

        return anagrams;
    }
    
    public List<string> GetTemplateMatches(string pattern, bool onlyCapitalised, bool onlyReversibles, bool alsoUnSpaced, string extras)
    {
        var results = new List<string>();
        var template = new CrosswordWordTemplate(pattern);
        List<string> matches = new();
        using StreamReader reader = new(_filePath, Clue.JbhEncoding);
        while (!reader.EndOfStream)
        {
            var mot = reader.ReadLine();
            if (mot is { } word)
            {
                var wordTemplate = new CrosswordWordTemplate(word);
                if (wordTemplate.MatchesTemplateWithExtraCharsToBeIncluded(template, extras, alsoUnSpaced))
                {
                    if (char.IsUpper(word[0]) || !onlyCapitalised)
                    {
                        matches.Add(word);
                    }
                }
            }
        }
     
        if (onlyReversibles)
        {
            var retained = new List<string>();
            reader.BaseStream.Position = 0; // reset read position to start
            while (!reader.EndOfStream)
            {
                var mot = reader.ReadLine();
                if (mot is { })
                {
                    var back = ClueContent.Backwards(mot);
                    if (matches.Contains(back))
                    {
                        retained.Add(back);
                    }
                }
            }

            retained.Sort();
            foreach (var wd in retained)
            {
                results.Add(wd);
            }
        }
        else
        {
            foreach (var wd in matches)
            {
                results.Add(wd);
            }
        }

        return results;
    }

    
}
