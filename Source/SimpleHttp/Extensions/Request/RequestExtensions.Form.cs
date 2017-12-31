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
