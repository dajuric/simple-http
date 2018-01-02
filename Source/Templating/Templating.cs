#region License
// Copyright © 2018 Darko Jurić
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SimpleHttp
{
    /// <summary>
    /// Class defining methods for string or file pattern replacements.
    /// </summary>
    public static class Templating
    {
        #region Replacements (with key-values)

        /// <summary>
        /// Replaces all occurrences defined inside each {key} expression with values. Keys and values are specified in the replacements.
        /// </summary>
        /// <param name="fileName">File path with name.</param>
        /// <param name="replacements">Key-value pair collection for replacements.</param>
        /// <returns>Processed file content.</returns>
        public static string RenderFile(string fileName, Dictionary<string, string> replacements)
        {
            var str = File.ReadAllText(fileName);
            return RenderString(str, replacements);
        }

        /// <summary>
        /// Replaces all occurrences defined inside each {key} expression with values. Keys and values are specified in the replacements.
        /// </summary>
        /// <param name="template">Template string.</param>
        /// <param name="replacements">Key-value pair collection for replacements.</param>
        /// <returns>Processed template.</returns>
        public static string RenderString(string template, Dictionary<string, string> replacements)
        {
            var r = Regex.Replace(template, @"\{\w+\}", m =>
            {
                var key = m.Value.Substring(1, m.Value.Length - 1 - 1);

                if (!replacements.TryGetValue(key, out string val))
                    val = String.Empty;

                return val;
            });

            return r;
        }

        #endregion

        #region Replacements (with objects)

        /// <summary>
        /// Replaces all occurrences defined inside each {key} expression with values. Keys and values are defined as object property names and values.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="fileName">File path with name.</param>
        /// <param name="obj">Object to use for replacements.</param>
        /// <returns>Processed file content.</returns>
        public static string RenderFile<T>(string fileName, T obj)
        {
            var replacements = fromObject(obj);
            return RenderFile(fileName, replacements);
        }

        /// <summary>
        /// Replaces all occurrences defined inside each {key} expression with values. Keys and values are defined as object property names and values.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="template">Template string.</param>
        /// <param name="obj">Object to use for replacements.</param>
        /// <returns>Processed file content.</returns>
        public static string RenderString<T>(string template, T obj)
        {
            var replacements = fromObject(obj);
            return RenderString(template, replacements);
        }

        private static Dictionary<string, string> fromObject<T>(T obj)
        {
            var d = new Dictionary<string, string>();

            foreach (var pi in typeof(T).GetProperties())
                d[pi.Name] = pi.GetValue(obj).ToString();

            return d;
        }

        #endregion

    }
}
