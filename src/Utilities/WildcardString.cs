using System.Text.RegularExpressions;

namespace SMTPBroker.Utilities;

public static class WildcardString
{
    private static string WildCardToRegular(string value) 
    {
        return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$"; 
    }

    public static bool IsMatch(string input, string wildcardString)
    {
        return Regex.IsMatch(input, WildCardToRegular(wildcardString));
    }
}