using System.Collections.Generic;
using System.IO;
using System.Net;

namespace SimpleHttpRpc
{
    public static class RequestExtensions
    {
        public static string BodyAsString(this HttpListenerRequest request)
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

        public static Dictionary<string, string> FormData(this HttpListenerRequest request)
        {
            var d = new Dictionary<string, string>();

            if (request.ContentType != "application/x-www-form-urlencoded")
                return d;

            var str = request.BodyAsString();
            if (str == null)
                return d;

            foreach (var pair in str.Split('&'))
            {
                var nameValue = pair.Split('=');
                if (nameValue.Length != (1 + 1))
                    continue;

                d.Add(nameValue[0], WebUtility.UrlDecode(nameValue[1]));
            }

            return d;
        }
    }
}
