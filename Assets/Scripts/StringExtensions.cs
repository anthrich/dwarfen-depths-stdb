using System.Text;

public static class StringExtensions
{
    private static string TagOpenStart = "<color=";
    private static string TagOpenEnd = ">";
    private static string TagClose = "</color>";
    private static StringBuilder StringBuilder = new();
    
    public static string WithRTColour(this string str, string colour)
    {
        StringBuilder.Append(TagOpenStart);
        StringBuilder.Append(colour);
        StringBuilder.Append(TagOpenEnd);
        StringBuilder.Append(str);
        StringBuilder.Append(TagClose);
        str = StringBuilder.ToString();
        StringBuilder.Clear();
        return str;
    }
}