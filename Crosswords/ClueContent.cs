using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crosswords;

public class ClueContent
{
    public static bool GoodFormatSpecification(string q, int total)
    {
        bool ok = true;
        char[] acceptableChars = "0123456789,-".ToCharArray();
        for (int n = 0; n < q.Length; n++)
        {
            if (!acceptableChars.Contains(q[n]))
            {
                ok = false;
                break;
            }
        }

        if (!ok)
        {
            return false; // contains unacceptable character
        }

        string[] parts = q.Split("-,".ToCharArray());
        int added = 0;
        foreach (var part in parts)
        {
            if (int.TryParse(part, out int i))
            {
                if (i < 1)
                {
                    ok = false; // zero length element
                }
                else
                {
                    added += i;
                }
            }
            else
            {
                ok = false; // length not an integer
            }
        }
        if (!ok)
        {
            return false; // contains invalid length specifier
        }
        return added==total;
    }

    public static List<string> FormatList(string q)
    {
        List<string> r = new();
        StringBuilder builder = new StringBuilder("#");

        for (int p = 0; p < q.Length; p++)
        {
            char c = q[p];
            if (char.IsDigit(c))
            {
                builder.Append(c);
            }
            else
            {
                r.Add(builder.ToString());
                builder.Clear();
                builder.Append(c);
            }
        }

        r.Add(builder.ToString());
        return r;
    }

    private string _letters;

    public string Letters 
    {
        get
        {
            return _letters;
        }
        set
        {
            _letters = value;
        } 
    }
    public string Format { get; set; }

    public ClueContent(string specification)
    {
        int p = specification.IndexOf(':');
        _letters = specification[..p];
        Format = specification[(p + 1)..];
    }

    public ClueContent(int sz)
    {
        // called in Clue constructor
        _letters =CrosswordWordTemplate.Stringy(sz,Clue.UnknownLetterChar);
        Format =$"{sz}";
    }

    public string Specification()
    {
        // called when saving Clues to file
        return $"{Letters}:{Format}";
    }

    public static List<string> LetterPatterns(int length)
    {
        List<List<int>> foundPatterns = AddGap(length, new List<int>());
        int order = 1;
        bool added = true;
        while (added)
        {
            List<List<int>> additions = new();
            foreach (var pattern in foundPatterns)
            {
                if (pattern.Count == order)
                {
                    List<List<int>> novelties = AddGap(length, pattern);
                    additions.AddRange(novelties);
                }
            }

            foundPatterns
                .AddRange(additions); // new items added to foundPatterns OUTSIDE the iteration over foundPatterns
            order++;
            added = additions.Count > 0;
        }

        List<List<int>> sortedFoundPatterns = SortedPatterns(foundPatterns);  // limit list to five words (four gaps)
        
        // Convert the list of int positions into a list of word-length patterns
        List<string> results = new();

        foreach (var gapPattern in sortedFoundPatterns)
        {
            results.Add(Translated(length, gapPattern));
        }

        return results;
    }

    private static string Translated(int clueLength, List<int> raw)
    {
        string answer = string.Empty;
        int position = 0;
        foreach (var p in raw)
        {
            int wordLength = p - position;
            answer += $"{wordLength},";
            position = p;
        }
        int tailLength = clueLength - position;
        answer += $"{tailLength}";
        return answer;
    }

    /// <summary>
    /// Converts a string of integers separated by commas into a list of all permutations of the separators as commas and hyphens
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static List<string> CommaHyphenPermutations(string source)
    {
        int ct = 0;
        foreach (var c  in source)
        {
            if (c == ',')
            {
                ct++;
            }
        }
        var perms =(int) Math.Pow(2, ct);
        int binaryStringLength = Convert.ToString(perms - 1, 2).Length;
        List<string> outputList = new();
        for (var i = 0; i < perms; i++)
        {
            var binary = Convert.ToString(i, 2);
            binary = binary.PadLeft(binaryStringLength);
            string output = string.Empty;
            int p = 0;
            foreach (var c in source)
            {
                switch (c)
                {
                    case ',':
                    {
                        switch (binary[p])
                        {
                            case '1':
                            {
                                output += '-';
                                break;
                            }
                            default:
                            {
                                output += ',';
                                break;
                            }
                        }

                        p++;
                        break;
                    }
                    default:
                    {
                        output += c;
                        break;
                    }
                }
                
            }
            outputList.Add(output);
        }

        return outputList;
    }

    private static List<List<int>> AddGap(int length, List<int> deja)
    {
        List<List<int>> results = new();
        int rightmost = (deja.Count == 0) ? 0 : deja.Max();
        // the int values represent the letter position BEFORE WHICH the gap occurs
        for (int nova = rightmost+1; nova < length ; nova++)
        {
            List<int> pattern = new List<int>();
            foreach (var p in deja)
            {
                pattern.Add(p);
            }
            pattern.Add(nova);
            results.Add(pattern);
        }

        return results;
    }

    private static List<List<int>> SortedPatterns(List<List<int>> source)
    {
        // limit list to five words (four gaps)
        List<List<int>> results = new(); // will be sorted by number of words and then by lengths of words
        for (int length = 1; length < 5; length++)
        {
            List<Tuple<string, List<int>>> sortingList = new();
            foreach (var sample in source)
            {
                if (sample.Count == length)
                {
                    string sortable = string.Empty;
                    foreach (var i in sample)
                    {
                        sortable += $"{i:00}";
                    }
                    sortingList.Add(new Tuple<string, List<int>>(sortable, sample));
                }
            }
            sortingList.Sort();
            foreach (var tuple in sortingList)
            {
                results.Add(tuple.Item2);
            }
        }

        return results;
    }

    public static string Backwards(string normal)
    {
        // Convert to char array, then call Array.Reverse.
        // ... Finally use string constructor on array.
        char[] array = normal.ToCharArray();
        Array.Reverse(array);
        return new string(array);
    }
    
}