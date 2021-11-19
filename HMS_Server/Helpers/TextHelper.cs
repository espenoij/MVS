using System.Collections.Generic;
using System.Linq;

public static class TextHelper
{
    public const int ErrorMessageWrapLength = 55;

    public static string Wrap(string text, int max = ErrorMessageWrapLength)
    {
        if (text != null)
        {
            List<string> lines = text
                .Split(' ')
                .Aggregate(new[] { "" }.ToList(), (a, x) =>
                {
                    var last = a[a.Count - 1];
                    if ((last + " " + x).Length > max)
                    {
                        a.Add(x);
                    }
                    else
                    {
                        a[a.Count - 1] = (last + " " + x).Trim();
                    }
                    return a;
                });

            string wrapText = "";
            foreach (var line in lines)
                wrapText += line + "\n";

            return wrapText;
        }
        else
            return "";
    }

    public static string EscapeControlChars(string str)
    {
        // Escaping av utvalgte kontroll karakterer for å gjøre data fra sensorer litt mer "leselig".
        if (str != null)
        {
            string newString = string.Empty;

            if (!string.IsNullOrEmpty(str))
            {
                newString = str;

                newString = newString
                    .Replace("\a", @"\a")
                    .Replace("\b", @"\b")
                    .Replace("\f", @"\f")
                    .Replace("\n", @"\n")
                    .Replace("\r", @"\r")
                    .Replace("\t", @"\t")
                    .Replace("\v", @"\v");

                //newString = newString
                //    .Replace("\'", @"\'")
                //    .Replace("\"", "\\\"")
                //    .Replace("\\", @"\")
                //    .Replace("\\?", @"\\?");

                newString = newString
                    .Replace("\u0000", @"\u0000")
                    .Replace("\u0001", @"\u0001")
                    .Replace("\u0002", @"\u0002")
                    .Replace("\u0003", @"\u0003")
                    .Replace("\u0004", @"\u0004")
                    .Replace("\u0005", @"\u0005")
                    .Replace("\u0006", @"\u0006")
                    .Replace("\u0007", @"\u0007")
                    .Replace("\u0008", @"\u0008")
                    .Replace("\u0009", @"\u0009")
                    .Replace("\u000A", @"\u000A")
                    .Replace("\u000B", @"\u000B")
                    .Replace("\u000C", @"\u000C")
                    .Replace("\u000D", @"\u000D")
                    .Replace("\u000E", @"\u000E")
                    .Replace("\u000F", @"\u000F")
                    .Replace("\u0010", @"\u0010")
                    .Replace("\u0011", @"\u0011")
                    .Replace("\u0012", @"\u0012")
                    .Replace("\u0013", @"\u0013")
                    .Replace("\u0014", @"\u0014")
                    .Replace("\u0015", @"\u0015")
                    .Replace("\u0016", @"\u0016")
                    .Replace("\u0017", @"\u0017")
                    .Replace("\u0018", @"\u0018")
                    .Replace("\u0019", @"\u0019")
                    .Replace("\u001A", @"\u001A")
                    .Replace("\u001B", @"\u001B")
                    .Replace("\u001C", @"\u001C")
                    .Replace("\u001D", @"\u001D")
                    .Replace("\u001E", @"\u001E")
                    .Replace("\u001F", @"\u001F");
            }

            return newString;
        }
        else
            return "";
    }

    public static string EscapeSpace(string str)
    {
        if (str != null)
            return str.Replace("\u0020", @"\u0020");
        else
            return "";
    }

    public static string UnescapeSpace(string str)
    {
        if (str != null)
            return str.Replace(@"\u0020", "\u0020");
        else
            return "";
    }

    public static string RemoveLinefeed(string str)
    {
        if (str != null)
            return str.Replace("\r\n", "");
        else
            return "";
    }

    public static string RemoveNewLine(string str)
    {
        if (str != null)
            return str.Replace("\n", "");
        else
            return "";
    }
}
