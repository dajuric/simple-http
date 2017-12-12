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
                stream.CopyTo(response.OutputStream, MAX_BUFFER_SIZE);
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
