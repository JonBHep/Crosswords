using System;
using System.Globalization;
using System.Text;

namespace Crosswords;

public class SourceWord
{
    // TODO Remove unnecessary elements from this class
    public string UnSpaced { get; }
    public string Formatted { get; }

    public SourceWord(string source)
    {
        Formatted = source.Trim();
        UnSpaced = UnSpace(Formatted);
        SortString(UnSpaced);
    }
    
    public int FullLength { get => Formatted.Length; }
    public int UnSpacedLength { get=> UnSpaced.Length; }
    private static string UnSpace(string input)
    {
        var builder = new StringBuilder(); 
        for (int a = 0; a < input.Length; a++)
        {
            var c = input[a];
            if (char.IsLetter(c))
            {
                builder.Append(c);
            }
        }

        return builder.ToString().ToLower(CultureInfo.CurrentCulture);
    }

    
    
    // private static string Stringy(int size, char c)
    // {
    //     var builder = new StringBuilder();
    //     for (int x = 0; x < size; x++)
    //     {
    //         builder.Append(c);
    //     }
    //
    //     return builder.ToString();
    // }
    
    // public static bool MatchesTemplate(string testWord, string matchingTemplate)
    // {
    //     if (matchingTemplate == "*")
    //     {
    //         return true;
    //     }
    //
    //     if (matchingTemplate == testWord)
    //     {
    //         return true;
    //     }
    //
    //     int templateLength = matchingTemplate.Length;
    //     string qmarks = Stringy(templateLength, '?');
    //     if ((matchingTemplate == qmarks) && (testWord.Length == templateLength))
    //     {
    //         return true; // template is all question marks and the same length as the word
    //     } 
    //
    //     int pt = -1;
    //     int stars = 0;
    //     do
    //     {
    //         pt = matchingTemplate.IndexOf('*', pt + 1);
    //         if (pt >= 0)
    //         {
    //             stars++;
    //         }
    //     } while (pt >= 0);
    //
    //     int templateMinimumLength = templateLength - stars;
    //     if (testWord.Length < (templateMinimumLength))
    //     {
    //         return false; // if the test word is shorter than the length of the template (ignoring stars) then it cannot be a match : in my interpretation, * can be length zero
    //     } 
    //
    //     if (stars == 0)
    //     {
    //         if (testWord.Length != templateLength)
    //         {
    //             return false; // The template cannot represent this word as they are different lengths (there are no stars which could be of variable length)
    //         } 
    //
    //         bool flagA = true;
    //         for (int n = 0; n < templateLength; n++)
    //         {
    //             char w = testWord[n];
    //             char t = matchingTemplate[n];
    //             if ((w != t) && (t != '?'))
    //             {
    //                 flagA = false; // The template should contain either a wildcard or a matching character at each position
    //                 break;
    //             } 
    //         }
    //
    //         return flagA;
    //     }
    //
    //     // The template contains at least one *
    //     int spot = matchingTemplate.IndexOf('*');
    //     string l = matchingTemplate.Substring(0, spot); // everything to the left of the first star
    //     string r = matchingTemplate.Substring(spot + 1); // everything to the right of the first star
    //     bool flagB = false;
    //
    //     for (int z = 0; z <= (1 + testWord.Length - templateLength); z++)
    //     {
    //         string q = Stringy(z, '?');
    //         if (MatchesTemplate(testWord, l + q + r))
    //         {
    //             flagB = true;
    //             break;
    //         }
    //     }
    //
    //     return flagB;
    // }
    
    // public static bool MatchesTemplate(SourceWord testWord, string matchingTemplate)
    // {
    //     
    //
    //     if (matchingTemplate == testWord)
    //     {
    //         return true;
    //     }
    //
    //     int templateLength = matchingTemplate.Length;
    //     string qmarks = Stringy(templateLength, '.');
    //     if ((matchingTemplate == qmarks) && (testWord.Length == templateLength))
    //     {
    //         return true; // template is all question marks and the same length as the word
    //     } 
    //
    //     int pt = -1;
    //     int stars = 0;
    //     do
    //     {
    //         pt = matchingTemplate.IndexOf('*', pt + 1);
    //         if (pt >= 0)
    //         {
    //             stars++;
    //         }
    //     } while (pt >= 0);
    //
    //     int templateMinimumLength = templateLength - stars;
    //     if (testWord.Length < (templateMinimumLength))
    //     {
    //         return false; // if the test word is shorter than the length of the template (ignoring stars) then it cannot be a match : in my interpretation, * can be length zero
    //     } 
    //
    //     if (stars == 0)
    //     {
    //         if (testWord.Length != templateLength)
    //         {
    //             return false; // The template cannot represent this word as they are different lengths (there are no stars which could be of variable length)
    //         } 
    //
    //         bool flagA = true;
    //         for (int n = 0; n < templateLength; n++)
    //         {
    //             char w = testWord[n];
    //             char t = matchingTemplate[n];
    //             if ((w != t) && (t != '?'))
    //             {
    //                 flagA = false; // The template should contain either a wildcard or a matching character at each position
    //                 break;
    //             } 
    //         }
    //
    //         return flagA;
    //     }
    //
    //     // The template contains at least one *
    //     int spot = matchingTemplate.IndexOf('*');
    //     string l = matchingTemplate.Substring(0, spot); // everything to the left of the first star
    //     string r = matchingTemplate.Substring(spot + 1); // everything to the right of the first star
    //     bool flagB = false;
    //
    //     for (int z = 0; z <= (1 + testWord.Length - templateLength); z++)
    //     {
    //         string q = Stringy(z, '?');
    //         if (MatchesTemplate(testWord, l + q + r))
    //         {
    //             flagB = true;
    //             break;
    //         }
    //     }
    //
    //     return flagB;
    // }
}