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
