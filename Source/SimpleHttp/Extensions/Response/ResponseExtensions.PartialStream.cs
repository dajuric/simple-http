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

using HeyRed.Mime;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace SimpleHttp
{
    public static partial class ResponseExtensions
    {
        const string BYTES_RANGE_HEADER = "Range";
        const int MAX_BUFFER_SIZE = 8 * 1024 * 1024;


        /// <summary>
        /// Writes the specified file content to the response.
        /// <para>Response is closed and can not be longer modified.</para>
        /// <para>Built-in support for 'byte-range' response, 'ETag' and 'Last-Modified'.</para>
        /// </summary>
        /// <param name="response">HTTP response.</param>
        /// <param name="request">HTTP request used to determine 'Range' header</param>
        /// <param name="fileName">File path with name.</param>
        public static void AsFile(this HttpListenerResponse response, HttpListenerRequest request, string fileName)
        {
            if (!File.Exists(fileName))
            {
                response.WithCode(HttpStatusCode.NotFound);
                throw new FileNotFoundException($"The file '{fileName}' was not found.");
            }

            if (request.Headers[BYTES_RANGE_HEADER]== null && handleIfCached()) //do not cache partial responses
                return;

            var sourceStream = File.OpenRead(fileName);
            fromStream(request, response, sourceStream, MimeTypesMap.GetMimeType(Path.GetExtension(fileName)));

            bool handleIfCached()
            {
                var lastModified = File.GetLastWriteTimeUtc(fileName);
                response.Headers["ETag"] = lastModified.Ticks.ToString("x");
                response.Headers["Last-Modified"] = lastModified.ToString("R");

                var ifNoneMatch = request.Headers["If-None-Match"];
                if (ifNoneMatch != null)
                {
                    var eTags = ifNoneMatch.Split(',').Select(x => x.Trim()).ToArray();
                    if (eTags.Contains(response.Headers["ETag"]))
                    {
                        response.StatusCode = (int)HttpStatusCode.NotModified;
                        response.Close();
                        return true;
                    }
                }

                var dateExists = DateTime.TryParse(request.Headers["If-Modified-Since"], out DateTime ifModifiedSince); //only for GET requests
                if (dateExists)
                {
                    if (lastModified <= ifModifiedSince)
                    {
                        response.StatusCode = (int)HttpStatusCode.NotModified;
                        response.Close();
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Writes the specified data to the response.
        /// <para>Response is closed and can not be longer modified.</para>
        /// </summary>
        /// <param name="response">HTTP response.</param>
        /// <param name="request">HTTP request used to determine 'Range' header</param>
        /// <param name="data">Data to write.</param>
        /// <param name="mime">Mime type.</param>
        public static void AsBytes(this HttpListenerResponse response, HttpListenerRequest request, byte[] data, string mime = "octet/stream")
        {
            if (data == null)
            {
                response.WithCode(HttpStatusCode.BadRequest);
                throw new ArgumentNullException(nameof(data));
            }

            var sourceStream = new MemoryStream(data);
            fromStream(request, response, sourceStream, mime);
        }

        /// <summary>
        /// Writes the specified data to the response.
        /// <para>Response is closed and can not be longer modified.</para>
        /// </summary>
        /// <param name="response">HTTP response.</param>
        /// <param name="request">HTTP request used to determine 'Range' header</param>
        /// <param name="stream">
        /// Data to write.
        /// <para>Stream must support seek operation due to 'byte-range' functionality.</para>
        /// </param>
        /// <param name="mime">Mime type.</param>
        public static void AsStream(this HttpListenerResponse response, HttpListenerRequest request, Stream stream, string mime = "octet/stream")
        {
            if (stream == null)
            {
                response.WithCode(HttpStatusCode.BadRequest);
                throw new ArgumentNullException(nameof(stream));
            }

            fromStream(request, response, stream, mime);
        }

        static void fromStream(HttpListenerRequest request, HttpListenerResponse response, Stream stream, string mime)
        {
            if (request.Headers.AllKeys.Count(x => x == BYTES_RANGE_HEADER) > 1)
                throw new NotSupportedException("Multiple 'Range' headers are not supported.");

            int start = 0, end = (int)stream.Length - 1;

            //partial stream response support
            var rangeStr = request.Headers[BYTES_RANGE_HEADER];
            if (rangeStr != null)
            {
                var range = rangeStr.Replace("bytes=", String.Empty)
                                    .Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(x => Int32.Parse(x))
                                    .ToArray();

                start = (range.Length > 0) ? range[0] : 0;
                end = (range.Length > 1) ? range[1] : (int)(stream.Length - 1);

                response.WithHeader("Accept-Ranges", "bytes")
                        .WithHeader("Content-Range", "bytes " + start + "-" + end + "/" + stream.Length)
                        .WithCode(HttpStatusCode.PartialContent);

                response.KeepAlive = true;
            }

            //common properties
            response.WithContentType(mime);
            response.ContentLength64 = (end - start + 1);

            //data delivery
            try
            {
                stream.Position = start;
                stream.CopyTo(response.OutputStream, Math.Min(MAX_BUFFER_SIZE, end - start + 1));
            }
            catch (Exception ex) when (ex is HttpListenerException) //request canceled
            {
                response.StatusCode = (int)HttpStatusCode.NoContent;              
            }
            finally
            {
                stream.Close();
                response.Close();
            }
        }
    }
}
