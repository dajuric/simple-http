using System.Collections.Generic;
using System.IO;
using System.Net;

namespace SimpleHttp
{
    static partial class RequestExtensions
    {
        static bool ParseForm(HttpListenerRequest request, Dictionary<string, string> args)
        {
            if (request.ContentType != "application/x-www-form-urlencoded")
                return false;

            var str = request.BodyAsString();
            if (str == null)
                return false;

            foreach (var pair in str.Split('&'))
            {
                var nameValue = pair.Split('=');
                if (nameValue.Length != (1 + 1))
                    continue;

                args.Add(nameValue[0], WebUtility.UrlDecode(nameValue[1]));
            }

            return true;
        }

        static string BodyAsString(this HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
                return null;

            string str = null;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                str = reader.ReadToEnd();
            }

            return str;
        }
    }
}
