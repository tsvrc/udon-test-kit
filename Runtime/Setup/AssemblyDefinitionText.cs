using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tsvrc.UdonTestKit
{
    // Reads and adds the items of a string array in the text of an assembly definition, and leaves
    // the rest of the text as it is.
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

        // Adds the items after those of the array, one to a line when its items are, or adds the array
        // after the opening brace when the text has none.
        internal static string WithItemsAdded(string json, string key, IEnumerable<string> items)
        {
            List<string> quoted = items.Select(item => $"\"{item}\"").ToList();
            if (quoted.Count == 0)
            {
                return json;
            }

            Match array = ArrayOf(key).Match(json);
            if (!array.Success)
            {
                return json.Insert(json.IndexOf('{') + 1, $"\n    \"{key}\": [{string.Join(", ", quoted)}],");
            }

            Group present = array.Groups["items"];
            string kept = present.Value.TrimEnd();
            string added;
            if (kept.Length == 0)
            {
                added = string.Join(", ", quoted);
            }
            else if (kept.Contains('\n'))
            {
                string indent = Regex.Match(kept, "\n(?<indent>[ \\t]*)\"").Groups["indent"].Value;
                added = kept + string.Concat(quoted.Select(item => $",\n{indent}{item}")) + present.Value.Substring(kept.Length);
            }
            else
            {
                added = kept + string.Concat(quoted.Select(item => $", {item}")) + present.Value.Substring(kept.Length);
            }
            return json.Substring(0, present.Index) + added + json.Substring(present.Index + present.Length);
        }

        private static Regex ArrayOf(string key) => new Regex($"\"{key}\"\\s*:\\s*\\[(?<items>[^\\]]*)\\]");
    }
}
