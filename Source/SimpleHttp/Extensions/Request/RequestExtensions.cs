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
using System.Net;

namespace SimpleHttp
{
    /// <summary>
    /// Class containing <see cref="HttpListenerRequest"/> extensions.
    /// </summary>
    public static partial class RequestExtensions
    {
        /// <summary>
        /// Parses body of the request including form and multi-part form data.
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <param name="args">Key-value pairs populated by the form data by this function.</param>
        /// <returns>Name-file pair collection.</returns>
        public static Dictionary<string, HttpFile> ParseBody(this HttpListenerRequest request, Dictionary<string, string> args)
        {
            return request.ParseBody(args, (n, fn, ct) => new MemoryStream());
        }

        /// <summary>
        /// Parses body of the request including form and multi-part form data.
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <param name="args">Key-value pairs populated by the form data by this function.</param>
        /// <param name="onFile">
        /// Function called if a file is about to be parsed. The stream is attached to a corresponding <see cref="HttpFile"/>.
        /// <para>By default, <see cref="MemoryStream"/> is used, but for large files, it is recommended to open <see cref="FileStream"/> directly.</para>
        /// </param>
        /// <returns>Name-file pair collection.</returns>
        public static Dictionary<string, HttpFile> ParseBody(this HttpListenerRequest request, Dictionary<string, string> args, OnFile onFile)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (args == null)
                throw new ArgumentNullException(nameof(args));

            if (onFile == null)
                throw new ArgumentNullException(nameof(onFile));


            var files = new Dictionary<string, HttpFile>();

            if (request.ContentType.StartsWith("application/x-www-form-urlencoded"))
            {
                ParseForm(request, args);
            }
            else if (request.ContentType.StartsWith("multipart/form-data"))
            {
                files = ParseMultipartForm(request, args, onFile);
            }
            else
                throw new NotSupportedException("The body content-type is not supported.");

            return files;
        }
    }
}
