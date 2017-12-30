using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SimpleHttp
{
    /// <summary>
    /// Class containing extensions for <see cref="String"/>.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Matches all the expressions inside '{ }' defined in <paramref name="pattern"/> for the <paramref name="query"/> and populates the <paramref name="args"/>.
        /// <para>Example: query: "Hello world", pattern: "{first} world" => args["first"] is "Hello".</para>
        /// </summary>
        /// <param name="query">Query string.</param>
        /// <param name="pattern">Pattern string defining the expressions to match inside '{ }'.</param>
        /// <param name="args">Key-value pair collection populated by <paramref name="pattern"/> keys and matches in <paramref name="query"/> if found.</param>
        /// <returns>True is all defined keys in <paramref name="pattern"/> are matched, false otherwise.</returns>
        public static bool TryMatch(this string query, string pattern, Dictionary<string, string> args)
        {
            var names = new List<string>();
            var regex = Regex.Replace(pattern, @"\{\w+\}", m =>
            {
                names.Add(m.Value.Substring(1, m.Value.Length - 1 - 1));
                return @"(.+?)";
            });

            //if regex is not employed, strings must match
            if (names.Count == 0)
                return String.Compare(query, regex, true) == 0;

            //make the last pattern greedy
            regex = replaceLastOccurrence(regex, @"(.+?)", @"(.+)");

            var match = Regex.Match(query, regex, RegexOptions.IgnoreCase);
            if (!match.Success) return false;

            for (int i = 0; i < Math.Min(names.Count, match.Groups.Count - 1); i++)
            {
                args.Add(names[i], match.Groups[i + 1].Value);
            }

            return true;
        }

        static string replaceLastOccurrence(string source, string oldStr, string newStr)
        {
            int place = source.LastIndexOf(oldStr);

            if (place == -1)
                return source;

            string result = source.Remove(place, oldStr.Length).Insert(place, newStr);
            return result;
        }
    }
}
