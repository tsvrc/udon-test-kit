using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tsvrc.UdonTestKit
{
    // Reads the items of a string array in the text of an assembly definition.
    internal static class AssemblyDefinitionText
    {
        private static readonly Regex StringItem = new Regex("\"(?<value>[^\"]*)\"");

        internal static List<string> Items(string json, string key)
        {
            Match array = ArrayOf(key).Match(json);
            return array.Success
                ? StringItem.Matches(array.Groups["items"].Value).Cast<Match>().Select(item => item.Groups["value"].Value).ToList()
                : new List<string>();
        }

        private static Regex ArrayOf(string key) => new Regex($"\"{key}\"\\s*:\\s*\\[(?<items>[^\\]]*)\\]");
    }
}
