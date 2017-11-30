using System;
using System.Collections.Generic;
using System.Net;

namespace SimpleHttp
{
    public static partial class RequestExtensions
    {
        public static Dictionary<string, HttpFile> ParseBody(this HttpListenerRequest request, Dictionary<string, string> args)
        {
            var files = new Dictionary<string, HttpFile>();

            if (request.ContentType.StartsWith("application/x-www-form-urlencoded"))
            {
                ParseForm(request, args);
            }
            else if (request.ContentType.StartsWith("multipart/form-data"))
            {
                files = ParseMultipartForm(request, args);
            }
            else
                throw new NotSupportedException("The body content-type is not supported.");

            return files;
        }
    }
}
