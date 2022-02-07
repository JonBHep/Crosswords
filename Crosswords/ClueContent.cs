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
        _letters =CrosswordWordTemplate.Stringy(sz, CrosswordGrid.UnknownLetterChar);
        Format =$"{sz}";
    }

    public string Specification()
    {
        // called when saving Clues to file
        return $"{Letters}:{Format}";
    }
}