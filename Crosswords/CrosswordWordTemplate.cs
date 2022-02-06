using System;
using System.Globalization;
using System.Text;

namespace Crosswords;

public class CrosswordWordTemplate
{
    public string UnSpaced { get; }
    public string Formatted { get; }

    public string Gaps { get; } // A string representing the positions of spaces and hyphens to allow comparison
    // of the pattern of the template and of the word under consideration

    private static readonly char[] Accented
        = "àèìòùÀÈÌÒÙ äëïöüÄËÏÖÜ âêîôûÂÊÎÔÛ áéíóúÁÉÍÓÚðÐýÝ ãñõÃÑÕšŠžŽçÇåÅøØ".ToCharArray();

    private static readonly char[] Unaccented
        = "aeiouAEIOU aeiouAEIOU aeiouAEIOU aeiouAEIOUdDyY anoANOsSzZcCaAoO".ToCharArray();

    public CrosswordWordTemplate(string source)
    {
        Formatted = source.Trim().ToLower(CultureInfo.CurrentCulture); // convert to lower to aid comparison
        Gaps = GetGaps(out string plain);
        UnSpaced = plain;
    }

    public int UnSpacedLength
    {
        get => UnSpaced.Length;
    }

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
            else
            {
                unspacedBuilder.Append(c);
            }
        }

        squeezed = unspacedBuilder.ToString();
        return gapsBuilder.ToString();
    }
    
    public static string Unspaced(string input)
    {
        var unspacedBuilder = new StringBuilder();
        for (int a = 0; a < input.Length; a++)
        {
            var c = input[a];
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

        // wildcard is full stop, not question mark and there is no variable-length wildcard '*'

        string allWildCards = Stringy(UnSpacedLength, '.');
        if (template.UnSpaced == allWildCards)
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