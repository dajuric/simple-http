using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SimpleHttpRpc
{
    public static class Templating
    {
        public static string RenderFile(string fileName, Dictionary<string, string> replacements)
        {
            var str = File.ReadAllText(fileName);
            return RenderString(str, replacements);
        }

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


        public static string RenderFile<T>(string fileName, T obj)
        {
            var replacements = fromObject(obj);
            return RenderFile(fileName, replacements);
        }

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

    }
}
