﻿using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Crosswords;

public class CrosswordWordTemplate
{
    private string UnSpaced { get; }
    private string Formatted { get; }

    private string Gaps { get; } // A string representing the positions of spaces and hyphens to allow comparison
    // of the pattern of the template and of the word under consideration

    private static readonly char[] Accented
        = "àèìòùÀÈÌÒÙ äëïöüÄËÏÖÜ âêîôûÂÊÎÔÛ áéíóúÁÉÍÓÚðÐýÝ ãñõÃÑÕšŠžŽçÇåÅøØ".ToCharArray();

    private static readonly char[] Unaccented
        = "aeiouAEIOU aeiouAEIOU aeiouAEIOU aeiouAEIOUdDyY anoANOsSzZcCaAoO".ToCharArray();

    public CrosswordWordTemplate(string source)
    {
        string lowered = source.Trim().ToLower(CultureInfo.CurrentCulture); // convert to lower to aid comparison
        string relevant = IrrelevantCharsExcluded(lowered);
        Formatted = relevant;
        Gaps = GetGaps(out string plain);
        UnSpaced = plain;
    }

    private string IrrelevantCharsExcluded(string a)
    {
        string b = string.Empty;
        foreach (char c in a)
        {
            if (char.IsLetter(c))
            {
                b += c;
            }
            else if (c is Clue.UnknownLetterChar)
            {
                b += c;
            }
            else          
            {
                if (c is ' ' or '-')
                {
                    b += c;
                }
            }
        }

        return b;
    }

    private int UnSpacedLength => UnSpaced.Length;

    private string GetGaps(out string squeezed)
    {
        var gapsBuilder = new StringBuilder();
        var unspacedBuilder = new StringBuilder();
        for (int a = 0; a < Formatted.Length; a++)
        {
            var c = Formatted[a];
            if (c is ' ' or '-') // either a space or a hyphen
            {
                gapsBuilder.Append($"{a:00}");
            }
            else if (char.IsLetter(c) || c==Clue.UnknownLetterChar)
            {
                unspacedBuilder.Append(c);
            }
            // NB other characters e.g. apostrophes do not count as spacers nor letters
        }

        squeezed = unspacedBuilder.ToString();
        return gapsBuilder.ToString();
    }

    private static string Unspaced(string input)
    {
        var unspacedBuilder = new StringBuilder();
        foreach (var c in input)
        {
            if (char.IsLetter(c)) 
            {
                unspacedBuilder.Append(c);
            }
        }
        return unspacedBuilder.ToString();
    }
    
    public static string Stringy(int size, char c)
    {
        var builder = new StringBuilder();
        for (int x = 0; x < size; x++)
        {
            builder.Append(c);
        }

        return builder.ToString();
    }

    public bool MatchesTemplate(CrosswordWordTemplate template)
    {
        
        if (template.Formatted == Formatted)
        {
            return true; // trivially
        }

        if (template.UnSpacedLength != UnSpacedLength)
        {
            
            return false;
        }

        if (template.Gaps != Gaps)
        {
            // whether the pattern of gaps between words matches (ignoring whether the gaps are spaces or hyphens)
            return false;
        }

        // wildcard is full stop, not question mark, and there is no variable-length wildcard '*'

        string onlyWildCards = Stringy(UnSpacedLength, '.');
        
        if (template.UnSpaced == onlyWildCards)
        {
            return true; // the template is all wildcards and the same length and spacing as this word
        }

        bool unmatchedFlag = false;
        for (int n = 0; n < UnSpacedLength; n++)
        {
            char moi = UnSpaced[n];
            char toi = template.UnSpaced[n];
            if (toi != '.') // template character is not a wildcard
            {
                char moiPlain = UnAccent(moi); // ignore accents on characters - convert to plain character
                char toiPlain = UnAccent(toi);
                if (moiPlain != toiPlain)
                {
                    unmatchedFlag = true;
                    break;
                }
            }
        }

        return !unmatchedFlag;
    }
    
    public bool MatchesTemplateWithExtraCharsToBeIncluded(CrosswordWordTemplate template, string extras)
    {
        // Assuming that this CrosswordWordTemplate (to be compared with pattern in template parameter) contains no wildcards
        
        // Wildcard is full stop, not question mark, and there is no variable-length wildcard '*'
       
        if (template.Formatted == Formatted)
        {
            return true; // trivially
        }

        if (template.UnSpacedLength != UnSpacedLength)
        {
            return false;
        }
        
        if (template.Gaps != Gaps)
        {
            // whether the pattern of gaps between words matches (ignoring whether the gaps are spaces or hyphens)
            return false;
        }
        
        // Check whether all literal letters in template match this word
        var unmatchedLetters =new StringBuilder(); // Find letters in this word not used in matching the given letters in the template
        var mismatch = false;
        for (var n = 0; n < UnSpacedLength; n++)
        {
            var moi = UnSpaced[n];
            var toi = template.UnSpaced[n];
            if (toi == '.') // template character is a wildcard
            {
                unmatchedLetters.Append(UnAccent(moi));
            }
            else // template character is an actual letter, not a wildcard
            {
                var moiPlain = UnAccent(moi); // ignore accents on characters - convert to plain character
                var toiPlain = UnAccent(toi);
                if (moiPlain != toiPlain) // word and matching template both specify a letter in this position but they are different
                {
                    mismatch = true;
                    break;
                }
            }
        }

        if (mismatch)
        {
            return false;
        }
        
        var unconsumedLetters = unmatchedLetters.ToString().ToUpper(CultureInfo.CurrentCulture);
        var unconsumed = unconsumedLetters.ToCharArray().ToList();
        foreach (var extra in extras)
        {
            var u = UnAccent(extra);
            if (unconsumed.Contains(u))
            {
                unconsumed.Remove(u); // In case more than one copy of the same letter is included in extras
            }
            else
            {
                mismatch = true;
                break;
            }
        }
        return !mismatch;
    }
    private static char UnAccent(char quoi)
    {
        char sub = quoi;
        for (int index = 0; index <= Accented.GetUpperBound(0); index++)
        {
            if (quoi == Accented[index])
            {
                sub = Unaccented[index];
                break;
            }
        }
        return sub;
    }
    
    private static string UnAccent(string quoi)
    {
        for (int index = 0; index <=Accented.GetUpperBound(0); index++)
        {
            quoi = quoi.Replace(Accented[index], Unaccented[index]);
        }
        return quoi;
    }
    
    private static string SortedString(string input)
    {
        var characters = input.ToCharArray();
        Array.Sort(characters);
        return new string(characters);
    }

    public static string AnagramString(string sample)
    {
        return SortedString(SortingString(sample));
    }
    
    public static string SortingString(string sample)
    {
        var x = UnAccent(sample);
        var y = Unspaced(x);
        return y.ToLower();
    }
    
}